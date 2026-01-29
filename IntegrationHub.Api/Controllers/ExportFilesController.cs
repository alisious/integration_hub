using IntegrationHub.Api.Services;
using IntegrationHub.Common.Primitives;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace IntegrationHub.Api.Controllers;

/// <summary>
/// Kontroler umożliwiający pobieranie wyeksportowanych plików po źródle (sourceKey) i nazwie pliku.
/// Źródła są konfigurowane w FileExport:Sources (np. horkos → App_Data/Horkos/Exports, anprs → App_Data/ANPRS/Exports).
/// </summary>
[ApiController]
[Route("tools/files")]
[ApiExplorerSettings(GroupName = "v1")]
[Produces("application/octet-stream", "text/csv", "text/html", "text/plain", "application/json", "application/xml")]
public sealed class ExportFilesController : ControllerBase
{
    private readonly IExportFileService _exportFileService;
    private readonly ILogger<ExportFilesController> _logger;

    public ExportFilesController(IExportFileService exportFileService, ILogger<ExportFilesController> logger)
    {
        _exportFileService = exportFileService;
        _logger = logger;
    }

    /// <summary>
    /// Pobiera plik eksportu na podstawie źródła (sourceKey) i identyfikatora (nazwy pliku).
    /// </summary>
    /// <param name="sourceKey">Klucz źródła z FileExport:Sources (np. horkos, anprs).</param>
    /// <param name="fileName">Nazwa pliku (np. lista_roczna_20250930_154712_WYNIK.csv).</param>
    /// <param name="ct">Token anulowania.</param>
    /// <returns>Plik do pobrania (200) lub błąd walidacji/brak pliku (400/404).</returns>
    [HttpGet("{sourceKey}/{fileName}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Pobierz plik eksportu po źródle i nazwie",
        Description = "Zwraca plik z katalogu przypisanego do źródła w FileExport:Sources. Źródła: horkos, anprs.",
        OperationId = "Tools_GetExportFile",
        Tags = new[] { "Eksporty plików" }
    )]
    public async Task<IActionResult> GetByFileName([FromRoute] string sourceKey, [FromRoute] string fileName, CancellationToken ct)
    {
        var result = await _exportFileService.GetByFileNameAsync(sourceKey, fileName, ct);

        if (result.IsSuccess)
        {
            var content = result.Value!;
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
            return File(content.Content, content.ContentType, content.FileName);
        }

        var err = result.Error!;
        var status = err.HttpStatus ?? 500;
        if (status == 404)
        {
            _logger.LogDebug("Export file not found: {SourceKey}/{FileName}", sourceKey, fileName);
            return NotFound(err.Message);
        }
        if (status == 400)
        {
            return BadRequest(err.Message);
        }
        _logger.LogWarning("Export file error for {SourceKey}/{FileName}: {Message}", sourceKey, fileName, err.Message);
        return StatusCode(status, err.Message);
    }
}
