using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.UpKi.Contracts
{
    public class DaneDokumentuResponseDto
    {
        [JsonPropertyName("dokumentUprawnieniaKierowcy")]
        public List<DokumentUprawnieniaKierowcyDto>? DokumentUprawnieniaKierowcy { get; set; }

        [JsonPropertyName("komunikat")]
        public string? Komunikat { get; set; }

        // W SOAP to DataType="date" → w DTO trzymamy DateTime? (data bez czasu)
        [JsonPropertyName("dataZapytania")]
        public DateTime? DataZapytania { get; set; }
    }

    public class DokumentUprawnieniaKierowcyDto
    {
        [JsonPropertyName("typDokumentu")]
        public UpKiWartoscSlownikowaDto? TypDokumentu { get; set; }

        [JsonPropertyName("numerDokumentu")]
        public string? NumerDokumentu { get; set; }

        [JsonPropertyName("seriaNumerDokumentu")]
        public string? SeriaNumerDokumentu { get; set; }

        [JsonPropertyName("organWydajacyDokument")]
        public UpKiWartoscSlownikowaDto? OrganWydajacyDokument { get; set; }

        [JsonPropertyName("dataWaznosci")]
        public DateTime? DataWaznosci { get; set; }

        [JsonPropertyName("dataWydania")]
        public DateTime? DataWydania { get; set; }

        [JsonPropertyName("parametrOsobaId")]
        public ParametrOsobaIdDto? ParametrOsobaId { get; set; }

        [JsonPropertyName("stanDokumentu")]
        public UpKiWartoscSlownikowaDto? StanDokumentu { get; set; }

        [JsonPropertyName("ograniczenie")]
        public List<OgraniczenieDto>? Ograniczenie { get; set; }

        [JsonPropertyName("daneUprawnieniaKategorii")]
        public List<DaneUprawnieniaKategoriiDto>? DaneUprawnieniaKategorii { get; set; }

        [JsonPropertyName("komunikatyBiznesowe")]
        public KomunikatBiznesowyDto? KomunikatyBiznesowe { get; set; }
    }

    public class UpKiWartoscSlownikowaDto
    {
        [JsonPropertyName("kod")]
        public string? Kod { get; set; }

        [JsonPropertyName("wartoscOpisowa")]
        public string? WartoscOpisowa { get; set; }
    }

    public class OgraniczenieDto
    {
        [JsonPropertyName("kodOgraniczenia")]
        public string? KodOgraniczenia { get; set; }

        [JsonPropertyName("wartoscOgraniczenia")]
        public string? WartoscOgraniczenia { get; set; }

        [JsonPropertyName("opisKodu")]
        public string? OpisKodu { get; set; }

        [JsonPropertyName("dataDo")]
        public DateTime? DataDo { get; set; }
    }

    public class DaneUprawnieniaKategoriiDto
    {
        [JsonPropertyName("kategoria")]
        public UpKiWartoscSlownikowaDto? Kategoria { get; set; }

        [JsonPropertyName("dataWaznosci")]
        public DateTime? DataWaznosci { get; set; }

        [JsonPropertyName("dataWydania")]
        public DateTime? DataWydania { get; set; }

        [JsonPropertyName("daneZakazuCofniecia")]
        public List<DaneZakazuCofnieciaDto>? DaneZakazuCofniecia { get; set; }

        [JsonPropertyName("ograniczenie")]
        public List<OgraniczenieDto>? Ograniczenie { get; set; }
    }

    public class DaneZakazuCofnieciaDto
    {
        [JsonPropertyName("typZdarzenia")]
        public string? TypZdarzenia { get; set; }

        [JsonPropertyName("dataDo")]
        public DateTime? DataDo { get; set; }
    }

    public class ParametrOsobaIdDto
    {
        [JsonPropertyName("osobaId")]
        public long? OsobaId { get; set; }

        [JsonPropertyName("wariantId")]
        public long? WariantId { get; set; }

        [JsonPropertyName("tokenKierowcy")]
        public string? TokenKierowcy { get; set; }

        [JsonPropertyName("idk")]
        public string? Idk { get; set; }

        [JsonPropertyName("daneKierowcy")]
        public DaneKierowcyDto? DaneKierowcy { get; set; }
    }

    public class DaneKierowcyDto
    {
        [JsonPropertyName("numerPesel")]
        public string? NumerPesel { get; set; }

        [JsonPropertyName("imiePierwsze")]
        public string? ImiePierwsze { get; set; }

        [JsonPropertyName("nazwisko")]
        public string? Nazwisko { get; set; }

        [JsonPropertyName("dataUrodzenia")]
        public DateTime? DataUrodzenia { get; set; }

        [JsonPropertyName("miejsceUrodzenia")]
        public string? MiejsceUrodzenia { get; set; }

        [JsonPropertyName("adres")]
        public UpKiAdresDto? Adres { get; set; }
    }

    public class UpKiAdresDto
    {
        [JsonPropertyName("miejsce")]
        public MiejsceDto? Miejsce { get; set; }

        [JsonPropertyName("nrLokalu")]
        public string? NrLokalu { get; set; }

        [JsonPropertyName("miejscowoscPodstawowa")]
        public MiejscowoscPodstawowaDto? MiejscowoscPodstawowa { get; set; }

        [JsonPropertyName("kraj")]
        public UpKiWartoscSlownikowaDto? Kraj { get; set; }

        [JsonPropertyName("ulica")]
        public UlicaDto? Ulica { get; set; }
    }

    public class MiejsceDto
    {
        [JsonPropertyName("kodTERYT")]
        public string? KodTERYT { get; set; }

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

        [JsonPropertyName("kodRodzajuGminy")]
        public string? KodRodzajuGminy { get; set; }

        [JsonPropertyName("kodPocztowyKrajowy")]
        public string? KodPocztowyKrajowy { get; set; }

        [JsonPropertyName("kodMiejscowosci")]
        public string? KodMiejscowosci { get; set; }

        [JsonPropertyName("nazwaMiejscowosci")]
        public string? NazwaMiejscowosci { get; set; }
    }

    public class MiejscowoscPodstawowaDto
    {
        [JsonPropertyName("kodMiejscowosciPodstawowej")]
        public string? KodMiejscowosciPodstawowej { get; set; }

        [JsonPropertyName("NazwaMiejscowosciPodstawowej")]
        public string? NazwaMiejscowosciPodstawowej { get; set; }
    }

    public class UlicaDto
    {
        [JsonPropertyName("cechaUlicy")]
        public UpKiWartoscSlownikowaDto? CechaUlicy { get; set; }

        [JsonPropertyName("kodUlicy")]
        public string? KodUlicy { get; set; }

        [JsonPropertyName("nazwaUlicy")]
        public string? NazwaUlicy { get; set; }

        [JsonPropertyName("nazwaUlicyZDokumentu")]
        public string? NazwaUlicyZDokumentu { get; set; }

        [JsonPropertyName("nrDomu")]
        public string? NrDomu { get; set; }
    }

    public class KomunikatBiznesowyDto
    {
        [JsonPropertyName("kod")]
        public string? Kod { get; set; }

        [JsonPropertyName("opis")]
        public string? Opis { get; set; }
    }
}
