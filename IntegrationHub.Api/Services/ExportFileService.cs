using IntegrationHub.Api.Config;
using IntegrationHub.Common.Primitives;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace IntegrationHub.Api.Services;

/// <summary>
/// Serwis pobierania plików z katalogów źródeł skonfigurowanych w FileExport:Sources (np. horkos, anprs).
/// Waliduje sourceKey i nazwę pliku (brak path traversal).
/// </summary>
public sealed class ExportFileService : IExportFileService
{
    private readonly IWebHostEnvironment _env;
    private readonly FileExportOptions _options;

    public ExportFileService(IWebHostEnvironment env, IOptions<FileExportOptions> options)
    {
        _env = env;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<Result<ExportFileContent, Error>> GetByFileNameAsync(string sourceKey, string fileName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
            return ErrorFactory.BusinessError(ErrorCodeEnum.ValidationError, "Źródło pliku (sourceKey) nie może być puste.", 400);

        if (!_options.Sources.TryGetValue(sourceKey, out var relativePath) || string.IsNullOrWhiteSpace(relativePath))
            return ErrorFactory.BusinessError(ErrorCodeEnum.ValidationError, "Nieznane źródło pliku.", 400);

        // Ścieżka względna do ContentRoot lub absolutna
        var exportDir = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(_env.ContentRootPath, relativePath);

        if (string.IsNullOrWhiteSpace(fileName))
            return ErrorFactory.BusinessError(ErrorCodeEnum.ValidationError, "Identyfikator pliku (nazwa) nie może być pusty.", 400);

        var safeName = Path.GetFileName(fileName);
        if (safeName != fileName || safeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return ErrorFactory.BusinessError(ErrorCodeEnum.ValidationError, "Nieprawidłowa nazwa pliku (dozwolona tylko nazwa pliku bez ścieżki).", 400);

        var fullPath = Path.GetFullPath(Path.Combine(exportDir, safeName));
        var exportDirFull = Path.GetFullPath(exportDir);
        if (!fullPath.StartsWith(exportDirFull, StringComparison.OrdinalIgnoreCase))
            return ErrorFactory.BusinessError(ErrorCodeEnum.ValidationError, "Nieprawidłowa nazwa pliku.", 400);

        if (!File.Exists(fullPath))
            return ErrorFactory.BusinessError(ErrorCodeEnum.NotFoundError, "Plik nie istnieje.", 404);

        try
        {
            var content = await File.ReadAllBytesAsync(fullPath, ct);
            var contentType = GetContentType(safeName);
            return new ExportFileContent(content, contentType, safeName);
        }
        catch (OperationCanceledException)
        {
            return ErrorFactory.TechnicalError(ErrorCodeEnum.OperationCanceledError, "Operacja anulowana.", 499);
        }
        catch (Exception ex)
        {
            return ErrorFactory.TechnicalError(ErrorCodeEnum.UnexpectedError, "Błąd odczytu pliku.", 500, ex.Message);
        }
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".csv" => "text/csv",
            ".html" => "text/html",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".xml" => "application/xml",
            _ => "application/octet-stream"
        };
    }
}
