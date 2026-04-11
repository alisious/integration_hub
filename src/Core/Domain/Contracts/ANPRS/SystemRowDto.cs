using System.Text.Json.Serialization;

namespace IntegrationHub.Domain.Contracts.ANPRS;

/// <summary>
/// Reprezentuje wiersz opisuj¹cy system w integracji ANPRS.
/// W³aœciwoœci s¹ serializowane do JSON przy u¿yciu nazw w camelCase.
/// </summary>
public sealed record SystemRowDto(
    [property: JsonPropertyName("systemCode")] string SystemCode,
    [property: JsonPropertyName("description")] string? Description
);