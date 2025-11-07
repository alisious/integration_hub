using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public sealed record class VehiclesInPointReportRequest
{
    [Required, StringLength(3, MinimumLength = 3)]
    [JsonPropertyName("country")]
    public string Country { get; init; } = default!;

    [Required]
    [JsonPropertyName("system")]
    public string System { get; init; } = default!;

    [Required]
    [JsonPropertyName("bcpId")]
    public string BcpId { get; init; } = default!;

    [Required, JsonPropertyName("dateFrom")]
    public DateTime DateFrom { get; init; }

    [Required, JsonPropertyName("dateTo")]
    public DateTime DateTo { get; init; }

    [JsonPropertyName("userName")]
    public string? UserName { get; init; }

    [JsonPropertyName("unitName")]
    public string? UnitName { get; init; }
    
}
