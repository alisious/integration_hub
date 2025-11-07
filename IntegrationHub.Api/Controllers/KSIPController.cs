// IntegrationHub.Sources.KSIP/Controllers/KSIPController.cs
using IntegrationHub.Common.Contracts;                           // ProxyResponse<T>, ProxyStatus
using IntegrationHub.Sources.KSIP.Contracts;                     // SprawdzenieOsobyWRuchuDrogowymRequest/Response
using IntegrationHub.Sources.KSIP.Services;                      // ISprawdzenieOsobyWRuchuDrogowymService
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;                        // SwaggerOperation
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationHub.Sources.KSIP.Controllers
{
    [ApiController]
    [Route("KSIP")]
    [Produces("application/json")]
    public sealed class KSIPController : ControllerBase
    {
        private readonly IKSIPService _service;
        private readonly ILogger<KSIPController> _logger;

        public KSIPController(
            IKSIPService service,
            ILogger<KSIPController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// KSIP – Sprawdzenie osoby w ruchu drogowym.
        /// Zwraca <see cref="ProxyResponse{T}"/> z danymi osoby i ewentualnymi wpisami mandatowymi/wykroczeń.
        /// </summary>
        /// <remarks>
        /// <b>Minimalne kryteria zapytania</b><br/>
        /// Zapytanie jest akceptowane, gdy spełniony jest <u>jeden</u> z warunków:
        /// <ul>
        ///   <li><code>userId</code> <i>i</i> <code>nrPesel</code>, <b>albo</b></li>
        ///   <li><code>userId</code> <i>oraz</i> <code>firstName</code>, <code>lastName</code>, <code>birthDate</code>.</li>
        /// </ul>
        /// Format <code>birthDate</code>: <c>yyyy-MM-dd</c>.<br/>
        /// Wszystkie pola są trimowane; <code>nrPesel</code> normalizowany do 11 cyfr.
        /// </remarks>
        [HttpPost("sprawdzenie-osoby-w-ruchu-drogowym")]
        [Consumes("application/json")]
        [Produces(typeof(ProxyResponse<SprawdzenieOsobyWRuchuDrogowymResponse>))]
        [ProducesResponseType(typeof(ProxyResponse<SprawdzenieOsobyWRuchuDrogowymResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<SprawdzenieOsobyWRuchuDrogowymResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<SprawdzenieOsobyWRuchuDrogowymResponse>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "KSIP – Sprawdzenie osoby w ruchu drogowym",
            Description =
                "<b>Minimalne kryteria zapytania</b><br/>" +
                "<ul>" +
                "<li><code>userId</code> i <code>nrPesel</code> <b>lub</b></li>" +
                "<li><code>userId</code> oraz <code>firstName</code>, <code>lastName</code>, <code>birthDate</code>.</li>" +
                "</ul>" +
                "Data <code>birthDate</code> w formacie <code>yyyy-MM-dd</code>. " +
                "Wszystkie pola są trimowane; <code>nrPesel</code> normalizowany do 11 cyfr."
        )]
        public async Task<ProxyResponse<SprawdzenieOsobyWRuchuDrogowymResponse>> SprawdzenieOsobyWRuchuDrogowym(
            [FromBody] SprawdzenieOsobyWRuchuDrogowymRequest body,
            CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                var result = await _service.SprawdzenieOsobyWRuchuDrogowymAsync(body, requestId, ct);

                if (result.Status != ProxyStatus.Success)
                {
                    _logger.LogWarning(
                        "KSIP.SprawdzenieOsoby: status={Status}, http={Http}, reqId={ReqId}",
                        result.Status, result.SourceStatusCode, result.RequestId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd w KSIPController.SprawdzenieOsobyWRuchuDrogowym. reqId={ReqId}", requestId);

                return new ProxyResponse<SprawdzenieOsobyWRuchuDrogowymResponse>
                {
                    RequestId = requestId,
                    Source = "KSIP",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = "500",
                    Message = "Wystąpił nieoczekiwany błąd po stronie API."
                };
            }
        }
    }
}