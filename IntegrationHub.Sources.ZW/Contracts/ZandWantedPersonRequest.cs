using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.ZW.Contracts;

public sealed class ZandWantedPersonRequest
{
    [JsonPropertyName("pesel")]
    public string Pesel { get; init; } = default!;
}
