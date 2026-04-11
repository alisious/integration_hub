// IntegrationHub.Sources.CEP.UpKi.Contracts/DaneDokumentuContracts.cs
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.UpKi.Contracts
{
    
    /// <summary>
    /// Model komunikatu wyjściowego DaneDokumentuResponse (xsd: upki:DaneDokumentuResponse).
    /// </summary>
    public sealed class DaneDokumentuResponse
    {
        /// <summary>
        /// Data zapytania, jeśli zwrócona w odpowiedzi (xsd:date).
        /// </summary>
        [JsonPropertyName("dataZapytania")]
        public string? DataZapytania { get; set; }

        /// <summary>
        /// Lista dokumentów uprawnień kierowcy.
        /// (XML: 0..n elementów dokumentUprawnieniaKierowcy)
        /// </summary>
        [JsonPropertyName("dokumentyUprawnieniaKierowcy")]
        public List<DokumentUprawnieniaKierowcy> DokumentyUprawnieniaKierowcy { get; set; } = new();

        /// <summary>
        /// Dodatkowy komunikat tekstowy z CEK, jeśli występuje (XML: komunikat).
        /// </summary>
        [JsonPropertyName("komunikat")]
        public string? Komunikat { get; set; }
    }

    /// <summary>
    /// upki:DokumentUprawnieniaKierowcy
    /// </summary>
    public sealed class DokumentUprawnieniaKierowcy
    {
        [JsonPropertyName("dokumentId")]
        public string? DokumentId { get; set; }

        [JsonPropertyName("typDokumentu")]
        public WartoscSlownikowa? TypDokumentu { get; set; }

        [JsonPropertyName("numerDokumentu")]
        public string? NumerDokumentu { get; set; }

        [JsonPropertyName("seriaNumerDokumentu")]
        public string? SeriaNumerDokumentu { get; set; }

        [JsonPropertyName("organWydajacyDokument")]
        public WartoscSlownikowa? OrganWydajacyDokument { get; set; }

        /// <summary>Data ważności dokumentu, xsd:date.</summary>
        [JsonPropertyName("dataWaznosci")]
        public string? DataWaznosci { get; set; }

        /// <summary>Data wydania dokumentu, xsd:date.</summary>
        [JsonPropertyName("dataWydania")]
        public string? DataWydania { get; set; }

        [JsonPropertyName("parametrOsobaId")]
        public ParametrOsobaId? ParametrOsobaId { get; set; }

        [JsonPropertyName("stanDokumentu")]
        public StanDokumentu? StanDokumentu { get; set; }

        /// <summary>Zakazy / cofnięcia – maks. 2 wg XSD.</summary>
        [JsonPropertyName("daneZakazuCofniecia")]
        public List<DaneZakazuCofniecia> DaneZakazuCofniecia { get; set; } = new();

        /// <summary>Ograniczenia – dowolna liczba wg XSD.</summary>
        [JsonPropertyName("ograniczenia")]
        public List<Ograniczenie> Ograniczenia { get; set; } = new();

        /// <summary>Kategorie uprawnień – maks. 16 wg XSD.</summary>
        [JsonPropertyName("daneUprawnieniaKategorii")]
        public List<DaneUprawnieniaKategorii> DaneUprawnieniaKategorii { get; set; } = new();

        /// <summary>Komunikat biznesowy zwrócony dla tego dokumentu.</summary>
        [JsonPropertyName("komunikatBiznesowy")]
        public KomunikatBiznesowy? KomunikatBiznesowy { get; set; }
    }

    /// <summary>
    /// upki:WartoscSlownikowa – standardowy słownik (kod, opis).
    /// </summary>
    public sealed class WartoscSlownikowa
    {
        [JsonPropertyName("kod")]
        public string? Kod { get; set; }

        [JsonPropertyName("wartoscOpisowa")]
        public string? WartoscOpisowa { get; set; }
    }

    /// <summary>
    /// upki:ParametrOsobaId
    /// </summary>
    public sealed class ParametrOsobaId
    {
        [JsonPropertyName("osobaId")]
        public string? OsobaId { get; set; }

        [JsonPropertyName("wariantId")]
        public string? WariantId { get; set; }

        /// <summary>tokenKierowcy wg XSD (w przykładzie: brak).</summary>
        [JsonPropertyName("tokenKierowcy")]
        public string? TokenKierowcy { get; set; }

        [JsonPropertyName("idk")]
        public string? Idk { get; set; }

        [JsonPropertyName("daneKierowcy")]
        public DaneKierowcy? DaneKierowcy { get; set; }
    }

    /// <summary>
    /// upki:DaneKierowcy – dane osoby + adres.
    /// </summary>
    public sealed class DaneKierowcy
    {
        [JsonPropertyName("numerPesel")]
        public string? NumerPesel { get; set; }

        [JsonPropertyName("imiePierwsze")]
        public string? ImiePierwsze { get; set; }

        [JsonPropertyName("nazwisko")]
        public string? Nazwisko { get; set; }

        /// <summary>xsd:date, w odpowiedzi zwykle z offsetem (np. "1973-02-09+01:00").</summary>
        [JsonPropertyName("dataUrodzenia")]
        public string? DataUrodzenia { get; set; }

        [JsonPropertyName("miejsceUrodzenia")]
        public string? MiejsceUrodzenia { get; set; }

        [JsonPropertyName("adres")]
        public Adres? Adres { get; set; }
    }

    /// <summary>
    /// upki:Adres
    /// </summary>
    public sealed class Adres
    {
        [JsonPropertyName("miejsce")]
        public Miejsce? Miejsce { get; set; }

        [JsonPropertyName("nrLokalu")]
        public string? NrLokalu { get; set; }

        [JsonPropertyName("miejscowoscPodstawowa")]
        public MiejscowoscPodstawowa? MiejscowoscPodstawowa { get; set; }

        [JsonPropertyName("kraj")]
        public WartoscSlownikowa? Kraj { get; set; }

        [JsonPropertyName("ulica")]
        public Ulica? Ulica { get; set; }
    }

    /// <summary>
    /// upki:Miejsce
    /// </summary>
    public sealed class Miejsce
    {
        [JsonPropertyName("kodTERYT")]
        public string? KodTeryt { get; set; }

        [JsonPropertyName("kodWojewodztwa")]
        public string? KodWojewodztwa { get; set; }

        [JsonPropertyName("nazwaWojewodztwaStanu")]
        public string? NazwaWojewodztwaStanu { get; set; }

        [JsonPropertyName("kodPowiatu")]
        public string? KodPowiatu { get; set; }

        [JsonPropertyName("nazwaPowiatuDzielnicy")]
        public string? NazwaPowiatuDzielnicy { get; set; }

        [JsonPropertyName("kodGminy")]
        public string? KodGminy { get; set; }

        [JsonPropertyName("nazwaGminy")]
        public string? NazwaGminy { get; set; }

        [JsonPropertyName("kodMiejscowosci")]
        public string? KodMiejscowosci { get; set; }

        [JsonPropertyName("nazwaMiejscowosci")]
        public string? NazwaMiejscowosci { get; set; }

        [JsonPropertyName("kodPocztowyKrajowy")]
        public string? KodPocztowyKrajowy { get; set; }
    }

    /// <summary>
    /// upki:MiejscowoscPodstawowa
    /// </summary>
    public sealed class MiejscowoscPodstawowa
    {
        [JsonPropertyName("kodMiejscowosciPodstawowej")]
        public string? KodMiejscowosciPodstawowej { get; set; }

        [JsonPropertyName("nazwaMiejscowosciPodstawowej")]
        public string? NazwaMiejscowosciPodstawowej { get; set; }
    }

    /// <summary>
    /// upki:Ulica
    /// </summary>
    public sealed class Ulica
    {
        [JsonPropertyName("cechaUlicy")]
        public WartoscSlownikowa? CechaUlicy { get; set; }

        [JsonPropertyName("kodUlicy")]
        public string? KodUlicy { get; set; }

        [JsonPropertyName("nazwaUlicy")]
        public string? NazwaUlicy { get; set; }

        [JsonPropertyName("nazwaUlicyZDokumentu")]
        public string? NazwaUlicyZDokumentu { get; set; }

        [JsonPropertyName("nrDomu")]
        public string? NrDomu { get; set; }
    }

    /// <summary>
    /// upki:StanDokumentu
    /// </summary>
    public sealed class StanDokumentu
    {
        [JsonPropertyName("stanDokumentu")]
        public WartoscSlownikowa? Stan { get; set; }

        [JsonPropertyName("dataZmianyStanu")]
        public string? DataZmianyStanu { get; set; }

        [JsonPropertyName("podmiotZmianyStanu")]
        public WartoscSlownikowa? PodmiotZmianyStanu { get; set; }

        [JsonPropertyName("powodZmianyStanu")]
        public List<WartoscSlownikowa> PowodZmianyStanu { get; set; } = new();
    }

    /// <summary>
    /// upki:DaneZakazuCofniecia
    /// </summary>
    public sealed class DaneZakazuCofniecia
    {
        [JsonPropertyName("typZdarzenia")]
        public string? TypZdarzenia { get; set; }

        /// <summary>Data końca obowiązywania zakazu, xsd:date.</summary>
        [JsonPropertyName("dataDo")]
        public string? DataDo { get; set; }
    }

    /// <summary>
    /// upki:Ograniczenie
    /// </summary>
    public sealed class Ograniczenie
    {
        [JsonPropertyName("kodOgraniczenia")]
        public string? KodOgraniczenia { get; set; }

        [JsonPropertyName("wartoscOgraniczenia")]
        public string? WartoscOgraniczenia { get; set; }

        [JsonPropertyName("opisKodu")]
        public string? OpisKodu { get; set; }

        /// <summary>Data obowiązywania ograniczenia do, xsd:date.</summary>
        [JsonPropertyName("dataDo")]
        public string? DataDo { get; set; }
    }

    /// <summary>
    /// upki:DaneUprawnieniaKategorii
    /// </summary>
    public sealed class DaneUprawnieniaKategorii
    {
        [JsonPropertyName("kategoria")]
        public WartoscSlownikowa? Kategoria { get; set; }

        [JsonPropertyName("dataWaznosci")]
        public string? DataWaznosci { get; set; }

        [JsonPropertyName("dataWydania")]
        public string? DataWydania { get; set; }

        [JsonPropertyName("daneZakazuCofniecia")]
        public List<DaneZakazuCofniecia> DaneZakazuCofniecia { get; set; } = new();

        [JsonPropertyName("ograniczenia")]
        public List<Ograniczenie> Ograniczenia { get; set; } = new();
    }

    /// <summary>
    /// upki:KomunikatBiznesowy
    /// </summary>
    public sealed class KomunikatBiznesowy
    {
        [JsonPropertyName("kod")]
        public string? Kod { get; set; }

        [JsonPropertyName("opis")]
        public string? Opis { get; set; }
    }
}
