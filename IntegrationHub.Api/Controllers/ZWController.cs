using IntegrationHub.Application.ZW;              // IZWSourceFacade
using IntegrationHub.Common.Contracts;           // ProxyResponse, ProxyResponses
using IntegrationHub.Common.RequestValidation;   // IRequestValidator<T>, ValidationResult
using IntegrationHub.Domain.Contracts.ZW;        // WPMRequest, WPMResponse
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace IntegrationHub.Api.Controllers
{
    [ApiController]
    [Route("ZW")]
    [Produces("application/json")]
    public sealed class ZWController : ControllerBase
    {
        private readonly IZWSourceFacade _facade;
        private readonly IRequestValidator<WPMRequest> _validator;

        public ZWController(IZWSourceFacade facade, IRequestValidator<WPMRequest> validator)
        {
            _facade = facade;
            _validator = validator;
        }

        [HttpGet("wpm/szukaj")]
        [Produces(typeof(ProxyResponse<IEnumerable<WPMResponse>>))]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<WPMResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<WPMResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<WPMResponse>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<WPMResponse>>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "WPM – wyszukiwanie pojazdów",
            Description = "Najpierw liczy potencjalne rekordy, a dopiero gdy nie przekraczają progu – zwraca wynik."
        )]
        public async Task<ProxyResponse<IEnumerable<WPMResponse>>> SearchWPMAsync(
            [FromQuery] string? nrRejestracyjny,
            [FromQuery] string? numerPodwozia,
            [FromQuery] string? nrSerProducenta,
            [FromQuery] string? nrSerSilnika,
            CancellationToken ct = default)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ZW";
            const int vehiclesLimit = 20; // TODO: w kolejnym kroku przenieść do appsettings

            try
            {
                static string? Norm(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

                var req = new WPMRequest
                {
                    NrRejestracyjny = Norm(nrRejestracyjny),
                    NumerPodwozia = Norm(numerPodwozia),
                    NrSerProducenta = Norm(nrSerProducenta),
                    NrSerSilnika = Norm(nrSerSilnika)
                };

                // Walidacja wejścia
                var vr = _validator.ValidateAndNormalize(req);
                if (!vr.IsValid)
                {
                    return ProxyResponses.BusinessError<IEnumerable<WPMResponse>>(
                        message: vr.MessageError!,
                        source: source,
                        sourceStatusCode: StatusCodes.Status400BadRequest.ToString(),
                        requestId: requestId);
                }

                // 1) pre-count
                var count = await _facade.CountVehiclesAsync(req, ct);

                if (count == 0)
                {
                    return ProxyResponses.BusinessError<IEnumerable<WPMResponse>>(
                        message: "Nie znaleziono pojazdów spełniających zadane kryteria.",
                        source: source,
                        sourceStatusCode: StatusCodes.Status404NotFound.ToString(),
                        requestId: requestId);
                }

                if (count > vehiclesLimit)
                {
                    return ProxyResponses.BusinessError<IEnumerable<WPMResponse>>(
                        message: $"Znaleziono więcej niż {vehiclesLimit} pojazdów. Popraw kryteria.",
                        source: source,
                        sourceStatusCode: StatusCodes.Status400BadRequest.ToString(),
                        requestId: requestId);
                }

                // 2) pobierz dane (count w granicach limitu)
                var rows = (await _facade.SearchAsync(req, ct))?.ToList() ?? new List<WPMResponse>();
                if (rows.Count == 0)
                {
                    // Nie powinno się zdarzyć po pre-count, ale zachowujemy bezpieczną ścieżkę
                    return ProxyResponses.BusinessError<IEnumerable<WPMResponse>>(
                        message: "Nie znaleziono pojazdów spełniających zadane kryteria.",
                        source: source,
                        sourceStatusCode: StatusCodes.Status404NotFound.ToString(),
                        requestId: requestId);
                }

                // Sukces z komunikatem "Znaleziono {count} pojazdów."
                var successMessage = $"Znalezione pojazdy: {count}";
                return new ProxyResponse<IEnumerable<WPMResponse>>
                {
                    Data = rows,
                    Status = 0,
                    Message = successMessage,
                    Source = source,
                    SourceStatusCode = StatusCodes.Status200OK.ToString(),
                    RequestId = requestId
                };
            }
            catch (OperationCanceledException)
            {
                return ProxyResponses.TechnicalError<IEnumerable<WPMResponse>>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponses.TechnicalError<IEnumerable<WPMResponse>>(
                    $"Nieoczekiwany błąd: {ex.Message}", source, StatusCodes.Status500InternalServerError.ToString(), requestId);
            }
        }

    }
}
