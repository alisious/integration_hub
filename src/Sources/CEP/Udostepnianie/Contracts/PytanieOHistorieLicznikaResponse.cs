// IntegrationHub.Sources.CEP.Udostepnianie.Contracts/PytanieOHistorieLicznikaResponse.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    public sealed class PytanieOHistorieLicznikaResponse
    {
        [JsonPropertyName("meta")] public PytanieMeta? Meta { get; set; }

        [JsonPropertyName("aktualnyIdPojazdu")] public AktualnyIdPojazduDto? AktualnyIdPojazdu { get; set; }

        [JsonPropertyName("licznikDrogi")] public LicznikDrogiDto? LicznikDrogi { get; set; }
    }

    public sealed class LicznikDrogiDto
    {
        [JsonPropertyName("historyczneStanyLicznika")]
        public List<HistorycznyStanLicznikaDto> HistoryczneStanyLicznika { get; set; } = new();
    }

    public sealed class HistorycznyStanLicznikaDto
    {
        [JsonPropertyName("identyfikatorSystemowyStanuLicznika")] public string? IdentyfikatorSystemowyStanuLicznika { get; set; }

        // Agregat wspólny – z ResponseDto.cs (wartosc, jednostka, daty, skp)
        [JsonPropertyName("stanLicznika")] public StanLicznikaDto? StanLicznika { get; set; }

        // Dodatkowe fragmenty specyficzne dla historii (sygnał zmiany)
        [JsonPropertyName("sygnalZmiany")]
        public SygnalZmianyLicznikaDto? SygnalZmiany { get; set; }

        // Zrzut minimalny danych czynności (identyfikator i krótkie deskrypcje)
        [JsonPropertyName("daneCzynnosci")]
        public DaneCzynnosciMinimalDto? DaneCzynnosci { get; set; }
        [JsonPropertyName("informacjeSkp")]
        public InformacjeSkpDto? InformacjeSkp { get; set; }
    }

    public sealed class SygnalZmianyLicznikaDto
    {
        [JsonPropertyName("nieprawidlowoscWZmianieStanuLicznika")] public bool? NieprawidlowoscWZmianieStanuLicznika { get; set; }
        [JsonPropertyName("zmianaStanuLicznika")] public int? ZmianaStanuLicznika { get; set; }
        [JsonPropertyName("jednostkaStanuLicznika")] public SlownikZakresowyDto? JednostkaStanuLicznika { get; set; }
    }

    public sealed class DaneCzynnosciMinimalDto
    {
        [JsonPropertyName("identyfikatorCzynnosci")] public string? IdentyfikatorCzynnosci { get; set; }
        [JsonPropertyName("rodzajCzynnosciKod")] public string? RodzajCzynnosciKod { get; set; }
        [JsonPropertyName("rodzajCzynnosciOpis")] public string? RodzajCzynnosciOpis { get; set; }
    }
}
