// IntegrationHub.Sources.CEP.Udostepnianie/Contracts/PytanieOPojazdResponse.cs
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    public sealed class PytanieOPojazdResponse
    {
        [JsonPropertyName("meta")]
        public PytanieMeta Meta { get; set; } = new();

        [JsonPropertyName("pojazd")]
        public PojazdDto? Pojazd { get; set; }
    }

    public sealed class PytanieMeta
    {
        [JsonPropertyName("identyfikatorTransakcji")] public string? IdentyfikatorTransakcji { get; set; }
        [JsonPropertyName("iloscZwroconychRekordow")] public int? IloscZwroconychRekordow { get; set; }
        [JsonPropertyName("znacznikCzasowy")] public string? ZnacznikCzasowy { get; set; }
        [JsonPropertyName("identyfikatorSystemuZewnetrznego")] public string? IdentyfikatorSystemuZewnetrznego { get; set; }
        [JsonPropertyName("znakSprawy")] public string? ZnakSprawy { get; set; }
        [JsonPropertyName("wnioskodawca")] public string? Wnioskodawca { get; set; }
    }

    public class PojazdDto
    {
        [JsonPropertyName("aktualnyIdPojazdu")] public AktualnyIdPojazduDto? AktualnyIdPojazdu { get; set; }
        [JsonPropertyName("stanPojazdu")] public StanPojazduDto? StanPojazdu { get; set; }
        [JsonPropertyName("daneOpisujace")] public DaneOpisujacePojazdDto? DaneOpisujace { get; set; }
        [JsonPropertyName("homologacja")] public HomologacjaDto? Homologacja { get; set; }
        [JsonPropertyName("pierwszaRejestracja")] public PierwszaRejestracjaDto? PierwszaRejestracja { get; set; }
        [JsonPropertyName("badanieTechniczne")] public BadanieTechniczneDto? BadanieTechniczne { get; set; }
        [JsonPropertyName("daneTech")] public DaneTechniczneDto? DaneTechniczne { get; set; }
        [JsonPropertyName("sprowadzony")] public PojazdSprowadzonyDto? Sprowadzony { get; set; }
        [JsonPropertyName("rejestracje")] public List<RejestracjaDto> Rejestracje { get; set; } = new();
        [JsonPropertyName("dokumenty")] public List<DokumentPojazduDto> Dokumenty { get; set; } = new();
        [JsonPropertyName("oc")] public PolisaOcDto? PolisaOc { get; set; }
        [JsonPropertyName("aktualnyNrRej")] public string? AktualnyNumerRejestracyjny { get; set; }
    }

    public sealed class AktualnyIdPojazduDto
    {
        [JsonPropertyName("identyfikatorSystemowyPojazdu")] public string? IdentyfikatorSystemowyPojazdu { get; set; }
        [JsonPropertyName("tokenAktualnosci")] public string? TokenAktualnosci { get; set; }
    }

    public sealed class StanPojazduDto
    {
        [JsonPropertyName("idStanuRejestracji")] public string? IdStanuRejestracji { get; set; }
        [JsonPropertyName("dataPoczatkuObowiazywania")] public string? DataPoczatkuObowiazywania { get; set; }
        [JsonPropertyName("kod")] public string? Kod { get; set; }
        [JsonPropertyName("opisSkrocony")] public string? OpisSkrocony { get; set; }
        [JsonPropertyName("opis")] public string? Opis { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    public sealed class DaneOpisujacePojazdDto
    {
        [JsonPropertyName("marka")] public string? Marka { get; set; }
        [JsonPropertyName("kodMarki")] public string? KodMarki { get; set; }
        [JsonPropertyName("model")] public string? Model { get; set; }
        [JsonPropertyName("kodModelu")] public string? KodModelu { get; set; }
        [JsonPropertyName("rodzaj")] public string? Rodzaj { get; set; }
        [JsonPropertyName("podrodzaj")] public string? Podrodzaj { get; set; }
        [JsonPropertyName("numerVin")] public string? NumerVin { get; set; }
        [JsonPropertyName("rokProdukcji")] public int? RokProdukcji { get; set; }
        [JsonPropertyName("kodRpp")] public string? KodRpp { get; set; }
    }

    public sealed class HomologacjaDto
    {
        [JsonPropertyName("idHomologacji")] public string? IdHomologacji { get; set; }
        [JsonPropertyName("wersjaKod")] public string? WersjaKod { get; set; }
        [JsonPropertyName("wariantKod")] public string? WariantKod { get; set; }
        [JsonPropertyName("typKod")] public string? TypKod { get; set; }
    }

    public sealed class PierwszaRejestracjaDto
    {
        [JsonPropertyName("wKraju")] public string? WKraju { get; set; }
        [JsonPropertyName("zaGranica")] public string? ZaGranica { get; set; }
        [JsonPropertyName("dataPierwszej")] public string? DataPierwszej { get; set; }
    }

    public sealed class BadanieTechniczneDto
    {
        [JsonPropertyName("idBadania")] public string? IdBadania { get; set; }
        [JsonPropertyName("terminKolejnegoBadania")] public string? TerminKolejnegoBadania { get; set; }
    }

    public sealed class DaneTechniczneDto
    {
        [JsonPropertyName("pojemnoscSilnika")] public decimal? PojemnoscSilnika { get; set; }
        [JsonPropertyName("mocSilnika")] public decimal? MocSilnika { get; set; }
        [JsonPropertyName("masaWlasna")] public int? MasaWlasna { get; set; }
    }

    public sealed class PojazdSprowadzonyDto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("numerRejZagraniczny")] public string? NumerRejZagraniczny { get; set; }
        [JsonPropertyName("krajKodAlfa2")] public string? KrajKodAlfa2 { get; set; }
        [JsonPropertyName("krajNazwa")] public string? KrajNazwa { get; set; }
        [JsonPropertyName("poprzedniVin")] public string? PoprzedniVin { get; set; }
    }

    public sealed class RejestracjaDto
    {
        [JsonPropertyName("idRejestracji")] public string? IdRejestracji { get; set; }
        [JsonPropertyName("organKod")] public string? OrganKod { get; set; }
        [JsonPropertyName("organNazwa")] public string? OrganNazwa { get; set; }
        [JsonPropertyName("dataRejestracji")] public string? DataRejestracji { get; set; }
        [JsonPropertyName("typKod")] public string? TypKod { get; set; }
        [JsonPropertyName("typOpisSkrocony")] public string? TypOpisSkrocony { get; set; }
    }

    public sealed class DokumentPojazduDto
    {
        [JsonPropertyName("idDokumentu")] public string? IdDokumentu { get; set; }
        [JsonPropertyName("typKod")] public string? TypKod { get; set; }
        [JsonPropertyName("typOpisSkrocony")] public string? TypOpisSkrocony { get; set; }
        [JsonPropertyName("seriaNumer")] public string? SeriaNumer { get; set; }
        [JsonPropertyName("dataWydania")] public string? DataWydania { get; set; }
        [JsonPropertyName("czyWtornik")] public string? CzyWtornik { get; set; }
        [JsonPropertyName("aktualny")] public string? Aktualny { get; set; }
        [JsonPropertyName("numerTablicy")] public string? NumerTablicy { get; set; }
        [JsonPropertyName("rodzajTablicyKod")] public string? RodzajTablicyKod { get; set; }
    }

    public sealed class PolisaOcDto
    {
        [JsonPropertyName("idPolisy")] public string? IdPolisy { get; set; }
        [JsonPropertyName("numerPolisy")] public string? NumerPolisy { get; set; }
        [JsonPropertyName("dataOd")] public string? DataOd { get; set; }
        [JsonPropertyName("dataDo")] public string? DataDo { get; set; }
    }
}
