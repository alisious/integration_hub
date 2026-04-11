// IntegrationHub.Sources.CEP.Udostepnianie.Contracts/PytanieOPodmiotResponse.cs
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    public sealed class PytanieOPodmiotResponse
    {
        [JsonPropertyName("meta")] public PytanieMeta? Meta { get; set; }

        // Root „podmiot” może być osobą lub firmą
        [JsonPropertyName("podmiot")] public PodmiotAnyDto? Podmiot { get; set; }
    }

    /// <summary>Root agregatu „podmiot” (union: osoba lub firma).</summary>
    public sealed class PodmiotAnyDto
    {
        [JsonPropertyName("identyfikatorSystemowyPodmiotu")] public string? IdentyfikatorSystemowyPodmiotu { get; set; }
        [JsonPropertyName("wariantPodmiotu")] public string? WariantPodmiotu { get; set; }

        [JsonPropertyName("osoba")] public OsobaDto? Osoba { get; set; }
        [JsonPropertyName("firma")] public FirmaDto? Firma { get; set; } // z ResponseDto.cs
    }

    public sealed class OsobaDto
    {
        [JsonPropertyName("PESEL")] public string? PESEL { get; set; }
        [JsonPropertyName("imiePierwsze")] public string? ImiePierwsze { get; set; }
        [JsonPropertyName("nazwisko")] public string? Nazwisko { get; set; }
        [JsonPropertyName("dataUrodzenia")] public string? DataUrodzenia { get; set; }
        [JsonPropertyName("miejsceUrodzeniaKod")] public string? MiejsceUrodzeniaKod { get; set; }
        [JsonPropertyName("miejsceUrodzenia")] public string? MiejsceUrodzenia { get; set; }
        [JsonPropertyName("adres")] public AdresDto? Adres { get; set; } // wspólne
    }
}
