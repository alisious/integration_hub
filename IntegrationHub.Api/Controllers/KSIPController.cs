using IntegrationHub.Common.Contracts;          // ProxyResponse<T>, ProxyStatus
using IntegrationHub.Common.Primitives;         // Result<T,Error>, Error
using IntegrationHub.Common.RequestValidation;  // ValidationResult, ToProxyResponse()
using IntegrationHub.Sources.KSIP.Config;       // KSIPConfig
using IntegrationHub.Sources.KSIP.Contracts;    // SprawdzenieOsobyRequest, SprawdzenieOsobyResponse
using IntegrationHub.Sources.KSIP.RequestValidation;
using IntegrationHub.Sources.KSIP.Services;     // IKSIPService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace IntegrationHub.Sources.KSIP.Controllers
{
    [ApiController]
    [Route("KSIP")]
    [Produces("application/json")]
    [Authorize(Roles = "User")]
    public sealed class KSIPController : ControllerBase
    {
        private readonly IKSIPService _service;
        private readonly ILogger<KSIPController> _logger;
        private readonly string _sourceName;

        public KSIPController(
            IKSIPService service,
            KSIPConfig config,
            ILogger<KSIPController> logger)
        {
            _service = service;
            _logger = logger;

            _sourceName = string.IsNullOrWhiteSpace(config?.ServiceName)
                ? "KSIP"
                : config.ServiceName;
        }

        /// <summary>
        /// KSIP – Sprawdzenie osoby w ruchu drogowym.
        /// </summary>
        [HttpPost("sprawdzenie-osoby-w-ruchu-drogowym")]
        [Consumes("application/json")]
        [Produces(typeof(ProxyResponse<SprawdzenieOsobyResponse>))]
        [SwaggerOperation(
            Summary = "KSIP – Sprawdzenie osoby w ruchu drogowym",
            Description =
                "<b>Minimalne kryteria zapytania</b><br/>" +
                "<ul>" +
                "<li><code>userId</code> oraz <code>nrPesel</code> <b>lub</b></li>" +
                "<li><code>userId</code> oraz <code>firstName</code>, <code>lastName</code>, <code>birthDate</code>.</li>" +
                "</ul>" +
                "<b>Format daty:</b> <code>yyyy-MM-dd</code>.<br/><br/>" +
                "<b>Przykłady testowe (KSIPServiceTest)</b><ul>" +
                "<li><code>nrPesel = \"03290901192\"</code> → plik <i>dziodko_RESPONSE.xml</i></li>" +
                "<li><code>firstName=\"MATEUSZ\"</code>, <code>lastName=\"DZIODKO\"</code>, <code>birthDate=\"2003-09-09\"</code></li>" +
                "<li><code>nrPesel = \"81040709629\"</code> → plik <i>OsobaJest_State0_RESPONSE.xml</i></li>" +
                "<li><code>firstName=\"EDYTA\"</code>, <code>lastName=\"KOROLCZUK\"</code>, <code>birthDate=\"1981-04-07\"</code></li>" +
                "<li>Inne parametry → <i>NotFound_RESPONSE.xml</i></li>" +
                "</ul>"
        )]
        public async Task<ProxyResponse<SprawdzenieOsobyResponse>> SprawdzenieOsobyWRuchuDrogowym(
            [FromBody] SprawdzenieOsobyRequest body,
            CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                //
                // 1) Body null → błąd biznesowy
                //
                if (body is null)
                {
                    return ProxyResponses.BusinessError<SprawdzenieOsobyResponse>(
                        "Body (SprawdzenieOsobyRequest) nie może być null.",
                        _sourceName,
                        StatusCodes.Status400BadRequest.ToString(),
                        requestId);
                }

                //
                // 2) Walidacja + normalizacja
                //
                var validator = new SprawdzenieOsobyRequestValidator();
                var vr = validator.ValidateAndNormalize(body);

                if (!vr.IsValid)
                {
                    var baseResp = vr.ToProxyResponse(_sourceName, requestId);

                    return new ProxyResponse<SprawdzenieOsobyResponse>
                    {
                        RequestId = requestId,
                        Source = _sourceName,
                        Status = baseResp.Status,
                        SourceStatusCode = baseResp.SourceStatusCode,
                        Message = baseResp.Message
                    };
                }

                //
                // 3) Wywołanie serwisu KSIP – obsługa przez Match() (jak w CEPUdostepnianie UpKi)
                //
                var result = await _service.SprawdzenieOsobyWRuchuDrogowymAsync(body, requestId, ct);

                return result.Match(
                    onSuccess: dto => new ProxyResponse<SprawdzenieOsobyResponse>
                    {
                        Data = dto,
                        Status = ProxyStatus.Success,
                        Message = "OK",
                        Source = _sourceName,
                        SourceStatusCode = StatusCodes.Status200OK.ToString(),
                        RequestId = requestId
                    },

                    onError: err =>
                    {
                        var code = (err.HttpStatus ?? StatusCodes.Status500InternalServerError).ToString();

                        if (err.HttpStatus is >= 400 and < 500)
                        {
                            _logger.LogWarning(
                                "KSIP business error: {Code} {Message} (reqId={ReqId})",
                                err.Code, err.Message, requestId);

                            return ProxyResponses.BusinessError<SprawdzenieOsobyResponse>(
                                message: err.Message,
                                source: _sourceName,
                                sourceStatusCode: code,
                                requestId: requestId);
                        }

                        _logger.LogError(
                            "KSIP technical error: {Code} {Message} (reqId={ReqId})",
                            err.Code, err.Message, requestId);

                        return ProxyResponses.TechnicalError<SprawdzenieOsobyResponse>(
                            message: err.Message,
                            source: _sourceName,
                            sourceStatusCode: code,
                            requestId: requestId);
                    });
            }
            //
            // 4) OperationCanceledException
            //
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return ProxyResponses.TechnicalError<SprawdzenieOsobyResponse>(
                    "Żądanie zostało anulowane.",
                    _sourceName,
                    "499",
                    requestId);
            }
            //
            // 5) Nieoczekiwany wyjątek
            //
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Nieoczekiwany błąd w KSIPController.SprawdzenieOsobyWRuchuDrogowym (reqId={ReqId})",
                    requestId);

                return ProxyResponses.TechnicalError<SprawdzenieOsobyResponse>(
                    $"Nieoczekiwany błąd: {ex.Message}",
                    _sourceName,
                    StatusCodes.Status500InternalServerError.ToString(),
                    requestId);
            }
        }
    }
}
