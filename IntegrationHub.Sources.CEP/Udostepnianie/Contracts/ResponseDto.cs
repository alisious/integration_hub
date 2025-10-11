// IntegrationHub.Sources.CEP.Udostepnianie.Contracts/ResponseDto.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    // ===== META (wspólna) =====
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


    // ===== ID pojazdu (wspólna) =====
    public sealed class AktualnyIdPojazduDto
    {
        [JsonPropertyName("identyfikatorSystemowyPojazdu")] public string? IdentyfikatorSystemowyPojazdu { get; set; }
        [JsonPropertyName("tokenAktualnosci")] public string? TokenAktualnosci { get; set; }
    }

    // ===== Słowniki (wspólne/rozszerzone) =====
    public class SlownikDto
    {
        [JsonPropertyName("kod")] public string? Kod { get; set; }
        [JsonPropertyName("wartoscOpisowaSkrocona")] public string? WartoscOpisowaSkrocona { get; set; }
        [JsonPropertyName("wartoscOpisowa")] public string? WartoscOpisowa { get; set; }
    }
    public class SlownikZakresowyDto : SlownikDto
    {
        [JsonPropertyName("dataOd")] public string? DataOd { get; set; }
        [JsonPropertyName("dataDo")] public string? DataDo { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }
    public sealed class SlownikRozszerzonyDto : SlownikZakresowyDto
    {
        [JsonPropertyName("zrodlo")] public string? Zrodlo { get; set; }
        [JsonPropertyName("oznaczenie")] public string? Oznaczenie { get; set; }
    }

    // ===== TypKodOpis (dla odpowiedzi dokumentowej) =====
    public sealed class TypKodOpisDto
    {
        [JsonPropertyName("kod")] public string? Kod { get; set; }
        [JsonPropertyName("wartoscOpisowaSkrocona")] public string? WartoscOpisowaSkrocona { get; set; }
        [JsonPropertyName("wartoscOpisowa")] public string? WartoscOpisowa { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("dataOd")] public string? DataOd { get; set; }
        [JsonPropertyName("dataDo")] public string? DataDo { get; set; }
        [JsonPropertyName("dataAktualizacji")] public string? DataAktualizacji { get; set; }
    }

    // ======= Pytanie o dokument pojazdu – pochodne =======
    public sealed class DokumentPojazduFullDto
    {
        [JsonPropertyName("identyfikatorSystemowyDokumentuPojazdu")] public string? IdentyfikatorSystemowyDokumentuPojazdu { get; set; }
        [JsonPropertyName("typDokumentu")] public TypKodOpisDto? TypDokumentu { get; set; }
        [JsonPropertyName("dokumentSeriaNumer")] public string? DokumentSeriaNumer { get; set; }
        [JsonPropertyName("czyWtornik")] public string? CzyWtornik { get; set; }
        [JsonPropertyName("dataWydaniaDokumentu")] public string? DataWydaniaDokumentu { get; set; }
        [JsonPropertyName("organWydajacyDokument")] public OrganDokumentDto? OrganWydajacyDokument { get; set; }
        [JsonPropertyName("homologacjaPojazdu")] public HomologacjaDokumentDto? HomologacjaPojazdu { get; set; }
        [JsonPropertyName("daneOpisujacePojazd")] public DaneOpisujacePojazdDokumentDto? DaneOpisujacePojazd { get; set; }
        [JsonPropertyName("stanyDokumentu")] public List<StanDokumentuDto> StanyDokumentu { get; set; } = new();
        [JsonPropertyName("oznaczeniaPojazdu")] public List<OznaczeniePojazduDokumentDto> OznaczeniaPojazdu { get; set; } = new();
    }
    public sealed class OrganDokumentDto
    {
        [JsonPropertyName("kod")] public string? Kod { get; set; }
        [JsonPropertyName("nazwa")] public string? Nazwa { get; set; }
        [JsonPropertyName("numerEwidencyjny")] public string? NumerEwidencyjny { get; set; }
        [JsonPropertyName("regon")] public string? REGON { get; set; }
        [JsonPropertyName("nazwaOrganuWydajacego")] public string? NazwaOrganuWydajacego { get; set; }
        [JsonPropertyName("typ")] public TypKodOpisDto? Typ { get; set; }
    }
    public sealed class HomologacjaDokumentDto
    {
        [JsonPropertyName("identyfikatorSystemowyHomologacjiPojazdu")] public string? IdentyfikatorSystemowyHomologacjiPojazdu { get; set; }
        [JsonPropertyName("identyfikatorPozycjiKatalogowej")] public string? IdentyfikatorPozycjiKatalogowej { get; set; }
        [JsonPropertyName("numerDokumentuHomologacji")] public string? NumerDokumentuHomologacji { get; set; }
        [JsonPropertyName("wersjaKod")] public string? WersjaKod { get; set; }
        [JsonPropertyName("wariantKod")] public string? WariantKod { get; set; }
        [JsonPropertyName("typKod")] public string? TypKod { get; set; }
    }
    public sealed class DaneOpisujacePojazdDokumentDto
    {
        [JsonPropertyName("identyfikatorSystemowyDanychPojazdu")] public string? IdentyfikatorSystemowyDanychPojazdu { get; set; }
        [JsonPropertyName("marka")] public string? Marka { get; set; }
        [JsonPropertyName("kodMarki")] public string? KodMarki { get; set; }
        [JsonPropertyName("model")] public string? Model { get; set; }
        [JsonPropertyName("kodModelu")] public string? KodModelu { get; set; }
        [JsonPropertyName("rodzaj")] public string? Rodzaj { get; set; }
        [JsonPropertyName("podrodzaj")] public string? Podrodzaj { get; set; }
        [JsonPropertyName("kodRPP")] public string? KodRPP { get; set; }
        [JsonPropertyName("numerPodwoziaNadwoziaRamy")] public string? NumerPodwoziaNadwoziaRamy { get; set; }
        [JsonPropertyName("rokProdukcji")] public int? RokProdukcji { get; set; }
    }
    public sealed class StanDokumentuDto
    {
        [JsonPropertyName("identyfikatorSystemowyStanuDokumentuPojazdu")] public string? IdentyfikatorSystemowyStanuDokumentuPojazdu { get; set; }
        [JsonPropertyName("dataPoczatkuObowiazywania")] public string? DataPoczatkuObowiazywania { get; set; }
        [JsonPropertyName("stan")] public TypKodOpisDto? Stan { get; set; }
        [JsonPropertyName("organUstanawiajacyStan")] public OrganDokumentDto? OrganUstanawiajacyStan { get; set; }
        [JsonPropertyName("dataOdnotowaniaStanu")] public string? DataOdnotowaniaStanu { get; set; }
    }
    public sealed class OznaczeniePojazduDokumentDto
    {
        [JsonPropertyName("identyfikatorSystemowyOznaczenia")] public string? IdentyfikatorSystemowyOznaczenia { get; set; }
        [JsonPropertyName("typOznaczenia")] public TypKodOpisDto? TypOznaczenia { get; set; }
        [JsonPropertyName("numerOznaczenia")] public string? NumerOznaczenia { get; set; }
        [JsonPropertyName("czyWtornik")] public string? CzyWtornik { get; set; }
        [JsonPropertyName("rodzajTablicyRejestracyjnej")] public TypKodOpisDto? RodzajTablicyRejestracyjnej { get; set; }
        [JsonPropertyName("wzorTablicyRejestracyjnej")] public TypKodOpisDto? WzorTablicyRejestracyjnej { get; set; }
        [JsonPropertyName("kolorTablicyRejestracyjnej")] public TypKodOpisDto? KolorTablicyRejestracyjnej { get; set; }
        [JsonPropertyName("stanOznaczenia")] public StanOznaczeniaDokumentuDto? StanOznaczenia { get; set; }
    }
    public sealed class StanOznaczeniaDokumentuDto
    {
        [JsonPropertyName("identyfikatorSystemowyStanuOznaczeniaPojazdu")] public string? IdentyfikatorSystemowyStanuOznaczeniaPojazdu { get; set; }
        [JsonPropertyName("dataPoczatkuObowiazywania")] public string? DataPoczatkuObowiazywania { get; set; }
        [JsonPropertyName("stan")] public TypKodOpisDto? Stan { get; set; }
        [JsonPropertyName("organUstanawiajacyStan")] public OrganDokumentDto? OrganUstanawiajacyStan { get; set; }
        [JsonPropertyName("dataOdnotowaniaStanu")] public string? DataOdnotowaniaStanu { get; set; }
    }

    // ======= Pytanie o pojazd – pochodne =======
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
    public sealed class HomologacjaBasicDto
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

    // ======= Pytanie o pojazd – ROZSZERZONE pochodne =======
    public sealed class MetaRozszerzone
    {
        [JsonPropertyName("identyfikatorTransakcji")] public string? IdentyfikatorTransakcji { get; set; }
        [JsonPropertyName("iloscZwroconychRekordow")] public int? IloscZwroconychRekordow { get; set; }
        [JsonPropertyName("znacznikCzasowy")] public string? ZnacznikCzasowy { get; set; }
        [JsonPropertyName("identyfikatorSystemuZewnetrznego")] public string? IdentyfikatorSystemuZewnetrznego { get; set; }
        [JsonPropertyName("znakSprawy")] public string? ZnakSprawy { get; set; }
        [JsonPropertyName("wnioskodawca")] public string? Wnioskodawca { get; set; }
    }
    public sealed class ParametryZapytaniaRozszerzone
    {
        [JsonPropertyName("numerRejestracyjny")] public string? NumerRejestracyjny { get; set; }
        [JsonPropertyName("znakSprawy")] public string? ZnakSprawy { get; set; }
        [JsonPropertyName("wnioskodawca")] public string? Wnioskodawca { get; set; }
        [JsonPropertyName("identyfikatorSystemuZewnetrznego")] public string? IdentyfikatorSystemuZewnetrznego { get; set; }
    }
    public sealed class InformacjeSkpDto
    {
        [JsonPropertyName("identyfikatorSystemowyInformacjiSKP")] public string? IdentyfikatorSystemowyInformacjiSkp { get; set; }
        [JsonPropertyName("identyfikatorCzynnosci")] public string? IdentyfikatorCzynnosci { get; set; }
        [JsonPropertyName("stacjaKontroliPojazdow")] public SkpPodmiotDto? StacjaKontroliPojazdow { get; set; }
        [JsonPropertyName("rodzajCzynnosciSKP")] public SlownikZakresowyDto? RodzajCzynnosciSkp { get; set; }
        [JsonPropertyName("numerZaswiadczenia")] public string? NumerZaswiadczenia { get; set; }
        [JsonPropertyName("wynikCzynnosci")] public SlownikRozszerzonyDto? WynikCzynnosci { get; set; }
        [JsonPropertyName("wpisDoDokumentuPojazdu")] public bool? WpisDoDokumentuPojazdu { get; set; }
        [JsonPropertyName("wydanieZaswiadczenia")] public bool? WydanieZaswiadczenia { get; set; }
        [JsonPropertyName("dataGodzWykonaniaCzynnosciSKP")] public string? DataGodzWykonaniaCzynnosciSkp { get; set; }
        [JsonPropertyName("trybAwaryjny")] public bool? TrybAwaryjny { get; set; }
        [JsonPropertyName("terminKolejnegoBadaniaTechnicznego")] public TerminKolejnegoBadaniaDto? TerminKolejnegoBadaniaTechnicznego { get; set; }
        [JsonPropertyName("stanLicznika")] public StanLicznikaDto? StanLicznika { get; set; }
    }
    public sealed class SkpPodmiotDto
    {
        [JsonPropertyName("dataOd")] public string? DataOd { get; set; }
        [JsonPropertyName("dataDo")] public string? DataDo { get; set; }
        [JsonPropertyName("statusRekordu")] public string? StatusRekordu { get; set; }
        [JsonPropertyName("kod")] public string? Kod { get; set; }
        [JsonPropertyName("nazwa")] public string? Nazwa { get; set; }
        [JsonPropertyName("numerEwidencyjny")] public string? NumerEwidencyjny { get; set; }
        [JsonPropertyName("identyfikatorREGON")] public string? IdentyfikatorREGON { get; set; }
        [JsonPropertyName("regon")] public string? REGON { get; set; }
        [JsonPropertyName("typ")] public SlownikZakresowyDto? Typ { get; set; }
    }
    public sealed class TerminKolejnegoBadaniaDto { [JsonPropertyName("dataKolejnegoBadania")] public string? DataKolejnegoBadania { get; set; } }
    public sealed class StanLicznikaDto
    {
        [JsonPropertyName("identyfikatorSystemowyStanuLicznika")] public string? IdentyfikatorSystemowyStanuLicznika { get; set; }
        [JsonPropertyName("wartoscStanuLicznika")] public int? WartoscStanuLicznika { get; set; }
        [JsonPropertyName("jednostkaStanuLicznika")] public SlownikZakresowyDto? JednostkaStanuLicznika { get; set; }
        [JsonPropertyName("dataSpisaniaLicznika")] public string? DataSpisaniaLicznika { get; set; }
        [JsonPropertyName("podmiotWprowadzajacy")] public SkpPodmiotDto? PodmiotWprowadzajacy { get; set; }
        [JsonPropertyName("dataOdnotowania")] public string? DataOdnotowania { get; set; }
    }
    public sealed class DaneTechnicznePojazduRozszerzoneDto
    {
        [JsonPropertyName("identyfikatorSystemowyDanychTechnicznych")] public string? IdentyfikatorSystemowyDanychTechnicznych { get; set; }
        [JsonPropertyName("pojemnoscSilnika")] public int? PojemnoscSilnika { get; set; }
        [JsonPropertyName("mocSilnika")] public int? MocSilnika { get; set; }
        [JsonPropertyName("masaWlasna")] public int? MasaWlasna { get; set; }
        [JsonPropertyName("masaCalkowita")] public int? MasaCalkowita { get; set; }
        [JsonPropertyName("dopuszczalnaMasaCalkowita")] public int? DopuszczalnaMasaCalkowita { get; set; }
        [JsonPropertyName("dopuszczalnaMasaCalkowitaZespoluPojazdow")] public int? DopuszczalnaMasaCalkowitaZespoluPojazdow { get; set; }
        [JsonPropertyName("dopuszczalnaLadownoscCalkowita")] public int? DopuszczalnaLadownoscCalkowita { get; set; }
        [JsonPropertyName("maksymalnaMasaCalkowitaCiagnietejPrzyczepyZHamulcem")] public int? MaksymalnaMasaCalkowitaCiagnietejPrzyczepyZHamulcem { get; set; }
        [JsonPropertyName("maksymalnaMasaCalkowitaCiagnietejPrzyczepyBezHamulca")] public int? MaksymalnaMasaCalkowitaCiagnietejPrzyczepyBezHamulca { get; set; }
        [JsonPropertyName("liczbaOsi")] public int? LiczbaOsi { get; set; }
        [JsonPropertyName("liczbaMiejscOgolem")] public int? LiczbaMiejscOgolem { get; set; }
        [JsonPropertyName("liczbaMiejscSiedzacych")] public int? LiczbaMiejscSiedzacych { get; set; }
        [JsonPropertyName("reduktorKatalityczny")] public string? ReduktorKatalityczny { get; set; }
        [JsonPropertyName("czyHak")] public string? CzyHak { get; set; }
        [JsonPropertyName("czyKierownicaPoPrawejStronie")] public string? CzyKierownicaPoPrawejStronie { get; set; }
        [JsonPropertyName("maksymalnyDopuszczalnyNaciskOsi")] public decimal? MaksymalnyDopuszczalnyNaciskOsi { get; set; }
        [JsonPropertyName("poziomEmisjiSpalinEURODlaGmin")] public SlownikRozszerzonyDto? PoziomEmisjiSpalinEuroDlaGmin { get; set; }
        [JsonPropertyName("paliwoPodstawowe")] public PaliwoPodstawoweDto? PaliwoPodstawowe { get; set; }
    }
    public sealed class PaliwoPodstawoweDto { [JsonPropertyName("rodzajPaliwa")] public RodzajPaliwaDto? RodzajPaliwa { get; set; } }
    public sealed class RodzajPaliwaDto
    {
        [JsonPropertyName("kodPaliwa")] public string? KodPaliwa { get; set; }
        [JsonPropertyName("wartoscOpisowa")] public string? WartoscOpisowa { get; set; }
        [JsonPropertyName("kod")] public string? Kod { get; set; }
        [JsonPropertyName("dataOd")] public string? DataOd { get; set; }
        [JsonPropertyName("dataDo")] public string? DataDo { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("zrodlo")] public string? Zrodlo { get; set; }
    }
    public sealed class HomologacjaRozszerzonaDto
    {
        [JsonPropertyName("identyfikatorSystemowyHomologacjiPojazdu")] public string? IdentyfikatorSystemowyHomologacjiPojazdu { get; set; }
        [JsonPropertyName("identyfikatorPozycjiKatalogowej")] public string? IdentyfikatorPozycjiKatalogowej { get; set; }
        [JsonPropertyName("wersjaPojazdu")] public HomologacjaPozycjaDto? WersjaPojazdu { get; set; }
        [JsonPropertyName("wariantPojazdu")] public HomologacjaPozycjaDto? WariantPojazdu { get; set; }
        [JsonPropertyName("typPojazdu")] public HomologacjaTypDto? TypPojazdu { get; set; }
        [JsonPropertyName("numerDokumentuHomologacji")] public string? NumerDokumentuHomologacji { get; set; }
        [JsonPropertyName("kodKategoriiHomologacji")] public HomologacjaKategoriaDto? KodKategoriiHomologacji { get; set; }
        [JsonPropertyName("homologacjaITS")] public HomologacjaITSDto? HomologacjaITS { get; set; }
        [JsonPropertyName("typWartoscOpisowa")] public string? TypWartoscOpisowa { get; set; }
        [JsonPropertyName("wariantWartoscOpisowa")] public string? WariantWartoscOpisowa { get; set; }
        [JsonPropertyName("wersjaWartoscOpisowa")] public string? WersjaWartoscOpisowa { get; set; }
    }
    public sealed class HomologacjaPozycjaDto
    {
        [JsonPropertyName("dataOd")] public string? DataOd { get; set; }
        [JsonPropertyName("dataDo")] public string? DataDo { get; set; }
        [JsonPropertyName("statusRekordu")] public string? StatusRekordu { get; set; }
        [JsonPropertyName("kod")] public string? Kod { get; set; }
        [JsonPropertyName("kodWersji")] public string? KodWersji { get; set; }
        [JsonPropertyName("wersjaHomologacji")] public string? WersjaHomologacji { get; set; }
        [JsonPropertyName("kodWariantu")] public string? KodWariantu { get; set; }
        [JsonPropertyName("nazwaWariantu")] public string? NazwaWariantu { get; set; }
        [JsonPropertyName("zrodlo")] public string? Zrodlo { get; set; }
    }
    public sealed class HomologacjaTypDto
    {
        [JsonPropertyName("dataOd")] public string? DataOd { get; set; }
        [JsonPropertyName("dataDo")] public string? DataDo { get; set; }
        [JsonPropertyName("statusRekordu")] public string? StatusRekordu { get; set; }
        [JsonPropertyName("kod")] public string? Kod { get; set; }
        [JsonPropertyName("wartoscOpisowa")] public string? WartoscOpisowa { get; set; }
        [JsonPropertyName("zrodlo")] public string? Zrodlo { get; set; }
    }
    public sealed class HomologacjaKategoriaDto
    {
        [JsonPropertyName("dataOd")] public string? DataOd { get; set; }
        [JsonPropertyName("dataDo")] public string? DataDo { get; set; }
        [JsonPropertyName("statusRekordu")] public string? StatusRekordu { get; set; }
        [JsonPropertyName("kodKatHom")] public string? KodKatHom { get; set; }
        [JsonPropertyName("wartoscOpisowa")] public string? WartoscOpisowa { get; set; }
        [JsonPropertyName("kod")] public string? Kod { get; set; }
    }
    public sealed class HomologacjaITSDto
    {
        [JsonPropertyName("dataOd")] public string? DataOd { get; set; }
        [JsonPropertyName("dataDo")] public string? DataDo { get; set; }
        [JsonPropertyName("statusRekordu")] public string? StatusRekordu { get; set; }
        [JsonPropertyName("identyfikatorPozycjiKatalogowej")] public string? IdentyfikatorPozycjiKatalogowej { get; set; }
        [JsonPropertyName("numerSwiadectwa")] public string? NumerSwiadectwa { get; set; }
    }
    public sealed class DaneOpisujacePojazdRozszerzoneDto
    {
        [JsonPropertyName("identyfikatorSystemowyDanychPojazdu")] public string? IdentyfikatorSystemowyDanychPojazdu { get; set; }
        [JsonPropertyName("marka")] public MarkaModelDto? Marka { get; set; }
        [JsonPropertyName("model")] public MarkaModelDto? Model { get; set; }
        [JsonPropertyName("rodzaj")] public RodzajPodrodzajDto? Rodzaj { get; set; }
        [JsonPropertyName("podrodzaj")] public RodzajPodrodzajDto? Podrodzaj { get; set; }
        [JsonPropertyName("przeznaczenie")] public SlownikZakresowyDto? Przeznaczenie { get; set; }
        [JsonPropertyName("kodRPP")] public KodRppDto? KodRPP { get; set; }
        [JsonPropertyName("pochodzeniePojazdu")] public SlownikRozszerzonyDto? PochodzeniePojazdu { get; set; }
        [JsonPropertyName("czyWybityNumerIdentyfikacyjny")] public SlownikZakresowyDto? CzyWybityNumerIdentyfikacyjny { get; set; }
        [JsonPropertyName("rodzajTabliczkiZnamionowej")] public SlownikZakresowyDto? RodzajTabliczkiZnamionowej { get; set; }
        [JsonPropertyName("sposobProdukcji")] public SlownikRozszerzonyDto? SposobProdukcji { get; set; }
        [JsonPropertyName("numerPodwoziaNadwoziaRamy")] public string? NumerPodwoziaNadwoziaRamy { get; set; }
        [JsonPropertyName("rokProdukcji")] public int? RokProdukcji { get; set; }
        [JsonPropertyName("rodzajKodowaniaRPP")] public KodRppDto? RodzajKodowaniaRPP { get; set; }
    }
    public sealed class MarkaModelDto
    {
        [JsonPropertyName("dataOd")] public string? DataOd { get; set; }
        [JsonPropertyName("dataDo")] public string? DataDo { get; set; }
        [JsonPropertyName("statusRekordu")] public string? StatusRekordu { get; set; }
        [JsonPropertyName("kodMarki")] public string? KodMarki { get; set; }
        [JsonPropertyName("wartoscOpisowa")] public string? WartoscOpisowa { get; set; }
        [JsonPropertyName("kod")] public string? Kod { get; set; }
        [JsonPropertyName("pozycjeSzczegolowe")] public string? PozycjeSzczegolowe { get; set; }
    }
    public sealed class RodzajPodrodzajDto
    {
        [JsonPropertyName("dataOd")] public string? DataOd { get; set; }
        [JsonPropertyName("dataDo")] public string? DataDo { get; set; }
        [JsonPropertyName("statusRekordu")] public string? StatusRekordu { get; set; }
        [JsonPropertyName("kodRodzaj")] public string? KodRodzaj { get; set; }
        [JsonPropertyName("rodzaj")] public string? Rodzaj { get; set; }
        [JsonPropertyName("kodPodrodzaj")] public string? KodPodrodzaj { get; set; }
        [JsonPropertyName("podrodzaj")] public string? Podrodzaj { get; set; }
        [JsonPropertyName("wersja")] public string? Wersja { get; set; }
        [JsonPropertyName("zrodlo")] public string? Zrodlo { get; set; }
    }
    public sealed class KodRppDto
    {
        [JsonPropertyName("dataOd")] public string? DataOd { get; set; }
        [JsonPropertyName("dataDo")] public string? DataDo { get; set; }
        [JsonPropertyName("statusRekordu")] public string? StatusRekordu { get; set; }
        [JsonPropertyName("kodRPP")] public string? KodRPP { get; set; }
        [JsonPropertyName("rodzaj")] public string? Rodzaj { get; set; }
        [JsonPropertyName("podrodzaj")] public string? Podrodzaj { get; set; }
        [JsonPropertyName("przeznaczenie")] public string? Przeznaczenie { get; set; }
        [JsonPropertyName("wersja")] public string? Wersja { get; set; }
        [JsonPropertyName("kod")] public string? Kod { get; set; }
        [JsonPropertyName("zrodlo")] public string? Zrodlo { get; set; }
    }
    public sealed class PierwszaRejestracjaRozszerzonaDto
    {
        [JsonPropertyName("identyfikatorSystemowyPierwszejRejestracjiPojazdu")] public string? IdentyfikatorSystemowyPierwszejRejestracjiPojazdu { get; set; }
        [JsonPropertyName("dataPierwszejRejestracjiWKraju")] public string? DataPierwszejRejestracjiWKraju { get; set; }
        [JsonPropertyName("dataPierwszejRejestracjiZaGranica")] public string? DataPierwszejRejestracjiZaGranica { get; set; }
        [JsonPropertyName("dataPierwszejRejestracji")] public string? DataPierwszejRejestracji { get; set; }
    }
    public sealed class OrganRozszerzonyDto
    {
        [JsonPropertyName("dataOd")] public string? DataOd { get; set; }
        [JsonPropertyName("dataDo")] public string? DataDo { get; set; }
        [JsonPropertyName("statusRekordu")] public string? StatusRekordu { get; set; }
        [JsonPropertyName("kod")] public string? Kod { get; set; }
        [JsonPropertyName("nazwa")] public string? Nazwa { get; set; }
        [JsonPropertyName("numerEwidencyjny")] public string? NumerEwidencyjny { get; set; }
        [JsonPropertyName("identyfikatorREGON")] public string? IdentyfikatorREGON { get; set; }
        [JsonPropertyName("regon")] public string? REGON { get; set; }
        [JsonPropertyName("nazwaOrganuWydajacego")] public string? NazwaOrganuWydajacego { get; set; }
        [JsonPropertyName("typ")] public SlownikZakresowyDto? Typ { get; set; }
    }
    public sealed class RejestracjaPojazduRozszerzonaDto
    {
        [JsonPropertyName("identyfikatorSystemowyRejestracji")] public string? IdentyfikatorSystemowyRejestracji { get; set; }
        [JsonPropertyName("organRejestrujacy")] public OrganRozszerzonyDto? OrganRejestrujacy { get; set; }
        [JsonPropertyName("dataRejestracjiPojazdu")] public string? DataRejestracjiPojazdu { get; set; }
        [JsonPropertyName("typRejestracji")] public SlownikZakresowyDto? TypRejestracji { get; set; }
    }
    public sealed class DokumentPojazduRozszerzonyDto
    {
        [JsonPropertyName("identyfikatorSystemowyDokumentuPojazdu")] public string? IdentyfikatorSystemowyDokumentuPojazdu { get; set; }
        [JsonPropertyName("typDokumentu")] public SlownikRozszerzonyDto? TypDokumentu { get; set; }
        [JsonPropertyName("dokumentSeriaNumer")] public string? DokumentSeriaNumer { get; set; }
        [JsonPropertyName("czyWtornik")] public string? CzyWtornik { get; set; }
        [JsonPropertyName("dataWydaniaDokumentu")] public string? DataWydaniaDokumentu { get; set; }
        [JsonPropertyName("organWydajacyDokument")] public OrganRozszerzonyDto? OrganWydajacyDokument { get; set; }
        [JsonPropertyName("danePierwszejRejestracji")] public PierwszaRejestracjaRozszerzonaDto? DanePierwszejRejestracji { get; set; }
        [JsonPropertyName("daneOpisujacePojazd")] public DaneOpisujacePojazdRozszerzoneDto? DaneOpisujacePojazd { get; set; }
        [JsonPropertyName("homologacjaPojazdu")] public HomologacjaRozszerzonaDto? HomologacjaPojazdu { get; set; }
        [JsonPropertyName("daneTechnicznePojazdu")] public DaneTechnicznePojazduRozszerzoneDto? DaneTechnicznePojazdu { get; set; }
        [JsonPropertyName("stanDokumentu")] public StanOznaczeniaRozszerzoneDto? StanDokumentu { get; set; }
        [JsonPropertyName("czyAktualny")] public string? CzyAktualny { get; set; }
        [JsonPropertyName("oznaczenia")] public List<OznaczeniePojazduRozszerzoneDto> Oznaczenia { get; set; } = new();
    }
    public sealed class OznaczeniePojazduRozszerzoneDto
    {
        [JsonPropertyName("identyfikatorSystemowyOznaczenia")] public string? IdentyfikatorSystemowyOznaczenia { get; set; }
        [JsonPropertyName("typOznaczenia")] public SlownikRozszerzonyDto? TypOznaczenia { get; set; }
        [JsonPropertyName("numerOznaczenia")] public string? NumerOznaczenia { get; set; }
        [JsonPropertyName("rodzajTablicyRejestracyjnej")] public SlownikZakresowyDto? RodzajTablicyRejestracyjnej { get; set; }
        [JsonPropertyName("wzorTablicyRejestracyjnej")] public SlownikZakresowyDto? WzorTablicyRejestracyjnej { get; set; }
        [JsonPropertyName("kolorTablicyRejestracyjnej")] public SlownikZakresowyDto? KolorTablicyRejestracyjnej { get; set; }
        [JsonPropertyName("czyWtornik")] public string? CzyWtornik { get; set; }
        [JsonPropertyName("stanOznaczenia")] public StanOznaczeniaRozszerzoneDto? StanOznaczenia { get; set; }
    }
    public sealed class StanOznaczeniaRozszerzoneDto
    {
        [JsonPropertyName("identyfikatorSystemowyStanuOznaczeniaPojazdu")] public string? IdentyfikatorSystemowyStanuOznaczeniaPojazdu { get; set; }
        [JsonPropertyName("dataPoczatkuObowiazywania")] public string? DataPoczatkuObowiazywania { get; set; }
        [JsonPropertyName("stanOznaczenia")] public SlownikZakresowyDto? Stan { get; set; }
        [JsonPropertyName("organUstanawiajacyStan")] public OrganRozszerzonyDto? OrganUstanawiajacyStan { get; set; }
        [JsonPropertyName("dataOdnotowaniaStanu")] public string? DataOdnotowaniaStanu { get; set; }
    }
    public sealed class OznaczenieAktualnyNrRejestracyjnyDto
    {
        [JsonPropertyName("identyfikatorSystemowyOznaczenia")] public string? IdentyfikatorSystemowyOznaczenia { get; set; }
        [JsonPropertyName("typOznaczenia")] public SlownikRozszerzonyDto? TypOznaczenia { get; set; }
        [JsonPropertyName("numerOznaczenia")] public string? NumerOznaczenia { get; set; }
    }

    // ======= Własność/podmiot (rozszerzone) =======
    public class WlasnoscPodmiotuDto
    {
        [JsonPropertyName("identyfikatorSystemowyWlasnosci")] public string? IdentyfikatorSystemowyWlasnosci { get; set; }
        [JsonPropertyName("kodWlasnosci")] public SlownikZakresowyDto? KodWlasnosci { get; set; }
        [JsonPropertyName("dataZmianyPrawWlasnosci")] public string? DataZmianyPrawWlasnosci { get; set; }
        [JsonPropertyName("dataOdnotowania")] public string? DataOdnotowania { get; set; }
        [JsonPropertyName("zmianaWlasnosci")] public ZmianaWlasnosciDto? ZmianaWlasnosci { get; set; }
        [JsonPropertyName("podmiot")] public PodmiotDto? Podmiot { get; set; }
    }
    public sealed class ZmianaWlasnosciDto
    {
        [JsonPropertyName("sposobZmianyPrawWlasnosci")] public SlownikRozszerzonyDto? SposobZmianyPrawWlasnosci { get; set; }
        [JsonPropertyName("dataOdnotowania")] public string? DataOdnotowania { get; set; }
    }
    public sealed class PodmiotDto
    {
        [JsonPropertyName("identyfikatorSystemowyPodmiotu")] public string? IdentyfikatorSystemowyPodmiotu { get; set; }
        [JsonPropertyName("wariantPodmiotu")] public string? WariantPodmiotu { get; set; }
        [JsonPropertyName("firma")] public FirmaDto? Firma { get; set; }
    }
    public sealed class FirmaDto
    {
        [JsonPropertyName("REGON")] public string? REGON { get; set; }
        [JsonPropertyName("nazwaFirmy")] public string? NazwaFirmy { get; set; }
        [JsonPropertyName("nazwaFirmyDrukowana")] public string? NazwaFirmyDrukowana { get; set; }
        [JsonPropertyName("formaWlasnosci")] public SlownikZakresowyDto? FormaWlasnosci { get; set; }
        [JsonPropertyName("identyfikatorSystemowyREGON")] public string? IdentyfikatorSystemowyREGON { get; set; }
        [JsonPropertyName("adres")] public AdresDto? Adres { get; set; }
    }
    public sealed class AdresDto
    {
        [JsonPropertyName("kraj")] public KrajDto? Kraj { get; set; }
        [JsonPropertyName("kodTeryt")] public string? KodTeryt { get; set; }
        [JsonPropertyName("kodTerytWojewodztwa")] public string? KodTerytWojewodztwa { get; set; }
        [JsonPropertyName("nazwaWojewodztwaStanu")] public string? NazwaWojewodztwaStanu { get; set; }
        [JsonPropertyName("kodTerytPowiatu")] public string? KodTerytPowiatu { get; set; }
        [JsonPropertyName("nazwaPowiatuDzielnicy")] public string? NazwaPowiatuDzielnicy { get; set; }
        [JsonPropertyName("kodTerytGminy")] public string? KodTerytGminy { get; set; }
        [JsonPropertyName("nazwaGminy")] public string? NazwaGminy { get; set; }
        [JsonPropertyName("kodRodzajuGminy")] public string? KodRodzajuGminy { get; set; }
        [JsonPropertyName("kodPocztowy")] public string? KodPocztowy { get; set; }
        [JsonPropertyName("kodTerytMiejscowosci")] public string? KodTerytMiejscowosci { get; set; }
        [JsonPropertyName("nazwaMiejscowosci")] public string? NazwaMiejscowosci { get; set; }
        [JsonPropertyName("nazwaMiejscowosciPodst")] public string? NazwaMiejscowosciPodst { get; set; }
        [JsonPropertyName("kodTerytUlicy")] public string? KodTerytUlicy { get; set; }
        [JsonPropertyName("ulicaCecha")] public SlownikZakresowyDto? UlicaCecha { get; set; }
        [JsonPropertyName("nazwaUlicy")] public string? NazwaUlicy { get; set; }
        [JsonPropertyName("numerDomu")] public string? NumerDomu { get; set; }
    }
    public sealed class KrajDto
    {
        [JsonPropertyName("dataOd")] public string? DataOd { get; set; }
        [JsonPropertyName("dataDo")] public string? DataDo { get; set; }
        [JsonPropertyName("statusRekordu")] public string? StatusRekordu { get; set; }
        [JsonPropertyName("kodNumeryczny")] public string? KodNumeryczny { get; set; }
        [JsonPropertyName("kodIsoAlfa2")] public string? KodIsoAlfa2 { get; set; }
        [JsonPropertyName("kodIsoAlfa3")] public string? KodIsoAlfa3 { get; set; }
        [JsonPropertyName("kodMks")] public string? KodMks { get; set; }
        [JsonPropertyName("czyNalezyDoUE")] public bool? CzyNalezyDoUE { get; set; }
        [JsonPropertyName("nazwa")] public string? Nazwa { get; set; }
        [JsonPropertyName("obywatelstwo")] public string? Obywatelstwo { get; set; }
        [JsonPropertyName("dataAktualizacji")] public string? DataAktualizacji { get; set; }
    }
    public sealed class StanPojazduRozszerzonyDto
    {
        [JsonPropertyName("identyfikatorSystemowyStanuRejestracjiPojazdu")] public string? IdentyfikatorSystemowyStanuRejestracjiPojazdu { get; set; }
        [JsonPropertyName("dataPoczatkuObowiazywaniaStanu")] public string? DataPoczatkuObowiazywaniaStanu { get; set; }
        [JsonPropertyName("stanPojazdu")] public SlownikZakresowyDto? StanPojazdu { get; set; }
        [JsonPropertyName("typRejestracji")] public SlownikZakresowyDto? TypRejestracji { get; set; }
    }
    public sealed class PolisaOcRozszerzonaDto
    {
        [JsonPropertyName("identyfikatorPolisy")] public string? IdentyfikatorPolisy { get; set; }
        [JsonPropertyName("numerPolisy")] public string? NumerPolisy { get; set; }
        [JsonPropertyName("dataZawarciaPolisy")] public string? DataZawarciaPolisy { get; set; }
        [JsonPropertyName("dataPoczatkuObowiazywaniaPolisy")] public string? DataPoczatkuObowiazywaniaPolisy { get; set; }
        [JsonPropertyName("dataKoncaObowiazywaniaPolisy")] public string? DataKoncaObowiazywaniaPolisy { get; set; }
        [JsonPropertyName("rodzajUbezpieczenia")] public SlownikZakresowyDto? RodzajUbezpieczenia { get; set; }
        [JsonPropertyName("daneZU")] public ZakladUbezpieczenDto? DaneZU { get; set; }
        [JsonPropertyName("wariantUbezpieczenia")] public WariantUbezpieczeniaDto? WariantUbezpieczenia { get; set; }
    }
    public sealed class ZakladUbezpieczenDto
    {
        [JsonPropertyName("identyfikatorSystemowyZakladuUbezpieczen")] public string? IdentyfikatorSystemowyZakladuUbezpieczen { get; set; }
        [JsonPropertyName("identyfikatorBiznesowyZakladuUbezpieczen")] public string? IdentyfikatorBiznesowyZakladuUbezpieczen { get; set; }
        [JsonPropertyName("odmowaUdostepnienia")] public bool? OdmowaUdostepnienia { get; set; }
        [JsonPropertyName("nazwaZakladuUbezpieczen")] public string? NazwaZakladuUbezpieczen { get; set; }
        [JsonPropertyName("nazwaHandlowaZakladuUbezpieczeniowego")] public string? NazwaHandlowaZakladuUbezpieczeniowego { get; set; }
    }
    public sealed class WariantUbezpieczeniaDto
    {
        [JsonPropertyName("dataPoczatkuWariantu")] public string? DataPoczatkuWariantu { get; set; }
        [JsonPropertyName("dataKoncaWariantu")] public string? DataKoncaWariantu { get; set; }
    }
    public sealed class PojazdSprowadzonyRozszerzonyDto
    {
        [JsonPropertyName("identyfikatorSystemowyPojazduSprowadzonego")] public string? IdentyfikatorSystemowyPojazduSprowadzonego { get; set; }
        [JsonPropertyName("numerRejestracyjnyZagraniczny")] public string? NumerRejestracyjnyZagraniczny { get; set; }
        [JsonPropertyName("krajZagranicznejRejestracji")] public KrajDto? KrajZagranicznejRejestracji { get; set; }
        [JsonPropertyName("poprzedniVINZagranicznejRejestracji")] public string? PoprzedniVinZagranicznejRejestracji { get; set; }
    }
    public sealed class AktualnyStanLicznikaDto
    {
        [JsonPropertyName("historiaLicznikaWymagaWeryfikacji")] public bool? HistoriaLicznikaWymagaWeryfikacji { get; set; }
        [JsonPropertyName("licznikWymieniony")] public bool? LicznikWymieniony { get; set; }
        [JsonPropertyName("stanLicznika")] public StanLicznikaDto? StanLicznika { get; set; }
    }
}
