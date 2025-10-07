// IntegrationHub.Sources.CEP.Udostepnianie.Contracts/PytanieOPojazdResponse.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    public sealed class PytanieOPojazdResponse
    {
        [JsonPropertyName("meta")] public PytanieMeta Meta { get; set; } = new();
        [JsonPropertyName("pojazd")] public PojazdDto? Pojazd { get; set; }
    }

    public class PojazdDto
    {
        [JsonPropertyName("aktualnyIdPojazdu")] public AktualnyIdPojazduDto? AktualnyIdPojazdu { get; set; }
        [JsonPropertyName("stanPojazdu")] public StanPojazduDto? StanPojazdu { get; set; }
        [JsonPropertyName("daneOpisujace")] public DaneOpisujacePojazdDto? DaneOpisujace { get; set; }
        [JsonPropertyName("homologacja")] public HomologacjaBasicDto? Homologacja { get; set; }
        [JsonPropertyName("pierwszaRejestracja")] public PierwszaRejestracjaDto? PierwszaRejestracja { get; set; }
        [JsonPropertyName("badanieTechniczne")] public BadanieTechniczneDto? BadanieTechniczne { get; set; }
        [JsonPropertyName("daneTech")] public DaneTechniczneDto? DaneTechniczne { get; set; }
        [JsonPropertyName("sprowadzony")] public PojazdSprowadzonyDto? Sprowadzony { get; set; }
        [JsonPropertyName("rejestracje")] public List<RejestracjaDto> Rejestracje { get; set; } = new();
        [JsonPropertyName("dokumenty")] public List<DokumentPojazduDto> Dokumenty { get; set; } = new();
        [JsonPropertyName("oc")] public PolisaOcDto? PolisaOc { get; set; }
        [JsonPropertyName("aktualnyNrRej")] public string? AktualnyNumerRejestracyjny { get; set; }
    }
}
