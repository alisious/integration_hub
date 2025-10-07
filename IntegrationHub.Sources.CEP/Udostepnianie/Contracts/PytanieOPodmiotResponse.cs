// IntegrationHub.Sources.CEP.Udostepnianie.Contracts/PytanieOPodmiotResponse.cs
using System.Text.Json.Serialization;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts; // wspólne DTO w tym namespace
// Używamy też komponentów wspólnych z ResponseDto.cs: PytanieMeta, AdresDto, KrajDto

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    public sealed class PytanieOPodmiotResponse
    {
        [JsonPropertyName("meta")] public PytanieMeta? Meta { get; set; }
        [JsonPropertyName("podmiot")] public PodmiotOsobaDto? Podmiot { get; set; }
    }

    /// <summary>Root agregatu „podmiot” (wariant: osoba fizyczna).</summary>
    public sealed class PodmiotOsobaDto
    {
        [JsonPropertyName("identyfikatorSystemowyPodmiotu")] public string? IdentyfikatorSystemowyPodmiotu { get; set; }
        [JsonPropertyName("wariantPodmiotu")] public string? WariantPodmiotu { get; set; }

        [JsonPropertyName("osoba")] public OsobaDto? Osoba { get; set; }
    }

    public sealed class OsobaDto
    {
        [JsonPropertyName("PESEL")] public string? PESEL { get; set; }
        [JsonPropertyName("imiePierwsze")] public string? ImiePierwsze { get; set; }
        [JsonPropertyName("nazwisko")] public string? Nazwisko { get; set; }
        [JsonPropertyName("dataUrodzenia")] public string? DataUrodzenia { get; set; }

        [JsonPropertyName("miejsceUrodzeniaKod")] public string? MiejsceUrodzeniaKod { get; set; }
        [JsonPropertyName("miejsceUrodzenia")] public string? MiejsceUrodzenia { get; set; }

        [JsonPropertyName("adres")] public AdresDto? Adres { get; set; } // z ResponseDto.cs (wspólne)
    }
}
