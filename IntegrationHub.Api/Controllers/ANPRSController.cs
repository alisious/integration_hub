using IntegrationHub.Api.Swagger.Examples.ANPRS;
using IntegrationHub.Common.Contracts;                       // ProxyResponse, ProxyResponses, ProxyStatus
using IntegrationHub.Common.Exceptions;         // Countries401Example, ...Countries404Example
using IntegrationHub.Sources.ANPRS.Client;                  // ANPRSHttpException (rzucany przez ANPRSHttpClient)
using IntegrationHub.Sources.ANPRS.Contracts;               // DictionaryResponse
using IntegrationHub.Sources.ANPRS.Services;                // IANPRSDictionaryService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;                       // SwaggerResponseExample
using System.Net;
using System.Text.Json;

// Examples:
// using IntegrationHub.Api.Swagger.Examples.ANPRS;         // Countries401Example, ...Countries404Example

namespace IntegrationHub.Api.Controllers
{
    [ApiController]
    [Route("ANPRS")]
    [Produces("application/json")]
   // [Authorize(Roles = "User")]
    public sealed class ANPRSController : ControllerBase
    {
        private readonly IANPRSDictionaryService _dict;

        public ANPRSController(IANPRSDictionaryService dict) => _dict = dict;

        /// <summary>
        /// ANPRS – słownik krajów.
        /// Zwraca tabelaryczny wynik (columnsNames + data) opakowany w ProxyResponse.
        /// </summary>
        /// <remarks>
        /// W przypadku błędu po stronie ANPRS (HTTP &ge; 400) kod HTTP i treść pola <c>message</c>
        /// są mapowane na <b>BusinessError</b> w ProxyResponse.
        /// </remarks>
        [HttpGet("dictionary/countries")]
        [Produces(typeof(ProxyResponse<DictionaryResponse>))]
        [ProducesResponseType(typeof(ProxyResponse<DictionaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<DictionaryResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<DictionaryResponse>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "ANPRS – słownik krajów",
            Description = "Pobiera listę krajów (ANPRS /api/Dictionary?type=countries) i mapuje wynik do ProxyResponse."
        )]
        // Example’y BusinessError (API proxy zawsze zwraca 200 z ProxyStatus=BusinessError; kod źródła w SourceStatusCode):
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401Example))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401ValidAuthRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403HttpsRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertInvalidExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code400InvalidParameterExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code404NotFoundExample))]
        public async Task<ProxyResponse<DictionaryResponse>> GetCountries(CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ANPRS";

            try
            {
                var data = await _dict.GetDictionaryAsync("countries", ct);
                if (data is null)
                {
                    return ProxyResponses.TechnicalError<DictionaryResponse>(
                        "Pusta odpowiedź z ANPRS.", source, "502", requestId);
                }

                return ProxyResponses.Success(data, source, "200", requestId);
            }
            catch (ANPRSHttpException ex)
            {
                var (code, message) = ExtractHttpCodeAndMessage(ex);
                return ProxyResponses.BusinessError<DictionaryResponse>(
                    message ?? $"ANPRS HTTP {code}.", source, code.ToString(), requestId);
            }
            catch (OperationCanceledException)
            {
                return ProxyResponses.TechnicalError<DictionaryResponse>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (CertificateException ex)
            {
                return ProxyResponses.BusinessError<DictionaryResponse>(
                    ex.Message, source, HttpStatusCode.BadRequest.ToString(), requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponses.TechnicalError<DictionaryResponse>(
                    $"Nieoczekiwany błąd: {ex.Message}", source, "500", requestId);
            }
        }


        /// <summary>
        /// ANPRS – słownik BCP.
        /// Zwraca tabelaryczny wynik (columnsNames + data) opakowany w ProxyResponse.
        /// </summary>
        /// <remarks>
        /// W przypadku błędu po stronie ANPRS (HTTP ≥ 400) kod HTTP i treść pola <c>message</c>
        /// są mapowane na <b>BusinessError</b> w ProxyResponse.
        /// </remarks>
        [HttpGet("dictionary/bcp")]
        [Produces(typeof(ProxyResponse<DictionaryResponse>))]
        [ProducesResponseType(typeof(ProxyResponse<DictionaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<DictionaryResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<DictionaryResponse>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "ANPRS – słownik BCP",
            Description = "Pobiera listę BCP (ANPRS /api/Dictionary?type=bcp) i mapuje wynik do ProxyResponse."
        )]
        // Example’y BusinessError (API proxy zwraca 200 z ProxyStatus=BusinessError; kod źródła w SourceStatusCode):
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401Example))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code401ValidAuthRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403HttpsRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertRequiredExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code403ClientCertInvalidExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code400InvalidParameterExample))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(Code404NotFoundExample))]
        public async Task<ProxyResponse<DictionaryResponse>> GetBcp(CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ANPRS";

            try
            {
                var data = await _dict.GetDictionaryAsync("bcp", ct);
                if (data is null)
                {
                    return ProxyResponses.TechnicalError<DictionaryResponse>(
                        "Pusta odpowiedź z ANPRS.", source, "502", requestId);
                }

                return ProxyResponses.Success(data, source, "200", requestId);
            }
            catch (ANPRSHttpException ex)
            {
                var (code, message) = ExtractHttpCodeAndMessage(ex);
                return ProxyResponses.BusinessError<DictionaryResponse>(
                    message ?? $"ANPRS HTTP {code}.", source, code.ToString(), requestId);
            }
            catch (OperationCanceledException)
            {
                return ProxyResponses.TechnicalError<DictionaryResponse>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponses.TechnicalError<DictionaryResponse>(
                    $"Nieoczekiwany błąd: {ex.Message}", source, "500", requestId);
            }
        }



        /// <summary>
        /// Wydobywa kod HTTP oraz komunikat "message" z treści wyjątku pochodzącego z ANPRSHttpClient.
        /// </summary>
        private static (int Code, string? Message) ExtractHttpCodeAndMessage(ANPRSHttpException ex)
        {
            // ANPRSHttpException ma StatusCode i w Message/body zwraca treść odpowiedzi ANPRS.
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
