using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.ZW.Contracts
{
    public sealed class OsobaZolnierzResponse
    {
        [JsonPropertyName("pesel")]
        public string Pesel { get; set; } = default!;

        [JsonPropertyName("stopien")]
        public string Stopien { get; set; } = default!;

        [JsonPropertyName("jednostka")]
        public string? Jednostka { get; set; }

        [JsonPropertyName("peselHash")]
        public string? PeselHash { get; set; }
    }
}
