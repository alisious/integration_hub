using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.ZW.Contracts
{
    public sealed class SoldierRequest
    {
        [JsonPropertyName("pesel")]
        public string Pesel { get; set; } = default!;
    }
}
