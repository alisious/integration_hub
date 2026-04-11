using System.Text.Json.Serialization;

namespace IntegrationHub.PIESP.Models;

/// <summary>
/// Reprezentuje element słownika z tabeli piesp.DictItems.
/// </summary>
public sealed class DictItem
{
    /// <summary>
    /// Identyfikator elementu słownika (DI_ID).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    /// <summary>
    /// Identyfikator słownika (DI_DID) - grupa elementów.
    /// </summary>
    [JsonPropertyName("dictId")]
    public string? DictId { get; init; }

    /// <summary>
    /// Kod elementu słownika (DI_CODE).
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>
    /// Wartość elementu słownika (DI_VALUE).
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; init; } = default!;

    /// <summary>
    /// Data utworzenia (DI_CREATEDAT).
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }
}
