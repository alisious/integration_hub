using System.Text.Json.Serialization;

namespace IntegrationHub.Domain.Contracts.ANPRS;

/// <summary>
/// Reprezentuje wiersz BCP (Border Control Point) używany w integracji ANPRS.
/// Właściwości są serializowane do JSON w formacie camelCase.
/// </summary>
public sealed record BcpRowDto(
    [property: JsonPropertyName("bcpId")] string BcpId,
    [property: JsonPropertyName("countryCode")] string CountryCode,
    [property: JsonPropertyName("systemCode")] string SystemCode,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("latitude")] decimal? Latitude,
    [property: JsonPropertyName("longitude")] decimal? Longitude
);