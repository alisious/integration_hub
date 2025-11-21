using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.ZW.Contracts
{
    public sealed class OsobaZolnierzRequest
    {
        [JsonPropertyName("pesel")]
        public string Pesel { get; set; } = default!;
    }
}
