using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.UpKi.Contracts;
/// <summary>
/// Model komunikatu wejściowego DaneDokumentuRequest (xsd: upki:DaneDokumentuRequest).
/// </summary>
public sealed class DaneDokumentuRequest
{
    /// <summary>
    /// Data zapytania (opcjonalna), xsd:date, format "yyyy-MM-dd".
    /// </summary>
    [JsonPropertyName("dataZapytania")]
    public string? DataZapytania { get; set; }

    /// <summary>numerPesel – jedno z kryteriów (CHOICE).</summary>
    [JsonPropertyName("numerPesel")]
    public string? NumerPesel { get; set; }

    /// <summary>numerDokumentu – jedno z kryteriów (CHOICE).</summary>
    [JsonPropertyName("numerDokumentu")]
    public string? NumerDokumentu { get; set; }

    /// <summary>seriaNumerDokumentu – jedno z kryteriów (CHOICE).</summary>
    [JsonPropertyName("seriaNumerDokumentu")]
    public string? SeriaNumerDokumentu { get; set; }

    /// <summary>daneOsoby – jedno z kryteriów (CHOICE).</summary>
    [JsonPropertyName("daneOsoby")]
    public DaneOsoby? DaneOsoby { get; set; }

    /// <summary>osobaId (xsd:long) – jedno z kryteriów (CHOICE). Przechowywany jako string.</summary>
    [JsonPropertyName("osobaId")]
    public string? OsobaId { get; set; }

    /// <summary>idk – jedno z kryteriów (CHOICE).</summary>
    [JsonPropertyName("idk")]
    public string? Idk { get; set; }
}

/// <summary>
/// upki:DaneOsoby
/// </summary>
public sealed class DaneOsoby
{
    [JsonPropertyName("imiePierwsze")]
    public string? ImiePierwsze { get; set; }

    [JsonPropertyName("nazwisko")]
    public string? Nazwisko { get; set; }

    /// <summary>Data urodzenia, xsd:date, format "yyyy-MM-dd".</summary>
    [JsonPropertyName("dataUrodzenia")]
    public string? DataUrodzenia { get; set; }
}