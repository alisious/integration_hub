// IntegrationHub.Sources.CEP.Udostepnianie.Contracts/PytanieOPodmiotRequest.cs
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    public sealed class PytanieOPodmiotRequest
    {
        // Wspólne / ID (opcjonalny skrót identyfikacji)
        [JsonPropertyName("identyfikatorSystemowyPodmiotu")]
        public string? IdentyfikatorSystemowyPodmiotu { get; set; }

        // Używamy wyłącznie gniazd:
        [JsonPropertyName("parametryOsoby")]
        public ParametryOsobyDto? ParametryOsoby { get; set; }

        [JsonPropertyName("parametryFirmy")]
        public ParametryFirmyDto? ParametryFirmy { get; set; }
    }

    // ===== OSOBA (pola wykorzystywane w Envelope: wartości płaskie + zagnieżdżenia)
    public sealed class ParametryOsobyDto
    {
        // Identyfikacja osoby
        [JsonPropertyName("PESEL")] public string? PESEL { get; set; }
        [JsonPropertyName("imiePierwsze")] public string? ImiePierwsze { get; set; }
        [JsonPropertyName("nazwisko")] public string? Nazwisko { get; set; }
        [JsonPropertyName("dataUrodzenia")] public string? DataUrodzenia { get; set; }
        [JsonPropertyName("miejsceUrodzenia")] public string? MiejsceUrodzenia { get; set; }
        // <miejsceUrodzeniaKod><kod>...</kod></miejsceUrodzeniaKod>
        [JsonPropertyName("miejsceUrodzeniaKod")] public string? MiejsceUrodzeniaKod { get; set; }

        // Adres (parametryAdresu)
        [JsonPropertyName("nazwaWojewodztwaStanu")] public string? NazwaWojewodztwaStanu { get; set; }
        [JsonPropertyName("nazwaPowiatuDzielnicy")] public string? NazwaPowiatuDzielnicy { get; set; }
        [JsonPropertyName("nazwaGminy")] public string? NazwaGminy { get; set; }
        [JsonPropertyName("nazwaMiejscowosci")] public string? NazwaMiejscowosci { get; set; }
        [JsonPropertyName("nazwaUlicy")] public string? NazwaUlicy { get; set; }
        [JsonPropertyName("numerDomu")] public string? NumerDomu { get; set; }
        [JsonPropertyName("kodPocztowy")] public string? KodPocztowy { get; set; }

        // Dokument tożsamości (parametryZagranicznegoDokumentuPotwierdzajacegoTozsamosc)
        [JsonPropertyName("nazwaDokumentu")] public string? NazwaDokumentu { get; set; }
        [JsonPropertyName("seriaNumerDokumentu")] public string? SeriaNumerDokumentu { get; set; }
        // <nazwaPanstwaWydajacegoDokument><kod>...</kod></nazwaPanstwaWydajacegoDokument>
        [JsonPropertyName("kodPanstwaWydajacegoDokument")] public string? KodPanstwaWydajacegoDokument { get; set; }
    }

    // ===== FIRMA (pola wykorzystywane w Envelope: wartości płaskie + zagnieżdżenia)
    public sealed class ParametryFirmyDto
    {
        // Identyfikacja firmy
        [JsonPropertyName("REGON")] public string? REGON { get; set; }
        [JsonPropertyName("nazwaFirmyDrukowana")] public string? NazwaFirmyDrukowana { get; set; }
        [JsonPropertyName("identyfikatorSystemowyREGON")] public string? IdentyfikatorSystemowyREGON { get; set; }

        // Firma zagraniczna (parametryFirmyZagranicznej)
        // <krajZagranicznejWlasnosci><kod>...</kod></krajZagranicznejWlasnosci>
        [JsonPropertyName("kodKrajuZagranicznejWlasnosci")] public string? KodKrajuZagranicznejWlasnosci { get; set; }
        [JsonPropertyName("zagranicznyNumerIdentyfikacyjny")] public string? ZagranicznyNumerIdentyfikacyjny { get; set; }

        // Adres (parametryAdresu)
        [JsonPropertyName("nazwaWojewodztwaStanu")] public string? NazwaWojewodztwaStanu { get; set; }
        [JsonPropertyName("nazwaPowiatuDzielnicy")] public string? NazwaPowiatuDzielnicy { get; set; }
        [JsonPropertyName("nazwaGminy")] public string? NazwaGminy { get; set; }
        [JsonPropertyName("nazwaMiejscowosci")] public string? NazwaMiejscowosci { get; set; }
        [JsonPropertyName("nazwaUlicy")] public string? NazwaUlicy { get; set; }
        [JsonPropertyName("numerDomu")] public string? NumerDomu { get; set; }
        [JsonPropertyName("kodPocztowy")] public string? KodPocztowy { get; set; }
    }
}
