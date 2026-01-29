using IntegrationHub.Common.Primitives;

namespace IntegrationHub.Api.Services;

/// <summary>
/// Serwis pobierania wyeksportowanych plików po źródle (sourceKey) i nazwie pliku.
/// Katalog źródła jest konfigurowany w FileExport:Sources (np. horkos, anprs).
/// </summary>
public interface IExportFileService
{
    /// <summary>
    /// Pobiera plik z katalogu przypisanego do źródła. Nazwa pliku jest walidowana (brak path traversal).
    /// </summary>
    /// <param name="sourceKey">Klucz źródła z konfiguracji FileExport:Sources (np. horkos, anprs).</param>
    /// <param name="fileName">Nazwa pliku (np. lista_roczna_20250930_154712_WYNIK.csv).</param>
    /// <param name="ct">Token anulowania.</param>
    /// <returns>Zawartość pliku z typem MIME i nazwą do pobrania lub błąd (ValidationError / NotFoundError).</returns>
    Task<Result<ExportFileContent, Error>> GetByFileNameAsync(string sourceKey, string fileName, CancellationToken ct);
}

/// <summary>
/// Wynik odczytu pliku eksportu – zawartość, typ MIME i nazwa pliku do nagłówka Content-Disposition.
/// </summary>
public sealed record ExportFileContent(
    byte[] Content,
    string ContentType,
    string FileName
);
