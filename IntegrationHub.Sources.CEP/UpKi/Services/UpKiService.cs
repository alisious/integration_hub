// IntegrationHub.Sources.CEP.UpKi/Services/UpKiService.cs
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

using IntegrationHub.Common.Primitives;                  // Result<T, Error>
using IntegrationHub.Sources.CEP.UpKi.Contracts;         // DaneDokumentuRequest, DaneDokumentuResponse
using IntegrationHub.Sources.CEP.UpKi.Mappers;           // DaneDokumentuResponseXmlMapper
using IntegrationHub.Sources.CEP.UpKi.RequestValidation; // DaneDokumentuRequestValidator

namespace IntegrationHub.Sources.CEP.UpKi.Services
{
    /// <summary>
    /// Produkcyjna implementacja serwisu UpKi – wywołuje realny endpoint CEK/TRU
    /// za pomocą HttpClient "CEPiK.UpKiClient" (konfigurowanego w ServiceCollectionExtensions).
    /// </summary>
    public sealed class UpKiService : IUpKiService
    {
        private readonly ILogger<UpKiService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string HttpClientName = "CEPiK.UpKiClient";

        public UpKiService(
            ILogger<UpKiService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<Result<DaneDokumentuResponse, Error>> GetDriverPermissionsAsync(
            DaneDokumentuRequest body,
            CancellationToken ct = default)
        {
            if (body is null)
            {
                return new Error(
                    Code: "REQUEST_NULL",
                    Message: "Body (DaneDokumentuRequest) nie może być null.",
                    HttpStatus: (int)HttpStatusCode.BadRequest);
            }

            // 1) Walidacja wejścia – tak samo jak w wersji testowej
            var validator = new DaneDokumentuRequestValidator();
            var vr = validator.ValidateAndNormalize(body);
            if (!vr.IsValid)
            {
                var message = vr.ToString();
                return new Error(
                    Code: "REQUEST_VALIDATION",
                    Message: message,
                    HttpStatus: (int)HttpStatusCode.BadRequest);
            }

            // 2) Budowa SOAP envelope
            var soapXml = Helpers.PytanieOUprawnieniaCekEnvelope.Create(body);

            var client = _httpClientFactory.CreateClient(HttpClientName);

            using var request = new HttpRequestMessage(HttpMethod.Post, string.Empty)
            {
                Content = new StringContent(soapXml, Encoding.UTF8, "text/xml")
            };

            // Jeśli WSDL wymaga SOAPAction – można w razie potrzeby włączyć:
            // request.Headers.Add("SOAPAction",
            //     "http://uprawnieniakierowcow-cek.tru.api.cepik.coi.gov.pl/pytanieOUprawnieniaCek");

            string? responseXml = null;

            try
            {
                // 3) Wywołanie HTTP
                using var response = await client
                    .SendAsync(request, HttpCompletionOption.ResponseContentRead, ct)
                    .ConfigureAwait(false);

                var statusCode = (int)response.StatusCode;
                responseXml = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    // Najpierw spróbuj potraktować odpowiedź jako błąd biznesowy (cepikException)
                    var (code, message, details) = TryParseCepikException(responseXml);

                    if (!string.IsNullOrWhiteSpace(code) || !string.IsNullOrWhiteSpace(message))
                    {
                        _logger.LogWarning(
                            "UpKiService – błąd biznesowy CEK/UpKi. Kod={Code}, Message={Message}, HttpStatus={Status}",
                            code, message, statusCode);

                        return new Error(
                            Code: code ?? "UPKI-ERROR",
                            Message: message ?? "Błąd biznesowy zwrócony przez CEK (UpKi).",
                            HttpStatus: statusCode,
                            Details: details);
                    }

                    // Techniczny HTTP error
                    var msg = $"Wywołanie CEK/UpKi zwróciło status HTTP {statusCode} ({response.StatusCode}).";
                    _logger.LogError(
                        "UpKiService – błąd techniczny HTTP. Status={Status}, Message={Message}",
                        statusCode, msg);

                    return new Error(
                        Code: "UPKI-HTTP",
                        Message: msg,
                        HttpStatus: statusCode,
                        Details: responseXml);
                }

                // 4) Mapowanie SOAP → DTO
                var dto = DaneDokumentuResponseXmlMapper.MapFromSoapEnvelope(responseXml);

                // implicit conversion DaneDokumentuResponse -> Result<DaneDokumentuResponse, Error>
                return dto;
            }
            catch (OperationCanceledException oce) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning(oce,
                    "UpKiService – operacja anulowana przez wywołującego.");

                return new Error(
                    Code: "UPKI-CANCELED",
                    Message: "Operacja została anulowana przez wywołującego.",
                    HttpStatus: (int)HttpStatusCode.RequestTimeout);
            }
            catch (InvalidOperationException ex)
            {
                // Mapper zgłosił cepikException (UPKI-1000..UPKI-1030) albo inne błędy mapowania.
                var (code, message, details) = TryParseCepikException(responseXml);

                _logger.LogWarning(ex,
                    "UpKiService – błąd biznesowy CEK (UpKi) podczas mapowania. Kod={Code}, Message={Message}",
                    code, message);

                return new Error(
                    Code: code ?? "UPKI-ERROR",
                    Message: message ?? ex.Message,
                    HttpStatus: (int)HttpStatusCode.BadRequest,
                    Details: details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "UpKiService – błąd techniczny podczas wywołania CEK/UpKi.");

                return new Error(
                    Code: "UPKI-TECHNICAL",
                    Message: ex.Message,
                    HttpStatus: (int)HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Parsuje fragment cepikException z XML (komunikaty/kod, komunikat, szczegoly).
        /// Kopia logiki z UpKiServiceTest, żeby zachować spójność.
        /// </summary>
        private static (string? Code, string? Message, string? Details) TryParseCepikException(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return (null, null, null);

            try
            {
                var doc = XDocument.Parse(xml);

                // Szukamy elementu <cepikException> (bez względu na namespace)
                var cepikException = doc
                    .Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "cepikException");

                if (cepikException == null)
                    return (null, null, null);

                // Wg schemy: <cepikException><komunikaty>...</komunikaty></cepikException>
                var komunikaty = cepikException
                    .Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "komunikaty")
                    ?? cepikException;

                string? Get(string localName)
                    => komunikaty
                        .Elements()
                        .FirstOrDefault(e => e.Name.LocalName == localName)?
                        .Value
                        .Trim();

                var code = Get("kod");          // np. UPKI-1030
                var msg = Get("komunikat");     // np. "Brak danych."
                var details = Get("szczegoly"); // jeśli występuje

                return (code, msg, details);
            }
            catch
            {
                // Nie udało się sparsować – traktujemy jako brak szczegółów.
                return (null, null, null);
            }
        }
    }
}
