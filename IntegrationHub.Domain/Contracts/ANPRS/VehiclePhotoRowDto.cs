using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.ANPRS.Contracts
{
    /// <summary>
    /// DTO reprezentujący jeden wiersz z grida zdjęć ANPRS:
    /// kolumny "Zdjęcia" i opcjonalnie "Położenie numeru".
    /// </summary>
    public sealed record VehiclePhotoRowDto
    {
        /// <summary>Wartość z kolumny "Zdjęcia" (Base64 JPG).</summary>
        [JsonPropertyName("zdjecie")]
        public string Zdjecie { get; init; } = string.Empty;

        /// <summary>Wartość z kolumny "Położenie numeru" (np. "front" / "rear"); null, jeśli brak w odpowiedzi.</summary>
        [JsonPropertyName("polozenie_numeru")]
        public string? PolozenieNumeru { get; init; }
    }
}
