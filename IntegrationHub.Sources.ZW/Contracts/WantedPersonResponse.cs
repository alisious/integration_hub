using System;
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.ZW.Contracts;

public sealed class WantedPersonResponse
{
    [JsonPropertyName("pesel")] public string? Pesel { get; init; }
    [JsonPropertyName("jzwPoszukujaca")] public string? JzwPoszukujaca { get; init; }
    
}