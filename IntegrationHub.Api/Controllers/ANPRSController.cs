using IntegrationHub.Api.Swagger.Examples.ANPRS;
using IntegrationHub.Application.ANPRS;                    // IANPRSDictionaryFacade
using IntegrationHub.Common.Contracts;                       // ProxyResponse, ProxyResponses, ProxyStatus
using IntegrationHub.Common.Exceptions;         // Countries401Example, ...Countries404Example
using IntegrationHub.Domain.Contracts.ANPRS;
using IntegrationHub.Sources.ANPRS.Client;                  // ANPRSHttpException (rzucany przez ANPRSHttpClient)
using IntegrationHub.Sources.ANPRS.Contracts;               // DictionaryResponse (used for swagger examples)
using IntegrationHub.Sources.ANPRS.Contracts.IntegrationHub.Sources.ANPRS.Contracts;
using IntegrationHub.Sources.ANPRS.Services;                // IANPRSReportsService
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;                       // SwaggerResponseExample
using System.Text.Json;

namespace IntegrationHub.Api.Controllers
{
    [ApiController]
    [Route("ANPRS")]
    [Produces("application/json")]
    public sealed class ANPRSController : ControllerBase
    {
        private readonly IANPRSDictionaryFacade _facade;
        private readonly IANPRSReportsService _reports;

        public ANPRSController(IANPRSDictionaryFacade facade, IANPRSReportsService reportsService)
        {
            _facade = facade;
            _reports = reportsService;
        }

        #region Dictionary endpoints
        /// <summary>
        /// ANPRS – słownik krajów (lista kodów krajów).
        /// </summary>
        [HttpGet("dictionary/countries")]
        [Produces(typeof(ProxyResponse<IEnumerable<string>>))]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<string>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<string>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<string>>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "ANPRS – słownik krajów",
            Description = "Zwraca listę kodów krajów (domenowe stringi) opakowaną w ProxyResponse."
        )]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401Example))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401ValidAuthRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403HttpsRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertInvalidExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code400InvalidParameterExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code404NotFoundExample))]
        public async Task<ProxyResponse<IEnumerable<string>>> GetCountries(CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ANPRS";

            try
            {
                var data = await _facade.GetCountriesAsync(ct);
                if (data is null)
                    return ProxyResponses.TechnicalError<IEnumerable<string>>("Pusta odpowiedź z ANPRS.", source, "502", requestId);

                return ProxyResponses.Success<IEnumerable<string>>(data, source, "200", requestId);
            }
            catch (ANPRSHttpException ex)
            {
                var (code, message) = ExtractHttpCodeAndMessage(ex);
                return ProxyResponses.BusinessError<IEnumerable<string>>(message ?? $"ANPRS HTTP {code}.", source, code.ToString(), requestId);
            }
            catch (OperationCanceledException)
            {
                return ProxyResponses.TechnicalError<IEnumerable<string>>("Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (CertificateException ex)
            {
                return ProxyResponses.BusinessError<IEnumerable<string>>(ex.Message, source, StatusCodes.Status400BadRequest.ToString(), requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponses.TechnicalError<IEnumerable<string>>($"Nieoczekiwany błąd: {ex.Message}", source, "500", requestId);
            }
        }

        /// <summary>
        /// ANPRS – słownik BCP (lista domenowych obiektów BCP).
        /// </summary>
        [HttpGet("dictionary/bcp")]
        [Produces(typeof(ProxyResponse<IEnumerable<BcpRowDto>>))]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<BcpRowDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<BcpRowDto>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<BcpRowDto>>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "ANPRS – słownik BCP",
            Description = "Zwraca listę BCP jako domenowe DTO (BcpRowDto) opakowaną w ProxyResponse."
        )]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401Example))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401ValidAuthRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403HttpsRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertInvalidExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code400InvalidParameterExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code404NotFoundExample))]
        public async Task<ProxyResponse<IEnumerable<BcpRowDto>>> GetBcp(CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ANPRS";

            try
            {
                                    var data = await _facade.GetBCPAsync(ct);
                if (data is null)
                    return ProxyResponses.TechnicalError<IEnumerable<BcpRowDto>>("Pusta odpowiedź z ANPRS.", source, "502", requestId);

                return ProxyResponses.Success<IEnumerable<BcpRowDto>>(data, source, "200", requestId);
            }
            catch (ANPRSHttpException ex)
            {
                var (code, message) = ExtractHttpCodeAndMessage(ex);
                return ProxyResponses.BusinessError<IEnumerable<BcpRowDto>>(message ?? $"ANPRS HTTP {code}.", source, code.ToString(), requestId);
            }
            catch (OperationCanceledException)
            {
                return ProxyResponses.TechnicalError<IEnumerable<BcpRowDto>>("Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponses.TechnicalError<IEnumerable<BcpRowDto>>($"Nieoczekiwany błąd: {ex.Message}", source, "500", requestId);
            }
        }

        /// <summary>
        /// ANPRS – słownik Systems (lista domenowych obiektów SystemRowDto).
        /// </summary>
        [HttpGet("dictionary/systems")]
        [Produces(typeof(ProxyResponse<IEnumerable<SystemRowDto>>))]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<SystemRowDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<SystemRowDto>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<SystemRowDto>>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "ANPRS – słownik Systems",
            Description = "Zwraca listę Systemów jako domenowe DTO (SystemRowDto) opakowaną w ProxyResponse."
        )]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401Example))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401ValidAuthRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403HttpsRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertInvalidExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code400InvalidParameterExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code404NotFoundExample))]
        public async Task<ProxyResponse<IEnumerable<SystemRowDto>>> GetSystems([FromQuery] string country, CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ANPRS";

            try
            {
                var data = await _facade.GetSystemsAsync(country, ct);
                if (data is null)
                    return ProxyResponses.TechnicalError<IEnumerable<SystemRowDto>>("Pusta odpowiedź z ANPRS.", source, "502", requestId);

                return ProxyResponses.Success<IEnumerable<SystemRowDto>>(data, source, "200", requestId);
            }
            catch (ANPRSHttpException ex)
            {
                var (code, message) = ExtractHttpCodeAndMessage(ex);
                return ProxyResponses.BusinessError<IEnumerable<SystemRowDto>>(message ?? $"ANPRS HTTP {code}.", source, code.ToString(), requestId);
            }
            catch (OperationCanceledException)
            {
                return ProxyResponses.TechnicalError<IEnumerable<SystemRowDto>>("Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponses.TechnicalError<IEnumerable<SystemRowDto>>($"Nieoczekiwany błąd: {ex.Message}", source, "500", requestId);
            }
        }

        [HttpPost("dictionary/countries/update")]
        [SwaggerOperation(Summary = "Zapis countries do DB")]
        public async Task<IActionResult> UpdateCountries(CancellationToken ct)
        {
            await _facade.SaveCountriesToDbAsync(ct);
            return Ok("Słownik krajów został pobrany i zapisany w lokalnym repozytorium.");
        }

        [HttpPost("dictionary/bcp/update")]
        [SwaggerOperation(Summary = "Zapis BCP do DB")]
        public async Task<IActionResult> UpdateBcp(CancellationToken ct)
        {
            await _facade.SaveBcpToDbAsync(ct);
            return Ok("Słownik BCP został pobrany i zapisany w lokalnym repozytorium.");
        }

        [HttpPost("dictionary/systems/update")]
        [SwaggerOperation(Summary = "Zapis Systems do DB dla wskazanego kraju")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> UpdateSystems([FromQuery] string country, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(country))
                return BadRequest("Parametr 'country' jest wymagany (np. PLN/EST/LTV/LVA).");

            await _facade.SaveSystemsToDbAsync(country, ct);
            return Ok($"Słownik Systemów został pobrany i zapisany w lokalnym repozytorium dla kraju={country.ToUpperInvariant()}.");
        }

        [HttpGet("dictionary/systems/local")]
        [Produces(typeof(ProxyResponse<IEnumerable<ANPRSSystemsResponse>>))]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<ANPRSSystemsResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<ANPRSSystemsResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<ANPRSSystemsResponse>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<ANPRSSystemsResponse>>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "Systems – odczyt z lokalnej bazy (anprs.Systems)",
            Description = "Zwraca listę systemów (Nazwa, Opis) dla wskazanego kraju z DB."
        )]
        public async Task<ProxyResponse<IEnumerable<ANPRSSystemsResponse>>> GetSystemsLocal([FromQuery] string country, CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ANPRS.LOCAL";

            try
            {
                if (string.IsNullOrWhiteSpace(country))
                    return ProxyResponses.BusinessError<IEnumerable<ANPRSSystemsResponse>>(
                        "Parametr 'country' jest wymagany (np. PLN/EST/LTV/LVA).",
                        source, StatusCodes.Status400BadRequest.ToString(), requestId);

                var cc = country.Trim().ToUpperInvariant();

                // 1) pobierz wszystkie wiersze z facady (DB)
                var rows = await _facade.GetSystemsLocalAsync(cc, ct);

                // 2) zmapuj na listę prostych DTO (Nazwa, Opis)
                var list = rows.Select(r => new ANPRSSystemsResponse(
                    Nazwa: r.SystemCode,
                    Opis: r.Description ?? string.Empty
                )).ToList();

                // 3) pusta lista -> 404
                if (list.Count == 0)
                    return ProxyResponses.BusinessError<IEnumerable<ANPRSSystemsResponse>>(
                        $"Brak danych Systems dla country={cc}.",
                        source, StatusCodes.Status404NotFound.ToString(), requestId);

                // 4) OK
                return ProxyResponses.Success<IEnumerable<ANPRSSystemsResponse>>(
                    list, source, StatusCodes.Status200OK.ToString(), requestId);
            }
            catch (OperationCanceledException)
            {
                return ProxyResponses.TechnicalError<IEnumerable<ANPRSSystemsResponse>>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponses.TechnicalError<IEnumerable<ANPRSSystemsResponse>>(
                    $"Nieoczekiwany błąd: {ex.Message}", source, StatusCodes.Status500InternalServerError.ToString(), requestId);
            }
        }
        #endregion

        #region Reports endpoints
        /// <summary>
        /// Raport: Pobierz zdarzenia w punkcie (VehiclesInPointWithGeo).
        /// </summary>
        /// <param name="country">Kod kraju (np. PLN/EST/LTV/LVA).</param>
        /// <param name="system">Kod systemu (np. OCR, ANPRS, BIA...)</param>
        /// <param name="bcp">Identyfikator punktu BCP (np. OCRB02IN).</param>
        /// <param name="dateFrom">Data od (format: yyyy-MM-dd HH:mm:ss).</param>
        /// <param name="dateTo">Data do (format: yyyy-MM-dd HH:mm:ss).</param>
        [HttpGet("reports/vehicles-in-point")]
        [Produces(typeof(ProxyResponse<VehiclesInPointResponse>))]
        [ProducesResponseType(typeof(ProxyResponse<VehiclesInPointResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<VehiclesInPointResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<VehiclesInPointResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProxyResponse<VehiclesInPointResponse>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "ANPRS – Pobierz zdarzenia w punkcie",
            Description = "Wywołuje /api/Reports/VehiclesInPointWithGeo i zwraca wynik w układzie columnsNames + data (ANPRSGridResponse) opakowany w ProxyResponse."
        )]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401Example))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401ValidAuthRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403HttpsRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertInvalidExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code400InvalidParameterExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code404NotFoundExample))]
        public async Task<ProxyResponse<VehiclesInPointResponse>> GetVehiclesInPoint(
            [FromQuery] string country,
            [FromQuery] string system,
            [FromQuery] string bcp,
            [FromQuery] DateTime dateFrom,
            [FromQuery] DateTime dateTo,
            CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ANPRS";

            // Walidacja prostych parametrów
            if (string.IsNullOrWhiteSpace(country) ||
                string.IsNullOrWhiteSpace(system) ||
                string.IsNullOrWhiteSpace(bcp))
            {
                return ProxyResponses.BusinessError<VehiclesInPointResponse>(
                    "Parametry 'country', 'system' i 'bcp' są wymagane.",
                    source, StatusCodes.Status400BadRequest.ToString(), requestId);
            }

            try
            {
                var resp = await _reports.GetVehiclesInPointWithGeoAsync(
                    country.Trim(), system.Trim(), bcp.Trim(), dateFrom, dateTo, ct);

                if (resp is null)
                    return ProxyResponses.BusinessError<VehiclesInPointResponse>(
                        "Pusta odpowiedź z ANPRS.", source, StatusCodes.Status502BadGateway.ToString(), requestId);

                return ProxyResponses.Success(resp, source, StatusCodes.Status200OK.ToString(), requestId);
            }
            catch (ANPRSHttpException ex)
            {
                var (code, message) = ExtractHttpCodeAndMessage(ex);
                return ProxyResponses.BusinessError<VehiclesInPointResponse>(
                    message ?? $"ANPRS HTTP {code}.", source, code.ToString(), requestId);
            }
            catch (OperationCanceledException)
            {
                return ProxyResponses.TechnicalError<VehiclesInPointResponse>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponses.TechnicalError<VehiclesInPointResponse>(
                    $"Nieoczekiwany błąd: {ex.Message}", source, "500", requestId);
            }
        }


        /// <summary>
        /// Raport: Pobierz zdarzenia (LicenseplateWithGeo).
        /// </summary>
        /// <param name="numberPlate">Numer rejestracyjny (np. 236TGO).</param>
        /// <param name="dateFrom">Data od (format: yyyy-MM-dd).</param>
        /// <param name="dateTo">Data do (format: yyyy-MM-dd).</param>
        [HttpGet("reports/license-plate")]
        [Produces(typeof(ProxyResponse<LicensePlateReportResponse>))]
        [ProducesResponseType(typeof(ProxyResponse<LicensePlateReportResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<LicensePlateReportResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<LicensePlateReportResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProxyResponse<LicensePlateReportResponse>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "ANPRS – Pobierz zdarzenia (po numerze rejestracyjnym)",
            Description = "Wywołuje /api/Reports/LicenseplateWithGeo i zwraca wynik w układzie columnsNames + data (ANPRSGridResponse) opakowany w ProxyResponse."
        )]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401Example))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401ValidAuthRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403HttpsRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertInvalidExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code400InvalidParameterExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code404NotFoundExample))]
        public async Task<ProxyResponse<LicensePlateReportResponse>> GetLicensePlate(
            [FromQuery] string numberPlate,
            [FromQuery] DateTime dateFrom,
            [FromQuery] DateTime dateTo,
            CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ANPRS";

            // Walidacja wejścia
            if (string.IsNullOrWhiteSpace(numberPlate))
            {
                return ProxyResponses.BusinessError<LicensePlateReportResponse>(
                    "Parametr 'numberPlate' jest wymagany.", source,
                    StatusCodes.Status400BadRequest.ToString(), requestId);
            }
            if (dateFrom > dateTo)
            {
                return ProxyResponses.BusinessError<LicensePlateReportResponse>(
                    "Parametr 'dateFrom' nie może być większy niż 'dateTo'.", source,
                    StatusCodes.Status400BadRequest.ToString(), requestId);
            }

            try
            {
                var resp = await _reports.GetLicensePlateWithGeoAsync(
                    numberPlate.Trim(), dateFrom, dateTo, ct);

                if (resp is null)
                    return ProxyResponses.BusinessError<LicensePlateReportResponse>(
                        "Pusta odpowiedź z ANPRS.", source, StatusCodes.Status502BadGateway.ToString(), requestId);

                return ProxyResponses.Success(resp, source, StatusCodes.Status200OK.ToString(), requestId);
            }
            catch (ANPRSHttpException ex)
            {
                var (code, message) = ExtractHttpCodeAndMessage(ex);
                return ProxyResponses.BusinessError<LicensePlateReportResponse>(
                    message ?? $"ANPRS HTTP {code}.", source, code.ToString(), requestId);
            }
            catch (OperationCanceledException)
            {
                return ProxyResponses.TechnicalError<LicensePlateReportResponse>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponses.TechnicalError<LicensePlateReportResponse>(
                    $"Nieoczekiwany błąd: {ex.Message}", source, "500", requestId);
            }
        }
        #endregion

        /// <summary>
        /// Wydobywa kod HTTP oraz komunikat "message" z treści wyjątku pochodzącego z ANPRSHttpClient.
        /// </summary>
        private static (int Code, string? Message) ExtractHttpCodeAndMessage(ANPRSHttpException ex)
        {
            string? body = null;
            var marker = "Body:"; // przyjmujemy konwencję formatowania wyjątku przez ANPRSHttpClient
            var msg = ex.Message ?? string.Empty;
            var idx = msg.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                body = msg[(idx + marker.Length)..].Trim();
            }

            string? extractedMessage = null;
            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                        extractedMessage = m.GetString();
                    else
                        extractedMessage = body; // brak pola message – zwróć całe body
                }
                catch
                {
                    extractedMessage = body; // body nie jest JSON-em – zwróć surową treść
                }
            }

            return (ex.StatusCode, extractedMessage);
        }
    }
}
