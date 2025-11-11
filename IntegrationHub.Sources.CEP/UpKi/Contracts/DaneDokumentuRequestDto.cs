using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.UpKi.Contracts
{
    public sealed class DaneDokumentuRequestDto
    {
        // Wypełnij dokładnie JEDEN z dwóch bloków: danePesel LUB daneOsoby
        [JsonPropertyName("danePesel")]
        public DanePesel? DanePesel { get; init; }

        [JsonPropertyName("daneOsoby")]
        public DaneOsoby? DaneOsoby { get; init; }
    }

    public sealed class DanePesel
    {
        [JsonPropertyName("numerPesel")]
        public string NumerPesel { get; init; } = default!;

        [JsonPropertyName("dataZapytania")]
        public DateTime DataZapytania { get; init; }
    }

    public sealed class DaneOsoby
    {
        [JsonPropertyName("imiePierwsze")]
        public string ImiePierwsze { get; init; } = default!;

        [JsonPropertyName("nazwisko")]
        public string Nazwisko { get; init; } = default!;

        [JsonPropertyName("dataUrodzenia")]
        public DateOnly DataUrodzenia { get; init; }

        [JsonPropertyName("dataZapytania")]
        public DateTime DataZapytania { get; init; }
    }
}
