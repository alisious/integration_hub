namespace IntegrationHub.Api.Config;

/// <summary>
/// Konfiguracja katalogów źródeł plików eksportu (FileExport:Sources).
/// Klucz = identyfikator źródła (np. horkos, anprs), wartość = ścieżka katalogu (względna do ContentRoot lub absolutna).
/// </summary>
public sealed class FileExportOptions
{
    public const string SectionName = "FileExport";

    /// <summary>
    /// Mapa: sourceKey → ścieżka katalogu (np. "horkos" → "App_Data/Horkos/Exports", "anprs" → "App_Data/ANPRS/Exports").
    /// </summary>
    public Dictionary<string, string> Sources { get; set; } = new();
}
