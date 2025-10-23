using IntegrationHub.Api.Swagger.Examples.ANPRS;
using IntegrationHub.Application.ANPRS;                    // IANPRSDictionaryFacade, IANPRSReportsFacade
using IntegrationHub.Common.Contracts;                   // ProxyResponse, ProxyResponses, ProxyStatus
using IntegrationHub.Common.Exceptions;                  // Countries401Example, ...Countries404Example
using IntegrationHub.Domain.Contracts.ANPRS;
using IntegrationHub.Sources.ANPRS.Client;                // ANPRSHttpException (rzucany przez ANPRSHttpClient)
using IntegrationHub.Sources.ANPRS.Contracts;             // DictionaryResponse (used for swagger examples)
using IntegrationHub.Sources.ANPRS.Contracts.IntegrationHub.Sources.ANPRS.Contracts;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;                     // SwaggerResponseExample
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

namespace IntegrationHub.Api.Controllers
{
    [ApiController]
    [Route("ANPRS")]
    [Produces("application/json")]
    public sealed class ANPRSController : ControllerBase
    {
        private readonly IANPRSDictionaryFacade _facade;
        private readonly IANPRSReportsFacade _reports;

        public ANPRSController(IANPRSDictionaryFacade facade, IANPRSReportsFacade reportsFacade)
        {
            _facade = facade;
            _reports = reportsFacade;
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
        [Produces(typeof(ProxyResponse<IEnumerable<SystemRowDto>>))]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<SystemRowDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<SystemRowDto>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<SystemRowDto>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<SystemRowDto>>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "Systems – odczyt z lokalnej bazy (anprs.Systems)",
            Description = "Zwraca listę systemów (SystemRowDto) dla wskazanego kraju z DB."
        )]
        public async Task<ProxyResponse<IEnumerable<SystemRowDto>>> GetSystemsLocal([FromQuery] string country, CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ANPRS.LOCAL";

            try
            {
                if (string.IsNullOrWhiteSpace(country))
                    return ProxyResponses.BusinessError<IEnumerable<SystemRowDto>>(
                        "Parametr 'country' jest wymagany (np. PLN/EST/LTV/LVA).",
                        source, StatusCodes.Status400BadRequest.ToString(), requestId);

                var cc = country.Trim().ToUpperInvariant();

                // 1) pobierz wszystkie wiersze z facady (DB)
                var rows = (await _facade.GetSystemsLocalAsync(cc, ct))?.ToList();

                // 2) pusta lista -> 404
                if (rows is null || rows.Count == 0)
                    return ProxyResponses.BusinessError<IEnumerable<SystemRowDto>>(
                        $"Brak danych Systems dla country={cc}.",
                        source, StatusCodes.Status404NotFound.ToString(), requestId);

                // 3) OK – zwracamy bezpośrednio SystemRowDto
                return ProxyResponses.Success<IEnumerable<SystemRowDto>>(
                    rows, source, StatusCodes.Status200OK.ToString(), requestId);
            }
            catch (OperationCanceledException)
            {
                return ProxyResponses.TechnicalError<IEnumerable<SystemRowDto>>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponses.TechnicalError<IEnumerable<SystemRowDto>>(
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
        [Produces(typeof(ProxyResponse<IEnumerable<VehicleInPointDto>>))]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<VehicleInPointDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<VehicleInPointDto>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<VehicleInPointDto>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<VehicleInPointDto>>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "ANPRS – Pobierz zdarzenia w punkcie",
            Description = "Wywołuje /api/Reports/VehiclesInPointWithGeo, mapuje wynik na domenowe DTO VehicleInPointDto i zwraca kolekcję opakowaną w ProxyResponse."
        )]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401Example))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401ValidAuthRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403HttpsRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertInvalidExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code400InvalidParameterExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code404NotFoundExample))]
        public async Task<ProxyResponse<IEnumerable<VehicleInPointDto>>> GetVehiclesInPoint(
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
                return ProxyResponses.BusinessError<IEnumerable<VehicleInPointDto>>(
                    "Parametry 'country', 'system' i 'bcp' są wymagane.",
                    source, StatusCodes.Status400BadRequest.ToString(), requestId);
            }

            try
            {
                // Use facade which returns domain DTOs
                var list = (await _reports.GetVehiclesInPointAsync(
                    country.Trim(), system.Trim(), bcp.Trim(), dateFrom, dateTo, ct))?.ToList();

                if (list is null)
                    return ProxyResponses.BusinessError<IEnumerable<VehicleInPointDto>>(
                        "Pusta odpowiedź z ANPRS.", source, StatusCodes.Status502BadGateway.ToString(), requestId);

                return ProxyResponses.Success<IEnumerable<VehicleInPointDto>>(list, source, StatusCodes.Status200OK.ToString(), requestId);
            }
            catch (ANPRSHttpException ex)
            {
                var (code, message) = ExtractHttpCodeAndMessage(ex);
                return ProxyResponses.BusinessError<IEnumerable<VehicleInPointDto>>(
                    message ?? $"ANPRS HTTP {code}.", source, code.ToString(), requestId);
            }
            catch (OperationCanceledException)
            {
                return ProxyResponses.TechnicalError<IEnumerable<VehicleInPointDto>>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponses.TechnicalError<IEnumerable<VehicleInPointDto>>(
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
        [Produces(typeof(ProxyResponse<IEnumerable<LicenseplateDto>>))]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<LicenseplateDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<LicenseplateDto>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<LicenseplateDto>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<LicenseplateDto>>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "ANPRS – Pobierz zdarzenia (po numerze rejestracyjnym)",
            Description = "Wywołuje /api/Reports/LicenseplateWithGeo, mapuje wynik na domenowe DTO LicenseplateDto i zwraca kolekcję opakowaną w ProxyResponse."
        )]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401Example))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401ValidAuthRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403HttpsRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertInvalidExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code400InvalidParameterExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code404NotFoundExample))]
        public async Task<ProxyResponse<IEnumerable<LicenseplateDto>>> GetLicensePlate(
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
                return ProxyResponses.BusinessError<IEnumerable<LicenseplateDto>>(
                    "Parametr 'numberPlate' jest wymagany.", source,
                    StatusCodes.Status400BadRequest.ToString(), requestId);
            }
            if (dateFrom > dateTo)
            {
                return ProxyResponses.BusinessError<IEnumerable<LicenseplateDto>>(
                    "Parametr 'dateFrom' nie może być większy niż 'dateTo'.", source,
                    StatusCodes.Status400BadRequest.ToString(), requestId);
            }

            try
            {
                var list = (await _reports.GetLicensePlateWithGeoAsync(
                    numberPlate.Trim(), dateFrom, dateTo, ct))?.ToList();

                if (list is null)
                    return ProxyResponses.BusinessError<IEnumerable<LicenseplateDto>>(
                        "Pusta odpowiedź z ANPRS.", source, StatusCodes.Status502BadGateway.ToString(), requestId);

                return ProxyResponses.Success<IEnumerable<LicenseplateDto>>(list, source, StatusCodes.Status200OK.ToString(), requestId);
            }
            catch (ANPRSHttpException ex)
            {
                var (code, message) = ExtractHttpCodeAndMessage(ex);
                return ProxyResponses.BusinessError<IEnumerable<LicenseplateDto>>(
                    message ?? $"ANPRS HTTP {code}.", source, code.ToString(), requestId);
            }
            catch (OperationCanceledException)
            {
                return ProxyResponses.TechnicalError<IEnumerable<LicenseplateDto>>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponses.TechnicalError<IEnumerable<LicenseplateDto>>(
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
