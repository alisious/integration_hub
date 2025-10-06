// IntegrationHub.Sources.CEP/Controllers/CEPUdostepnianieController.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using IntegrationHub.Common.Contracts;                       // ProxyResponse<T>, ProxyStatus
using IntegrationHub.Sources.CEP.Udostepnianie.Services;                   // ICEPUdostepnianieService
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;    // PytanieOPojazdRequest, PytanieOPojazdResponse

namespace IntegrationHub.Sources.CEP.Controllers
{
    [ApiController]
    [Route("CEP/udostepnianie")]
    [Produces("application/json")]
    public sealed class CEPUdostepnianieController : ControllerBase
    {
        private readonly ICEPUdostepnianieService _service;
        private readonly ILogger<CEPUdostepnianieController> _logger;

        public CEPUdostepnianieController(
            ICEPUdostepnianieService service,
            ILogger<CEPUdostepnianieController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Wywołuje usługę CEPIK – Pytanie o pojazd.
        /// Zwraca bezpośrednio ProxyResponse&lt;PytanieOPojazdResponse&gt;.
        /// </summary>
        [HttpPost("pytanie-o-pojazd")]
        [Consumes("application/json")]
        [Produces(typeof(ProxyResponse<PytanieOPojazdResponse>))]
        public async Task<ProxyResponse<PytanieOPojazdResponse>> PytanieOPojazd(
            [FromBody] PytanieOPojazdRequest body,
            CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                var result = await _service.PytanieOPojazdAsync(body, requestId, ct);

                if (result.Status != ProxyStatus.Success)
                {
                    _logger.LogWarning(
                        "CEPUdostepnianie.PytanieOPojazd: status={ProxyStatus}, http={Http}, reqId={ReqId}",
                        result.Status, result.SourceStatusCode, result.RequestId);
                }

                return result;
            }
            catch (Exception ex)
            {
                // Fallback na wypadek nieoczekiwanego wyjątku, który "uciekł" z warstwy serwisu
                _logger.LogError(ex, "Nieoczekiwany błąd w CEPUdostepnianieController.PytanieOPojazd. reqId={ReqId}", requestId);

                return new ProxyResponse<PytanieOPojazdResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Controller",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = 500,
                    ErrorMessage = "Wystąpił nieoczekiwany błąd po stronie API."
                };
            }
        }
    }
}
