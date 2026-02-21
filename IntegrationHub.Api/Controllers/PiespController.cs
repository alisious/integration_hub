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
                var result = await _dictService.GetByDictIdAsync(dictId, ct);
                var proxy = result.ToProxyResponse(_sourceName, requestId);
                if (result.IsSuccess)
                    proxy.Message = $"Znaleziono {result.Value?.Count ?? 0} elementów słownika.";
                return proxy;
            }
            catch (OperationCanceledException)
            {
                return ProxyResponseFactory.TechnicalError<IReadOnlyList<DictItem>>(
                    message: "Żądanie zostało anulowane.",
                    source: _sourceName,
                    sourceStatusCode: "499",
                    requestId: requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponseFactory.TechnicalError<IReadOnlyList<DictItem>>(
                    message: $"Nieoczekiwany błąd: {ex.Message}",
                    source: _sourceName,
                    sourceStatusCode: StatusCodes.Status500InternalServerError.ToString(),
                    requestId: requestId);
            }
        }
    }
}
