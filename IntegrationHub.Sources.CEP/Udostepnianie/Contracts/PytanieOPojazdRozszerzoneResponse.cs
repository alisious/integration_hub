using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    // ========== ROOT ==========
    public sealed class PytanieOPojazdRozszerzoneResponse
    {
        [JsonPropertyName("meta")]
        public MetaRozszerzone Meta { get; set; } = new();

        // Echo zapytania – tu opcjonalnie trzymamy „surowe” elementy z <parametryZapytania>
        // Jeśli nie chcesz zwracać – usuń tę sekcję.
        [JsonPropertyName("parametryZapytania")]
        public ParametryZapytaniaRozszerzone? ParametryZapytania { get; set; }

        [JsonPropertyName("pojazdRozszerzone")]
        public PojazdRozszerzoneDto? Pojazd { get; set; }
    }

    // ========== META / PARAMETRY ==========
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
        // w odpowiedzi zawarto dosłownie XML zapytania; wydzielamy to co realnie występuje
        [JsonPropertyName("numerRejestracyjny")] public string? NumerRejestracyjny { get; set; }
        [JsonPropertyName("znakSprawy")] public string? ZnakSprawy { get; set; }
        [JsonPropertyName("wnioskodawca")] public string? Wnioskodawca { get; set; }
        [JsonPropertyName("identyfikatorSystemuZewnetrznego")] public string? IdentyfikatorSystemuZewnetrznego { get; set; }
    }

    // ========== POJAZD (ROZSZERZONY) ==========
    public sealed class PojazdRozszerzoneDto
    {
        [JsonPropertyName("aktualnyIdentyfikatorPojazdu")] public AktualnyIdentyfikatorPojazduDto? AktualnyIdentyfikatorPojazdu { get; set; }
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

    public sealed class AktualnyIdentyfikatorPojazduDto
    {
        [JsonPropertyName("identyfikatorSystemowyPojazdu")] public string? IdentyfikatorSystemowyPojazdu { get; set; }
        [JsonPropertyName("tokenAktualnosci")] public string? TokenAktualnosci { get; set; }
    }

    // ======= INFORMACJE SKP =======
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

    public sealed class TerminKolejnegoBadaniaDto
    {
        [JsonPropertyName("dataKolejnegoBadania")] public string? DataKolejnegoBadania { get; set; }
    }

    public sealed class StanLicznikaDto
    {
        [JsonPropertyName("identyfikatorSystemowyStanuLicznika")] public string? IdentyfikatorSystemowyStanuLicznika { get; set; }
        [JsonPropertyName("wartoscStanuLicznika")] public int? WartoscStanuLicznika { get; set; }
        [JsonPropertyName("jednostkaStanuLicznika")] public SlownikZakresowyDto? JednostkaStanuLicznika { get; set; }
        [JsonPropertyName("dataSpisaniaLicznika")] public string? DataSpisaniaLicznika { get; set; }
        [JsonPropertyName("podmiotWprowadzajacy")] public SkpPodmiotDto? PodmiotWprowadzajacy { get; set; }
        [JsonPropertyName("dataOdnotowania")] public string? DataOdnotowania { get; set; }
    }

    // ======= DANE TECHNICZNE =======
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

    public sealed class PaliwoPodstawoweDto
    {
        [JsonPropertyName("rodzajPaliwa")] public RodzajPaliwaDto? RodzajPaliwa { get; set; }
    }

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

    // ======= HOMOLOGACJA =======
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

        // specyficzne pola w XML
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

    // ======= DANE OPISUJĄCE POJAZD =======
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

    // ======= PIERWSZA REJESTRACJA =======
    public sealed class PierwszaRejestracjaRozszerzonaDto
    {
        [JsonPropertyName("identyfikatorSystemowyPierwszejRejestracjiPojazdu")] public string? IdentyfikatorSystemowyPierwszejRejestracjiPojazdu { get; set; }
        [JsonPropertyName("dataPierwszejRejestracjiWKraju")] public string? DataPierwszejRejestracjiWKraju { get; set; }
        [JsonPropertyName("dataPierwszejRejestracjiZaGranica")] public string? DataPierwszejRejestracjiZaGranica { get; set; }
        [JsonPropertyName("dataPierwszejRejestracji")] public string? DataPierwszejRejestracji { get; set; }
    }

    // ======= DOKUMENTY =======
    public sealed class DokumentPojazduRozszerzonyDto
    {
        [JsonPropertyName("identyfikatorSystemowyDokumentuPojazdu")] public string? IdentyfikatorSystemowyDokumentuPojazdu { get; set; }

        [JsonPropertyName("typDokumentu")] public SlownikRozszerzonyDto? TypDokumentu { get; set; }
        [JsonPropertyName("dokumentSeriaNumer")] public string? DokumentSeriaNumer { get; set; }
        [JsonPropertyName("czyWtornik")] public string? CzyWtornik { get; set; }
        [JsonPropertyName("dataWydaniaDokumentu")] public string? DataWydaniaDokumentu { get; set; }

        [JsonPropertyName("organWydajacyDokument")] public OrganDto? OrganWydajacyDokument { get; set; }

        [JsonPropertyName("danePierwszejRejestracji")] public PierwszaRejestracjaRozszerzonaDto? DanePierwszejRejestracji { get; set; }
        [JsonPropertyName("daneOpisujacePojazd")] public DaneOpisujacePojazdRozszerzoneDto? DaneOpisujacePojazd { get; set; }
        [JsonPropertyName("homologacjaPojazdu")] public HomologacjaRozszerzonaDto? HomologacjaPojazdu { get; set; }
        [JsonPropertyName("daneTechnicznePojazdu")] public DaneTechnicznePojazduRozszerzoneDto? DaneTechnicznePojazdu { get; set; }

        [JsonPropertyName("stanDokumentu")] public StanOznaczeniaDto? StanDokumentu { get; set; }
        [JsonPropertyName("czyAktualny")] public string? CzyAktualny { get; set; }

        // Oznaczenia mieszczą się wewnątrz dokumentu (w pliku pojawiają się)
        [JsonPropertyName("oznaczenia")] public List<OznaczeniePojazduDto> Oznaczenia { get; set; } = new();
    }

    public sealed class OrganDto
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

    // ======= OZNACZENIA / STANY =======
    public sealed class OznaczeniePojazduDto
    {
        [JsonPropertyName("identyfikatorSystemowyOznaczenia")] public string? IdentyfikatorSystemowyOznaczenia { get; set; }
        [JsonPropertyName("typOznaczenia")] public SlownikRozszerzonyDto? TypOznaczenia { get; set; }
        [JsonPropertyName("numerOznaczenia")] public string? NumerOznaczenia { get; set; }

        [JsonPropertyName("rodzajTablicyRejestracyjnej")] public SlownikZakresowyDto? RodzajTablicyRejestracyjnej { get; set; }
        [JsonPropertyName("wzorTablicyRejestracyjnej")] public SlownikZakresowyDto? WzorTablicyRejestracyjnej { get; set; }
        [JsonPropertyName("kolorTablicyRejestracyjnej")] public SlownikZakresowyDto? KolorTablicyRejestracyjnej { get; set; }

        [JsonPropertyName("czyWtornik")] public string? CzyWtornik { get; set; }
        [JsonPropertyName("stanOznaczenia")] public StanOznaczeniaDto? StanOznaczenia { get; set; }
    }

    public sealed class StanOznaczeniaDto
    {
        [JsonPropertyName("identyfikatorSystemowyStanuOznaczeniaPojazdu")] public string? IdentyfikatorSystemowyStanuOznaczeniaPojazdu { get; set; }
        [JsonPropertyName("dataPoczatkuObowiazywania")] public string? DataPoczatkuObowiazywania { get; set; }
        [JsonPropertyName("stanOznaczenia")] public SlownikZakresowyDto? Stan { get; set; }
        [JsonPropertyName("organUstanawiajacyStan")] public OrganDto? OrganUstanawiajacyStan { get; set; }
        [JsonPropertyName("dataOdnotowaniaStanu")] public string? DataOdnotowaniaStanu { get; set; }
    }

    public sealed class OznaczenieAktualnyNrRejestracyjnyDto
    {
        [JsonPropertyName("identyfikatorSystemowyOznaczenia")] public string? IdentyfikatorSystemowyOznaczenia { get; set; }
        [JsonPropertyName("typOznaczenia")] public SlownikRozszerzonyDto? TypOznaczenia { get; set; }
        [JsonPropertyName("numerOznaczenia")] public string? NumerOznaczenia { get; set; }
    }

    // ======= WŁASNOŚĆ / PODMIOT =======
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

    // ======= STAN POJAZDU =======
    public sealed class StanPojazduRozszerzonyDto
    {
        [JsonPropertyName("identyfikatorSystemowyStanuRejestracjiPojazdu")] public string? IdentyfikatorSystemowyStanuRejestracjiPojazdu { get; set; }
        [JsonPropertyName("dataPoczatkuObowiazywaniaStanu")] public string? DataPoczatkuObowiazywaniaStanu { get; set; }
        [JsonPropertyName("stanPojazdu")] public SlownikZakresowyDto? StanPojazdu { get; set; }
        [JsonPropertyName("typRejestracji")] public SlownikZakresowyDto? TypRejestracji { get; set; }
    }

    public sealed class NajnowszyWariantPodmiotuDto : WlasnoscPodmiotuDto { } // te same pola co węzeł „wlasnoscPodmiotu” (w pliku występuje duplikat)

    // ======= REJESTRACJE =======
    public sealed class RejestracjaPojazduRozszerzonaDto
    {
        [JsonPropertyName("identyfikatorSystemowyRejestracji")] public string? IdentyfikatorSystemowyRejestracji { get; set; }
        [JsonPropertyName("organRejestrujacy")] public OrganDto? OrganRejestrujacy { get; set; }
        [JsonPropertyName("dataRejestracjiPojazdu")] public string? DataRejestracjiPojazdu { get; set; }
        [JsonPropertyName("typRejestracji")] public SlownikZakresowyDto? TypRejestracji { get; set; }
    }

    // ======= POLISA OC =======
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
        [JsonPropertyName("odmowaUdostepnienia")] public bool? OdmowaUdostepnienia { get; set; } // atrybut z XML
        [JsonPropertyName("nazwaZakladuUbezpieczen")] public string? NazwaZakladuUbezpieczen { get; set; }
        [JsonPropertyName("nazwaHandlowaZakladuUbezpieczeniowego")] public string? NazwaHandlowaZakladuUbezpieczeniowego { get; set; }
    }

    public sealed class WariantUbezpieczeniaDto
    {
        [JsonPropertyName("dataPoczatkuWariantu")] public string? DataPoczatkuWariantu { get; set; }
        [JsonPropertyName("dataKoncaWariantu")] public string? DataKoncaWariantu { get; set; }
    }

    // ======= POJAZD SPROWADZONY =======
    public sealed class PojazdSprowadzonyRozszerzonyDto
    {
        [JsonPropertyName("identyfikatorSystemowyPojazduSprowadzonego")] public string? IdentyfikatorSystemowyPojazduSprowadzonego { get; set; }
        [JsonPropertyName("numerRejestracyjnyZagraniczny")] public string? NumerRejestracyjnyZagraniczny { get; set; }
        [JsonPropertyName("krajZagranicznejRejestracji")] public KrajDto? KrajZagranicznejRejestracji { get; set; }
        [JsonPropertyName("poprzedniVINZagranicznejRejestracji")] public string? PoprzedniVinZagranicznejRejestracji { get; set; }
    }

    // ======= AKTUALNY NR REJ / LICZNIK =======
    
    public sealed class AktualnyStanLicznikaDto
    {
        [JsonPropertyName("historiaLicznikaWymagaWeryfikacji")] public bool? HistoriaLicznikaWymagaWeryfikacji { get; set; }
        [JsonPropertyName("licznikWymieniony")] public bool? LicznikWymieniony { get; set; }
        [JsonPropertyName("stanLicznika")] public StanLicznikaDto? StanLicznika { get; set; }
    }

    // ======= WSPÓLNE PROSTE TYPY SŁOWNIKOWE =======
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
        // Dla kilku węzłów: (np. „kod” jest nadany alternatywnie jako różne nazwy)
        [JsonPropertyName("oznaczenie")] public string? Oznaczenie { get; set; }
    }
}
