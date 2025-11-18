// IntegrationHub.Sources.CEP.UpKi/Services/UpKiServiceTest.cs
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using IntegrationHub.Common.Primitives;                 // Result<T, Error>, Error
using IntegrationHub.Common.RequestValidation;          // ValidationResult
using IntegrationHub.Sources.CEP.UpKi.Contracts;        // DaneDokumentuRequest, DaneDokumentuResponse
using IntegrationHub.Sources.CEP.UpKi.Mappers;          // DaneDokumentuResponseXmlMapper
using IntegrationHub.Sources.CEP.UpKi.RequestValidation; // DaneDokumentuRequestValidator

namespace IntegrationHub.Sources.CEP.UpKi.Services
{
    /// <summary>
    /// Testowa implementacja serwisu UpKi – czyta gotowe pliki XML z katalogu
    ///   &lt;contentRoot&gt;\TestData\CEK
    /// i mapuje je na DaneDokumentuResponse.
    ///
    /// Scenariusze:
    ///  - PESEL = 73020916558 → pytanieOUprawnieniaCek_RESPONSE.xml → sukces
    ///  - inny PESEL           → pytanieOUprawnieniaCek_BrakDanych_RESPONSE.xml
    ///                          → cepikException (np. UPKI-1030 – Brak danych) → Error
    /// </summary>
    public sealed class UpKiServiceTest : IUpKiService
    {
        private const string TestPesel = "73020916558";

        private readonly ILogger<UpKiServiceTest> _logger;
        private readonly string _testDataDir;

        public UpKiServiceTest(ILogger<UpKiServiceTest> logger, IHostEnvironment env)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // <contentRoot>\TestData\CEK
            _testDataDir = Path.Combine(env.ContentRootPath, "TestData", "CEK");
        }

        public async Task<Result<DaneDokumentuResponse, Error>> GetDriverPermissionsAsync(
            DaneDokumentuRequest body,
            CancellationToken ct = default)
        {
            if (body is null)
            {
                // implicit conversion Error -> Result<...,Error>
                return new Error(
                    Code: "REQUEST_NULL",
                    Message: "Body (DaneDokumentuRequest) nie może być null.",
                    HttpStatus: (int)HttpStatusCode.BadRequest);
            }

            // 1) Walidacja jak w produkcji
            var validator = new DaneDokumentuRequestValidator();
            var vr = validator.ValidateAndNormalize(body);
            if (!vr.IsValid)
            {
                // Możesz tu podmienić na swoje ładniejsze formatowanie komunikatu walidacji
                var message = vr.ToString();

                return new Error(
                    Code: "REQUEST_VALIDATION",
                    Message: message,
                    HttpStatus: (int)HttpStatusCode.BadRequest);
            }

            // 2) Dobór pliku testowego na podstawie PESEL
            var pesel = body.NumerPesel?.Trim();

            var fileName = string.Equals(pesel, TestPesel, StringComparison.Ordinal)
                ? "pytanieOUprawnieniaCek_RESPONSE.xml"
                : "pytanieOUprawnieniaCek_BrakDanych_RESPONSE.xml";

            var xmlPath = Path.Combine(_testDataDir, fileName);

            if (!File.Exists(xmlPath))
            {
                var msg = $"Brak pliku z danymi testowymi: {xmlPath}";
                _logger.LogError("UpKiServiceTest – {Message}", msg);

                return new Error(
                    Code: "UPKI-TESTDATA",
                    Message: msg,
                    HttpStatus: (int)HttpStatusCode.InternalServerError);
            }

            string? xml = null;

            try
            {
                xml = await File.ReadAllTextAsync(xmlPath, ct).ConfigureAwait(false);

                // 3) Mapowanie SOAP → DaneDokumentuResponse
                var dto = DaneDokumentuResponseXmlMapper.MapFromSoapEnvelope(xml);

                // implicit conversion DaneDokumentuResponse -> Result<...,Error>
                return dto;
            }
            catch (OperationCanceledException oce) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning(oce, "UpKiServiceTest – operacja anulowana przez wywołującego.");

                return new Error(
                    Code: "UPKI-CANCELED",
                    Message: "Operacja została anulowana przez wywołującego.",
                    HttpStatus: (int)HttpStatusCode.RequestTimeout);
            }
            catch (InvalidOperationException ex)
            {
                // Mapper zgłosił cepikException (UPKI-1000..UPKI-1030).
                // Próbujemy wyciągnąć kod/komunikat z XML (pytanieOUprawnieniaCek_BrakDanych_RESPONSE).
                var (code, message, details) = TryParseCepikException(xml);

                _logger.LogWarning(ex,
                    "UpKiServiceTest – błąd biznesowy CEK (UpKi). Kod={Code}, Message={Message}",
                    code, message);

                return new Error(
                    Code: code ?? "UPKI-ERROR",
                    Message: message ?? ex.Message,
                    HttpStatus: (int)HttpStatusCode.BadRequest,
                    Details: details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpKiServiceTest – błąd techniczny podczas odczytu pliku XML.");

                return new Error(
                    Code: "UPKI-TECHNICAL",
                    Message: ex.Message,
                    HttpStatus: (int)HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Parsuje fragment cepikException z XML (komunikaty/kod, komunikat, szczegoly).
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
                var msg = Get("komunikat");     // np. "Brak danych"
                var details = Get("szczegoly"); // dodatkowe informacje, jeśli są

                // Jak nie ma nic, zostawiamy null-e – wyżej fallback na ex.Message
                return (code, msg, details);
            }
            catch
            {
                // Nie psujemy całej obsługi – fallback
                return (null, null, null);
            }
        }
    }
}
