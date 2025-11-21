using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.ZW.Contracts
{
    public sealed class BronAdresRequest
    {
        [JsonPropertyName("miejscowosc")]
        public string? Miejscowosc { get; set; }

        [JsonPropertyName("ulica")]
        public string? Ulica { get; set; }

        [JsonPropertyName("numerDomu")]
        public string? NumerDomu { get; set; }

        [JsonPropertyName("numerLokalu")]
        public string? NumerLokalu { get; set; }

        [JsonPropertyName("kodPocztowy")]
        public string? KodPocztowy { get; set; }

        [JsonPropertyName("poczta")]
        public string? Poczta { get; set; }
    }
}
