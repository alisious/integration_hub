// IntegrationHub.Sources.CEP.Udostepnianie.Contracts/PytanieODokumentPojazduRequest.cs
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    /// <summary>
    /// Wejście do zapytania "pytanieODokumentPojazdu".
    /// Spłaszczone tylko z węzła <parametryDokumentuPojazdu>.
    /// </summary>
    public sealed class PytanieODokumentPojazduRequest
    {
        /// <summary>Jeśli podasz ten identyfikator – wystarczy samo to pole.</summary>
        [JsonPropertyName("identyfikatorSystemowyDokumentuPojazdu")]
        public string? IdentyfikatorSystemowyDokumentuPojazdu { get; set; }

        /// <summary>Kod typu dokumentu – domyślnie "DICT155_DR".</summary>
        [JsonPropertyName("typDokumentu")]
        public string? TypDokumentu { get; set; } = "DICT155_DR";

        /// <summary>Seria i numer dokumentu (np. DR) – gdy nie podajesz identyfikatora systemowego.</summary>
        [JsonPropertyName("dokumentSeriaNumer")]
        public string? DokumentSeriaNumer { get; set; }
    }
}
