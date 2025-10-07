// IntegrationHub.Sources.CEP.Udostepnianie.Contracts/PytanieOPojazdRozszerzoneResponse.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    public sealed class PytanieOPojazdRozszerzoneResponse
    {
        [JsonPropertyName("meta")] public MetaRozszerzone Meta { get; set; } = new();
        [JsonPropertyName("parametryZapytania")] public ParametryZapytaniaRozszerzone? ParametryZapytania { get; set; }
        [JsonPropertyName("pojazdRozszerzone")] public PojazdRozszerzoneDto? Pojazd { get; set; }
    }

    public sealed class PojazdRozszerzoneDto
    {
        [JsonPropertyName("aktualnyIdentyfikatorPojazdu")] public AktualnyIdPojazduDto? AktualnyIdentyfikatorPojazdu { get; set; }
        [JsonPropertyName("informacjeSKP")] public InformacjeSkpDto? InformacjeSkp { get; set; }
        [JsonPropertyName("daneTechnicznePojazdu")] public DaneTechnicznePojazduRozszerzoneDto? DaneTechnicznePojazdu { get; set; }
        [JsonPropertyName("homologacjaPojazdu")] public HomologacjaRozszerzonaDto? HomologacjaPojazdu { get; set; }
        [JsonPropertyName("daneOpisujacePojazd")] public DaneOpisujacePojazdRozszerzoneDto? DaneOpisujacePojazd { get; set; }
        [JsonPropertyName("danePierwszejRejestracji")] public PierwszaRejestracjaRozszerzonaDto? DanePierwszejRejestracji { get; set; }
        [JsonPropertyName("dokumentyPojazdu")] public List<DokumentPojazduRozszerzonyDto> DokumentPojazdu { get; set; } = new();
        [JsonPropertyName("danePojazduSprowadzonego")] public PojazdSprowadzonyRozszerzonyDto? DanePojazduSprowadzonego { get; set; }
        [JsonPropertyName("stanPojazdu")] public StanPojazduRozszerzonyDto? StanPojazdu { get; set; }
        [JsonPropertyName("najnowszyWariantPodmiotu")] public NajnowszyWariantPodmiotuDto? NajnowszyWariantPodmiotu { get; set; }
        [JsonPropertyName("rejestracjePojazdu")] public List<RejestracjaPojazduRozszerzonaDto> RejestracjePojazdu { get; set; } = new();
        [JsonPropertyName("danePolisyOC")] public PolisaOcRozszerzonaDto? DanePolisyOc { get; set; }
        [JsonPropertyName("oznaczenieAktualnyNrRejestracyjny")] public OznaczenieAktualnyNrRejestracyjnyDto? OznaczenieAktualnyNrRejestracyjny { get; set; }
        [JsonPropertyName("aktualnyStanLicznika")] public AktualnyStanLicznikaDto? AktualnyStanLicznika { get; set; }
    }

    // zachowujemy dziedziczenie jak w oryginale:
    public sealed class NajnowszyWariantPodmiotuDto : WlasnoscPodmiotuDto { }
}
