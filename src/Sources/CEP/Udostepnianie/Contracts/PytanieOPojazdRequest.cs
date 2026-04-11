using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    public sealed class PytanieOPojazdRequest
    {
        [JsonPropertyName("identyfikatorSystemowyPojazdu")]
        public string? IdentyfikatorSystemowyPojazdu { get; set; }

        // string w formacie yyyy-MM-ddTHH:mm:ss
        [JsonPropertyName("dataPrezentacji")]
        public string? DataPrezentacji { get; set; }

        [JsonPropertyName("wyszukiwaniePoDanychHistorycznych")]
        public bool WyszukiwaniePoDanychHistorycznych { get; set; } = false;

        [JsonPropertyName("numerRejestracyjny")]
        public string? NumerRejestracyjny { get; set; }

        // dokument w osobnym obiekcie (nullable)
        [JsonPropertyName("parametryDokumentuPojazdu")]
        public ParametryDokumentuPojazdu? ParametryDokumentuPojazdu { get; set; }

        [JsonPropertyName("numerPodwoziaNadwoziaRamy")]
        public string? NumerPodwoziaNadwoziaRamy { get; set; }

        // Dodatkowe kryteria
        [JsonPropertyName("numerRejestracyjnyZagraniczny")]
        public string? NumerRejestracyjnyZagraniczny { get; set; }

        [JsonPropertyName("identyfikatorSystemowyPodmiotu")]
        public string? IdentyfikatorSystemowyPodmiotu { get; set; }

        [JsonPropertyName("identyfikatorCzynnosci")]
        public string? IdentyfikatorCzynnosci { get; set; }
    }

    public sealed class ParametryDokumentuPojazdu
    {
        /// <summary>Typ dokumentu – kod słownikowy (domyślnie "DICT155_DR").</summary>
        [JsonPropertyName("typDokumentu")]
        public string TypDokumentu { get; set; } = "DICT155_DR";

        /// <summary>Seria i numer dokumentu (opcjonalnie).</summary>
        [JsonPropertyName("dokumentSeriaNumer")]
        public string? DokumentSeriaNumer { get; set; }
    }
}
