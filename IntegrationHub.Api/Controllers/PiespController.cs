using IntegrationHub.Common.Contracts;
using IntegrationHub.Common.Primitives;
using IntegrationHub.PIESP.Models;
using IntegrationHub.PIESP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace IntegrationHub.PIESP.Controllers
{
    /// <summary>
    /// Kontroler do operacji na słownikach PIESP.
    /// </summary>
    [ApiController]
    [Route("piesp/[controller]")]
    [Authorize]
    [SwaggerTag("Słowniki PIESP")]
    [Produces("application/json")]
    public sealed class PiespController : ControllerBase
    {
        private readonly IDictService _dictService;
        private const string _sourceName = "PIESP";

        public PiespController(IDictService dictService)
        {
            _dictService = dictService;
        }

        /// <summary>
        /// Pobiera wszystkie elementy słownika dla danego ID słownika (DI_DID).
        /// </summary>
        /// <param name="dictId">Identyfikator słownika (DI_DID).</param>
        /// <param name="ct">Token anulowania operacji.</param>
        /// <returns>Lista elementów słownika.</returns>
        [HttpGet("dict/{dictId}")]
        [SwaggerOperation(
            Summary = "Pobierz elementy słownika",
            Description = "Pobiera wszystkie elementy słownika o podanym identyfikatorze (DI_DID) z tabeli piesp.DictItems."
        )]
        [ProducesResponseType(typeof(ProxyResponse<IReadOnlyList<DictItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<IReadOnlyList<DictItem>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<IReadOnlyList<DictItem>>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProxyResponse<IReadOnlyList<DictItem>>), StatusCodes.Status500InternalServerError)]
        public async Task<ProxyResponse<IReadOnlyList<DictItem>>> GetDictItemsByDictIdAsync(
            [FromRoute] string dictId,
            CancellationToken ct = default)
        {
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                // Wywołanie serwisu zwracającego Result<T, Error>
                var result = await _dictService.GetByDictIdAsync(dictId, ct);

                // Konwersja Result na ProxyResponse
                var proxy = result.ToProxyResponse();

                // Uzupełnienie metadanych ProxyResponse
                proxy.Source = _sourceName;
                proxy.RequestId = requestId;

                if (result.IsSuccess)
                {
                    proxy.Status = ProxyStatus.Success;
                    proxy.SourceStatusCode = StatusCodes.Status200OK.ToString();
                    proxy.Message = $"Znaleziono {result.Value?.Count ?? 0} elementów słownika.";
                }
                else
                {
                    var err = result.Error;
                    proxy.Status = err.ErrorKind == ErrorKindEnum.Business
                        ? ProxyStatus.BusinessError
                        : ProxyStatus.TechnicalError;
                    proxy.SourceStatusCode = (err.HttpStatus ?? StatusCodes.Status500InternalServerError).ToString();
                    // Message ustawił już ProxyResponseMapper (err.Message).
                }

                return proxy;
            }
            catch (OperationCanceledException)
            {
                return ProxyResponses.TechnicalError<IReadOnlyList<DictItem>>(
                    message: "Żądanie zostało anulowane.",
                    source: _sourceName,
                    sourceStatusCode: "499",
                    requestId: requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponses.TechnicalError<IReadOnlyList<DictItem>>(
                    message: $"Nieoczekiwany błąd: {ex.Message}",
                    source: _sourceName,
                    sourceStatusCode: StatusCodes.Status500InternalServerError.ToString(),
                    requestId: requestId);
            }
        }
    }
}
