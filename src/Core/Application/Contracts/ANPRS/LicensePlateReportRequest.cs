using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public sealed record class LicensePlateReportRequest
{
    [Required]
    [JsonPropertyName("numberPlate")]
    public string NumberPlate { get; init; } = default!;

    [Required, JsonPropertyName("dateFrom")]
    public DateTime DateFrom { get; init; }

    [Required, JsonPropertyName("dateTo")]
    public DateTime DateTo { get; init; }

    [JsonPropertyName("userName")]
    public string? UserName { get; init; }

    [JsonPropertyName("unitName")]
    public string? UnitName { get; init; }
}
