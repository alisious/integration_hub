using IntegrationHub.Common.Primitives;
using IntegrationHub.Sources.KSIP.Config;
using IntegrationHub.Sources.KSIP.Contracts;
using IntegrationHub.Sources.KSIP.Helpers;
using IntegrationHub.Sources.KSIP.Mappers;
using IntegrationHub.Sources.KSIP.RequestValidation;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IntegrationHub.Sources.KSIP.Services
{
    /// <summary>
    /// Produkcyjna implementacja serwisu KSIP – wywołuje realny endpoint
    /// SprawdzenieOsoby w ruchu drogowym za pomocą HttpClient "KSIP.SprawdzenieOsobyClient".
    /// </summary>
    public sealed class KSIPService : IKSIPService
    {
        private const string HttpClientName = "KSIP.SprawdzenieOsobyClient";

        /// <summary>
        /// Wartość nagłówka SOAPAction dla operacji SprawdzenieOsoby / PersonOffencesSearch.
        /// </summary>
        private const string SoapActionSprawdzenieOsobyWRD = "um:opSprawdzenieOsoby";

        private readonly ILogger<KSIPService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly KSIPConfig _config; // zakładam, że już masz KSIPConfig – jest rejestrowany w ServiceCollectionExtensions

        public KSIPService(
            ILogger<KSIPService> logger,
            IHttpClientFactory httpClientFactory,
            KSIPConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<Result<SprawdzenieOsobyResponse, Error>> SprawdzenieOsobyWRuchuDrogowymAsync(
            SprawdzenieOsobyRequest body,
            string requestId,
            CancellationToken ct = default)
        {
            if (body is null)
            {
                return ErrorFactory.BusinessError(
                    ErrorCodeEnum.ValidationError,
                    "Body (SprawdzenieOsobyRequest) nie może być null.");
            }

            // 1) Walidacja
            var validator = new SprawdzenieOsobyRequestValidator();
            var vr = validator.ValidateAndNormalize(body);
            if (!vr.IsValid)
            {
                return ErrorFactory.BusinessError(
                    ErrorCodeEnum.ValidationError,
                    vr.MessageError ?? "Błąd walidacji SprawdzenieOsobyRequest.");
            }

            var getByPesel = !string.IsNullOrWhiteSpace(body.NrPesel);

            // 2) Budowa SOAP envelope
            // UnitID pobieramy z konfiguracji – jeśli nie zdefiniowane, rzucamy błąd techniczny.
            var unitId = _config.UnitId; // dodaj tę właściwość w KSIPConfig
            if (string.IsNullOrWhiteSpace(unitId))
            {
                return ErrorFactory.TechnicalError(
                    ErrorCodeEnum.ExternalServiceError,
                    "Brak skonfigurowanego UnitId dla KSIP w appsettings.");
            }

            
            var soapXml = SprawdzenieOsobyEnvelope.Create(
                body,
                requestId,
                unitId,
                systemName: _config.SystemName ?? "ŻW",
                applicationName: _config.ApplicationName ?? "ŻW",
                moduleName: _config.ModuleName ?? "ZW-KSIP",
                terminalName: body.TerminalName ?? "ZW-KSIP");

            //_logger.LogInformation(soapXml);

            var client = _httpClientFactory.CreateClient(HttpClientName);

            using var request = new HttpRequestMessage(HttpMethod.Post, string.Empty)
            {
                Content = new StringContent(soapXml, Encoding.UTF8, "text/xml")
            };

            // SOAPAction – zwykle wymagany przez endpoint
            request.Headers.Add("SOAPAction", SoapActionSprawdzenieOsobyWRD);

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
                    // Najpierw próbujemy zinterpretować odpowiedź jako błąd biznesowy
                    var (code, message, details) = TryParseSoapFault(responseXml);

                    if (!string.IsNullOrWhiteSpace(code) || !string.IsNullOrWhiteSpace(message))
                    {
                        _logger.LogWarning(
                            "KSIPService – błąd biznesowy KSIP. Kod={Code}, Message={Message}, HttpStatus={Status}",
                            code, message, statusCode);

                        return ErrorFactory.BusinessError(
                            ErrorCodeEnum.ExternalServiceError,
                            message ?? "Nieznany błąd biznesowy zwrócony przez usługę KSIP.",
                            details: code ?? details);
                    }

                    // Jeśli nie mamy sensownego błędu biznesowego – traktujemy jako błąd techniczny HTTP
                    var msg = $"Wywołanie KSIP SprawdzenieOsobyWRD zwróciło status HTTP {statusCode} ({response.StatusCode}).";

                    _logger.LogError(
                        "KSIPService – błąd techniczny HTTP. Status={Status}, Message={Message}",
                        statusCode, msg);

                    return ErrorFactory.TechnicalError(
                        ErrorCodeEnum.ExternalServiceError,
                        message: msg,
                        httpStatus: statusCode,
                        details: responseXml);
                }

                
                // 4) Mapowanie SOAP -> DTO
                var dto = SprawdzenieOsobyResponseMapper.MapFromSoapEnvelope(responseXml);
                //5) Sprawdzenie czy State = 0 tzn. brak wykroczeń 
                if (dto.State == 0)
                {
                    var msg = "";
                    if (getByPesel)
                    {
                        msg = $"Nie znaleziono wpisów dla osoby: PESEL = {body.NrPesel}.";
                    }
                    else
                    {
                        msg = $"Nie znaleziono wpisów dla osoby: {body.FirstName} {body.LastName}, ur. {body.BirthDate}.";
                    }

                    _logger.LogWarning(msg);
                    return ErrorFactory.BusinessError(
                        ErrorCodeEnum.NotFoundError,
                        message: msg);
                }

                // implicit conversion SprawdzenieOsobyResponse -> Result<SprawdzenieOsobyResponse, Error>
                return dto;
            }
            catch (OperationCanceledException oce) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning(oce,
                    "KSIPService – operacja anulowana przez wywołującego.");

                return ErrorFactory.TechnicalError(
                    codeEnum: ErrorCodeEnum.OperationCanceledError,
                    message: "Operacja została anulowana przez wywołującego.",
                    httpStatus: (int)HttpStatusCode.RequestTimeout);
            }
            catch (InvalidOperationException ex)
            {
                // Zwykle błąd mapowania/biznesowy (np. nietypowa odpowiedź)
                var (code, message, details) = TryParseSoapFault(responseXml);

                _logger.LogWarning(ex,
                    "KSIPService – błąd biznesowy podczas mapowania odpowiedzi KSIP. Kod={Code}, Message={Message}",
                    code, message);

                return new Error(
                    Code: code ?? "KSIP-BUSINESS-ERROR",
                    Message: message ?? ex.Message,
                    HttpStatus: (int)HttpStatusCode.BadRequest,
                    Details: details ?? responseXml);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "KSIPService – błąd techniczny podczas wywołania KSIP SprawdzenieOsobyWRD.");

                return new Error(
                    Code: "KSIP-TECHNICAL",
                    Message: ex.Message,
                    HttpStatus: (int)HttpStatusCode.InternalServerError,
                    Details: responseXml);
            }
        }

        /// <summary>
        /// Prosta analiza SOAP Fault (jeśli KSIP zwróci standardowy fault).
        /// Analogicznie do TryParseCepikException w UpKiService, ale dla SOAP Fault.
        /// </summary>
        private static (string? Code, string? Message, string? Details) TryParseSoapFault(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return (null, null, null);

            try
            {
                var doc = XDocument.Parse(xml);

                var fault = doc
                    .Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "Fault");

                if (fault == null)
                    return (null, null, null);

                string? Get(string localName) =>
                    fault.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value.Trim();

                var code = Get("faultcode");
                var msg = Get("faultstring");
                var details = fault
                    .Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "detail")
                    ?.Value
                    .Trim();

                return (code, msg, details);
            }
            catch
            {
                return (null, null, null);
            }
        }
    }
}
