// IntegrationHub.Sources.CEP.Udostepnianie.Contracts/PytanieOListeCzynnosciPojazduResponse.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    public sealed class PytanieOListeCzynnosciPojazduResponse
    {
        [JsonPropertyName("meta")] public PytanieMeta? Meta { get; set; }

        // Lista czynności CEP
        [JsonPropertyName("czynnosci")] public List<CzynnoscCepDto> CzynnoscCep { get; set; } = new();
    }

    public sealed class CzynnoscCepDto
    {
        [JsonPropertyName("identyfikatorCzynnosci")] public string? IdentyfikatorCzynnosci { get; set; }
        [JsonPropertyName("identyfikatorSystemowyPojazdu")] public string? IdentyfikatorSystemowyPojazdu { get; set; }

        [JsonPropertyName("rodzajCzynnosci")] public RodzajCzynnosciDto? RodzajCzynnosci { get; set; }
        [JsonPropertyName("podmiotUprawniony")] public OrganRozszerzonyDto? PodmiotUprawniony { get; set; }

        [JsonPropertyName("dataStanu")] public string? DataStanu { get; set; }
        [JsonPropertyName("dataOdnotowania")] public string? DataOdnotowania { get; set; }
        [JsonPropertyName("czyKorekta")] public bool? CzyKorekta { get; set; }
    }

    public sealed class RodzajCzynnosciDto : SlownikZakresowyDto
    {
        // Dodatkowe pola z odpowiedzi (poza standardowym „kod/…/dataOd/dataDo/status”)
        [JsonPropertyName("kodCzynnosci")] public string? KodCzynnosci { get; set; }
        [JsonPropertyName("modul")] public string? Modul { get; set; }
        [JsonPropertyName("opis")] public string? Opis { get; set; }
    }
}
