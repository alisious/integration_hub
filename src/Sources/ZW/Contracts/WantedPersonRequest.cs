using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.ZW.Contracts;

public sealed class WantedPersonRequest
{
    [JsonPropertyName("pesel")]
    public string Pesel { get; init; } = default!;
}
