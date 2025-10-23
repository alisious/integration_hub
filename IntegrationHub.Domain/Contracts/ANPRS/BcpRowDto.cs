namespace IntegrationHub.Domain.Contracts.ANPRS;

public sealed record BcpRowDto(
    string BcpId,
    string CountryCode,
    string SystemCode,
    string Name,
    string? Type,
    decimal? Latitude,
    decimal? Longitude
);
