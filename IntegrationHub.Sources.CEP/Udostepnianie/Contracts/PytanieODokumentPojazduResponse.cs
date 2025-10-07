// IntegrationHub.Sources.CEP.Udostepnianie.Contracts/PytanieODokumentPojazduResponse.cs
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    public sealed class PytanieODokumentPojazduResponse
    {
        [JsonPropertyName("meta")] public PytanieMeta? Meta { get; set; }
        [JsonPropertyName("pojazd")] public PojazdDokumentResponse? Pojazd { get; set; }
    }

    public sealed class PojazdDokumentResponse
    {
        [JsonPropertyName("aktualnyIdPojazdu")] public AktualnyIdPojazduDto? AktualnyIdPojazdu { get; set; }
        [JsonPropertyName("dokumentPojazdu")] public DokumentPojazduFullDto? DokumentPojazdu { get; set; }
    }
}
