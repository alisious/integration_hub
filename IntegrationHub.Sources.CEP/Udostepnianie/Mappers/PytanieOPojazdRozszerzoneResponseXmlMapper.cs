using System.Globalization;
using System.Xml.Linq;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Mappers
{
    public static class PytanieOPojazdRozszerzoneResponseXmlMapper
    {
        private static readonly XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
        private static readonly XNamespace elem = "http://elementy.cep.udo.api.cepik.coi.gov.pl";

        public static PytanieOPojazdRozszerzoneResponse Parse(string xml)
        {
            var doc = XDocument.Parse(xml);

            var body = doc.Root?.Element(soap + "Body");
            var rezultat = body?.Elements().FirstOrDefault(x => x.Name.LocalName == "pytanieOPojazdRozszerzoneRezultat");

            var resp = new PytanieOPojazdRozszerzoneResponse();

            if (rezultat is null)
                return resp;

            // ===== daneRezultatu -> Meta
            var daneRez = rezultat.ElementAnyNs("daneRezultatu");
            if (daneRez != null)
            {
                resp.Meta = new MetaRozszerzone
                {
                    IdentyfikatorTransakcji = daneRez.Val("identyfikatorTransakcji"),
                    IloscZwroconychRekordow = daneRez.Val("iloscZwroconychRekordow").ToInt(),
                    ZnacznikCzasowy = daneRez.Val("znacznikCzasowy"),
                    IdentyfikatorSystemuZewnetrznego = daneRez.Val("identyfikatorSystemuZewnetrznego"),
                    ZnakSprawy = daneRez.Val("znakSprawy"),
                    Wnioskodawca = daneRez.Val("wnioskodawca")
                };
            }

            // ===== opcjonalne echo parametrow
            var echo = rezultat.ElementAnyNs("parametryZapytania");
            if (echo != null)
            {
                resp.ParametryZapytania = new ParametryZapytaniaRozszerzone
                {
                    NumerRejestracyjny = echo.Desc("parametryPojazdu")?.Desc("parametryDanychPojazdu")?
                        .Desc("parametryOznaczeniaPojazdu")?.Val("numerRejestracyjny"),
                    ZnakSprawy = echo.Desc("parametryPytania")?.Desc("wnioskodawca")?.Val("znakSprawy"),
                    Wnioskodawca = echo.Desc("parametryPytania")?.Desc("wnioskodawca")?.Val("wnioskodawca"),
                    IdentyfikatorSystemuZewnetrznego = echo.Desc("parametryPytania")?.Val("identyfikatorSystemuZewnetrznego")
                };
            }

            // ===== pojazdRozszerzone
            var poj = rezultat.ElementAnyNs("pojazdRozszerzone");
            if (poj == null)
                return resp;

            var p = new PojazdRozszerzoneDto();

            // aktualnyIdentyfikatorPojazdu
            var aid = poj.Desc("aktualnyIdentyfikatorPojazdu");
            if (aid != null)
            {
                p.AktualnyIdentyfikatorPojazdu = new AktualnyIdentyfikatorPojazduDto
                {
                    IdentyfikatorSystemowyPojazdu = aid.Val("identyfikatorSystemowyPojazdu"),
                    TokenAktualnosci = aid.Val("tokenAktualnosci")
                };
            }

            // stanPojazdu (rozszerzony)
            var st = poj.Desc("stanPojazdu");
            if (st != null)
            {
                p.StanPojazdu = new StanPojazduRozszerzonyDto
                {
                    IdentyfikatorSystemowyStanuRejestracjiPojazdu = st.Val("identyfikatorSystemowyStanuRejestracjiPojazdu"),
                    DataPoczatkuObowiazywaniaStanu = st.Val("dataPoczatkuObowiazywaniaStanu"),
                    StanPojazdu = st.Desc("stanPojazdu")?.ToSlownikZakresowy(),
                    TypRejestracji = st.Desc("typrejestracji")?.ToSlownikZakresowy()
                };
            }

            // daneOpisujacePojazd
            var dop = poj.Desc("daneOpisujacePojazd");
            if (dop != null)
            {
                p.DaneOpisujacePojazd = new DaneOpisujacePojazdRozszerzoneDto
                {
                    IdentyfikatorSystemowyDanychPojazdu = dop.Val("identyfikatorSystemowyDanychPojazdu"),
                    Marka = dop.Desc("marka")?.ToMarkaModel(),
                    Model = dop.Desc("model")?.ToMarkaModel(),
                    Rodzaj = dop.Desc("rodzaj")?.ToRodzajPodrodzaj(),
                    Podrodzaj = dop.Desc("podrodzaj")?.ToRodzajPodrodzaj(),
                    Przeznaczenie = dop.Desc("przeznaczenie")?.ToSlownikZakresowy(),
                    KodRPP = dop.Desc("kodRPP")?.ToKodRpp(),
                    PochodzeniePojazdu = dop.Desc("pochodzeniePojazdu")?.ToSlownikRozszerzony(),
                    CzyWybityNumerIdentyfikacyjny = dop.Desc("czyWybityNumerIdentyfikacyjny")?.ToSlownikZakresowy(),
                    RodzajTabliczkiZnamionowej = dop.Desc("rodzajTabliczkiZnamionowej")?.ToSlownikZakresowy(),
                    SposobProdukcji = dop.Desc("sposobProdukcji")?.ToSlownikRozszerzony(),
                    NumerPodwoziaNadwoziaRamy = dop.Val("numerPodwoziaNadwoziaRamy"),
                    RokProdukcji = dop.Val("rokProdukcji").ToInt(),
                    RodzajKodowaniaRPP = dop.Desc("rodzajKodowaniaRPP")?.ToKodRpp()
                };
            }

            // homologacjaPojazdu
            var hom = poj.Desc("homologacjaPojazdu");
            if (hom != null)
            {
                p.HomologacjaPojazdu = new HomologacjaRozszerzonaDto
                {
                    IdentyfikatorSystemowyHomologacjiPojazdu = hom.Val("identyfikatorSystemowyHomologacjiPojazdu"),
                    IdentyfikatorPozycjiKatalogowej = hom.Val("identyfikatorPozycjiKatalogowej"),
                    WersjaPojazdu = hom.Desc("wersjaPojazdu")?.ToHomologacjaPozycja(),
                    WariantPojazdu = hom.Desc("wariantPojazdu")?.ToHomologacjaPozycja(),
                    TypPojazdu = hom.Desc("typPojazdu")?.ToHomologacjaTyp(),
                    NumerDokumentuHomologacji = hom.Val("numerDokumentuHomologacji"),
                    KodKategoriiHomologacji = hom.Desc("kodKategoriiHomologacji")?.ToHomologacjaKategoria(),
                    HomologacjaITS = hom.Desc("homologacjaITS")?.ToHomologacjaITS(),
                    TypWartoscOpisowa = hom.Val("typWartoscOpisowa"),
                    WariantWartoscOpisowa = hom.Val("wariantWartoscOpisowa"),
                    WersjaWartoscOpisowa = hom.Val("wersjaWartoscOpisowa")
                };
            }

            // danePierwszejRejestracji
            var pr = poj.Desc("danePierwszejRejestracji");
            if (pr != null)
            {
                p.DanePierwszejRejestracji = new PierwszaRejestracjaRozszerzonaDto
                {
                    IdentyfikatorSystemowyPierwszejRejestracjiPojazdu = pr.Val("identyfikatorSystemowyPierwszejRejestracjiPojazdu"),
                    DataPierwszejRejestracjiWKraju = pr.Val("dataPierwszejRejestracjiWKraju"),
                    DataPierwszejRejestracjiZaGranica = pr.Val("dataPierwszejRejestracjiZaGranica"),
                    DataPierwszejRejestracji = pr.Val("dataPierwszejRejestracji")
                };
            }

            // informacjeSKP
            var skp = poj.Desc("informacjeSKP");
            if (skp != null)
            {
                p.InformacjeSkp = new InformacjeSkpDto
                {
                    IdentyfikatorSystemowyInformacjiSkp = skp.Val("identyfikatorSystemowyInformacjiSKP"),
                    IdentyfikatorCzynnosci = skp.Val("identyfikatorCzynnosci"),
                    StacjaKontroliPojazdow = skp.Desc("stacjaKontroliPojazdow")?.ToSkpPodmiot(),
                    RodzajCzynnosciSkp = skp.Desc("rodzajCzynnosciSKP")?.ToSlownikZakresowy(),
                    NumerZaswiadczenia = skp.Val("numerZaswiadczenia"),
                    WynikCzynnosci = skp.Desc("wynikCzynnosci")?.ToSlownikRozszerzony(),
                    WpisDoDokumentuPojazdu = skp.Val("wpisDoDokumentuPojazdu").ToBool(),
                    WydanieZaswiadczenia = skp.Val("wydanieZaswiadczenia").ToBool(),
                    DataGodzWykonaniaCzynnosciSkp = skp.Val("dataGodzWykonaniaCzynnosciSKP"),
                    TrybAwaryjny = skp.Val("trybAwaryjny").ToBool(),
                    TerminKolejnegoBadaniaTechnicznego = skp.Desc("terminKolejnegoBadaniaTechnicznego")?.ToTerminKolejnegoBadania(),
                    StanLicznika = skp.Desc("stanLicznika")?.ToStanLicznika()
                };
            }

            // daneTechnicznePojazdu
            var dt = poj.Desc("daneTechnicznePojazdu");
            if (dt != null)
            {
                p.DaneTechnicznePojazdu = new DaneTechnicznePojazduRozszerzoneDto
                {
                    IdentyfikatorSystemowyDanychTechnicznych = dt.Val("identyfikatorSystemowyDanychTechnicznych"),
                    PojemnoscSilnika = dt.Val("pojemnoscSilnika").ToInt(),
                    MocSilnika = dt.Val("mocSilnika").ToInt(),
                    MasaWlasna = dt.Val("masaWlasna").ToInt(),
                    MasaCalkowita = dt.Val("maksymalnaMasaCalkowita").ToInt(), // jeżeli w XML pole to „maksymalna...”
                    DopuszczalnaMasaCalkowita = dt.Val("dopuszczalnaMasaCalkowita").ToInt(),
                    DopuszczalnaMasaCalkowitaZespoluPojazdow = dt.Val("dopuszczalnaMasaCalkowitaZestawu").ToInt(),
                    DopuszczalnaLadownoscCalkowita = dt.Val("dopuszczalnaLadownosc").ToInt(),
                    MaksymalnaMasaCalkowitaCiagnietejPrzyczepyZHamulcem = dt.Val("dopuszczalnaMasaCalkowitaPrzyczepyZHam").ToInt(),
                    MaksymalnaMasaCalkowitaCiagnietejPrzyczepyBezHamulca = dt.Val("dopuszczalnaMasaCalkowitaPrzyczepyBezHam").ToInt(),
                    LiczbaOsi = dt.Val("liczbaOsi").ToInt(),
                    LiczbaMiejscOgolem = null, // w wielu odpowiedziach brak – zostawiamy null
                    LiczbaMiejscSiedzacych = dt.Val("liczbaMiejscSiedzacych").ToInt(),
                    ReduktorKatalityczny = dt.Val("reduktorKatalityczny"),
                    CzyHak = dt.Val("czyHak"),
                    CzyKierownicaPoPrawejStronie = dt.Val("czyKierownicaPoPrawejStronie"),
                    MaksymalnyDopuszczalnyNaciskOsi = dt.Val("masaNaOs").ToDecimal(),
                    PoziomEmisjiSpalinEuroDlaGmin = dt.Desc("poziomEmisjiSpalinEURODlaGmin")?.ToSlownikRozszerzony(),
                    PaliwoPodstawowe = dt.Desc("paliwoPodstawowe")?.ToPaliwoPodstawowe()
                };
            }

            // danePojazduSprowadzonego
            var spr = poj.Desc("danePojazduSprowadzonego");
            if (spr != null)
            {
                p.DanePojazduSprowadzonego = new PojazdSprowadzonyRozszerzonyDto
                {
                    IdentyfikatorSystemowyPojazduSprowadzonego = spr.Val("identyfikatorSystemowyPojazduSprowadzonego"),
                    NumerRejestracyjnyZagraniczny = spr.Val("numerRejestracyjnyZagraniczny"),
                    KrajZagranicznejRejestracji = spr.Desc("krajZagranicznejRejestracji")?.ToKraj(),
                    PoprzedniVinZagranicznejRejestracji = spr.Val("poprzedniVINZagranicznejRejestracji")
                };
            }

            // rejestracjaPojazdu (wiele)
            foreach (var re in poj.DescMany("rejestracjaPojazdu"))
            {
                p.RejestracjePojazdu.Add(new RejestracjaPojazduRozszerzonaDto
                {
                    IdentyfikatorSystemowyRejestracji = re.Val("identyfikatorSystemowyRejestracji"),
                    OrganRejestrujacy = re.Desc("organRejestrujacy")?.ToOrgan(),
                    DataRejestracjiPojazdu = re.Val("dataRejestracjiPojazdu"),
                    TypRejestracji = re.Desc("typrejestracji")?.ToSlownikZakresowy()
                });
            }

            // dokumentPojazdu (wiele)
            foreach (var d in poj.DescMany("dokumentPojazdu"))
            {
                var dokDto = new DokumentPojazduRozszerzonyDto
                {
                    IdentyfikatorSystemowyDokumentuPojazdu = d.Val("identyfikatorSystemowyDokumentuPojazdu"),
                    TypDokumentu = d.Desc("typDokumentu")?.ToSlownikRozszerzony(),
                    DokumentSeriaNumer = d.Val("dokumentSeriaNumer"),
                    CzyWtornik = d.Val("czyWtornik"),
                    DataWydaniaDokumentu = d.Val("dataWydaniaDokumentu"),
                    OrganWydajacyDokument = d.Desc("organWydajacyDokument")?.ToOrgan(),
                    DanePierwszejRejestracji = d.Desc("danePierwszejRejestracji")?.ToPierwszaRejestracja(),
                    DaneOpisujacePojazd = d.Desc("daneOpisujacePojazd")?.ToDaneOpisujace(),
                    HomologacjaPojazdu = d.Desc("homologacjaPojazdu")?.ToHomologacjaRozszerzona(),
                    DaneTechnicznePojazdu = d.Desc("daneTechnicznePojazdu")?.ToDaneTechniczne(),
                    StanDokumentu = d.Desc("stanDokumentu")?.ToStanOznaczenia(),
                    CzyAktualny = d.Val("czyAktualny"),
                };

                foreach (var ozn in d.DescMany("oznaczeniePojazdu"))
                {
                    dokDto.Oznaczenia.Add(ozn.ToOznaczeniePojazdu());
                }

                p.DokumentPojazdu.Add(dokDto);
            }


            // własność / najnowszyWariantPodmiotu
            var nvp = poj.Desc("najnowszyWariantPodmiotu");
            if (nvp != null)
            {
                p.NajnowszyWariantPodmiotu = new NajnowszyWariantPodmiotuDto
                {
                    IdentyfikatorSystemowyWlasnosci = nvp.Val("identyfikatorSystemowyWlasnosci"),
                    KodWlasnosci = nvp.Desc("kodWlasnosci")?.ToSlownikZakresowy(),
                    DataZmianyPrawWlasnosci = nvp.Val("dataZmianyPrawWlasnosci"),
                    DataOdnotowania = nvp.Val("dataOdnotowania"),
                    ZmianaWlasnosci = nvp.Desc("zmianaWlasnosci")?.ToZmianaWlasnosci(),
                    Podmiot = nvp.Desc("podmiot")?.ToPodmiot()
                };
            }

            // danePolisyOC
            var oc = poj.Desc("danePolisyOC");
            if (oc != null)
            {
                p.DanePolisyOc = new PolisaOcRozszerzonaDto
                {
                    IdentyfikatorPolisy = oc.Val("identyfikatorPolisy"),
                    NumerPolisy = oc.Val("numerPolisy"),
                    DataZawarciaPolisy = oc.Val("dataZawarciaPolisy"),
                    DataPoczatkuObowiazywaniaPolisy = oc.Val("dataPoczatkuObowiazywaniaPolisy"),
                    DataKoncaObowiazywaniaPolisy = oc.Val("dataKoncaObowiazywaniaPolisy"),
                    RodzajUbezpieczenia = oc.Desc("rodzajUbezpieczenia")?.ToSlownikZakresowy(),
                    DaneZU = oc.Desc("daneZU")?.ToZu(),
                    WariantUbezpieczenia = oc.Desc("wariantUbezpieczenia")?.ToWariantUbezpieczenia()
                };
            }

            // oznaczeniePojazduAktualnyNrRejestracyjny
            var anr = poj.Desc("oznaczeniePojazduAktualnyNrRejestracyjny");
            if (anr != null)
            {
                p.OznaczenieAktualnyNrRejestracyjny = new OznaczenieAktualnyNrRejestracyjnyDto
                {
                    IdentyfikatorSystemowyOznaczenia = anr.Val("identyfikatorSystemowyOznaczenia"),
                    TypOznaczenia = anr.Desc("typOznaczenia")?.ToSlownikRozszerzony(),
                    NumerOznaczenia = anr.Val("numerOznaczenia")
                };
            }

            // aktualnyStanLicznika
            var asl = poj.Desc("aktualnyStanLicznika");
            if (asl != null)
            {
                p.AktualnyStanLicznika = new AktualnyStanLicznikaDto
                {
                    HistoriaLicznikaWymagaWeryfikacji = asl.Val("historiaLicznikaWymagaWeryfikacji").ToBool(),
                    LicznikWymieniony = asl.Val("licznikWymieniony").ToBool(),
                    StanLicznika = asl.Desc("stanLicznika")?.ToStanLicznika()
                };
            }

            resp.Pojazd = p;
            return resp;
        }

        // ====================== HELPERS ======================

        private static XElement? ElementAnyNs(this XElement parent, XName name) => parent.Element(name);
        private static XElement? ElementAnyNs(this XElement parent, string localName) =>
            parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName);

        private static IEnumerable<XElement> ElementsAnyNs(this XElement parent, string localName) =>
            parent.Elements().Where(e => e.Name.LocalName == localName);

        private static XElement? Desc(this XElement parent, string local) => parent.ElementAnyNs(local);
        private static IEnumerable<XElement> DescMany(this XElement parent, string local) => parent.ElementsAnyNs(local);

        private static string? Val(this XElement? el, string local) =>
            el?.ElementsAnyNs(local).FirstOrDefault()?.Value;

        private static int? ToInt(this string? s) =>
            int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : null;

        private static decimal? ToDecimal(this string? s) =>
            decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;

        private static bool? ToBool(this string? s)
        {
            if (s == null) return null;
            if (bool.TryParse(s, out var b)) return b;
            // czasem w odpowiedziach przychodzą „1/0”, „T/N”
            if (s == "1" || s.Equals("T", StringComparison.OrdinalIgnoreCase) || s.Equals("TAK", StringComparison.OrdinalIgnoreCase)) return true;
            if (s == "0" || s.Equals("N", StringComparison.OrdinalIgnoreCase) || s.Equals("NIE", StringComparison.OrdinalIgnoreCase)) return false;
            return null;
        }

        // --------- typed mappers ---------

        private static SlownikZakresowyDto ToSlownikZakresowy(this XElement e) => new()
        {
            DataOd = e.Val("dataOd"),
            DataDo = e.Val("dataDo"),
            Status = e.Val("status"),
            Kod = e.Val("kod"),
            WartoscOpisowaSkrocona = e.Val("wartoscOpisowaSkrocona"),
            WartoscOpisowa = e.Val("wartoscOpisowa")
        };

        private static SlownikRozszerzonyDto ToSlownikRozszerzony(this XElement e)
        {
            var s = e.ToSlownikZakresowy();
            return new SlownikRozszerzonyDto
            {
                DataOd = s.DataOd,
                DataDo = s.DataDo,
                Status = s.Status,
                Kod = s.Kod,
                WartoscOpisowaSkrocona = s.WartoscOpisowaSkrocona,
                WartoscOpisowa = s.WartoscOpisowa,
                Zrodlo = e.Val("zrodlo"),
                Oznaczenie = e.Val("oznaczenie")
            };
        }

        private static SkpPodmiotDto ToSkpPodmiot(this XElement e) => new()
        {
            DataOd = e.Val("dataOd"),
            DataDo = e.Val("dataDo"),
            StatusRekordu = e.Val("statusRekordu"),
            Kod = e.Val("kod"),
            Nazwa = e.Val("nazwa"),
            NumerEwidencyjny = e.Val("numerEwidencyjny"),
            IdentyfikatorREGON = e.Val("identyfikatorREGON"),
            REGON = e.Val("REGON"),
            Typ = e.Desc("typ")?.ToSlownikZakresowy()
        };

        private static TerminKolejnegoBadaniaDto ToTerminKolejnegoBadania(this XElement e) => new()
        {
            DataKolejnegoBadania = e.Val("dataKolejnegoBadania")
        };

        private static StanLicznikaDto ToStanLicznika(this XElement e) => new()
        {
            IdentyfikatorSystemowyStanuLicznika = e.Val("identyfikatorSystemowyStanuLicznika"),
            WartoscStanuLicznika = e.Val("wartoscStanuLicznika").ToInt(),
            JednostkaStanuLicznika = e.Desc("jednostkaMiary")?.ToSlownikZakresowy(),
            DataSpisaniaLicznika = e.Val("dataSpisaniaLicznika"),
            PodmiotWprowadzajacy = e.Desc("podmiotWprowadzajacy")?.ToSkpPodmiot(),
            DataOdnotowania = e.Val("dataOdnotowania")
        };

        private static PaliwoPodstawoweDto ToPaliwoPodstawowe(this XElement e) => new()
        {
            RodzajPaliwa = e.Desc("rodzajPaliwa")?.ToRodzajPaliwa()
        };

        private static RodzajPaliwaDto ToRodzajPaliwa(this XElement e) => new()
        {
            KodPaliwa = e.Val("kodPaliwa"),
            WartoscOpisowa = e.Val("wartoscOpisowa"),
            Kod = e.Val("kod"),
            DataOd = e.Val("dataOd"),
            DataDo = e.Val("dataDo"),
            Status = e.Val("status"),
            Zrodlo = e.Val("zrodlo")
        };

        private static HomologacjaPozycjaDto ToHomologacjaPozycja(this XElement e) => new()
        {
            DataOd = e.Val("dataOd"),
            DataDo = e.Val("dataDo"),
            StatusRekordu = e.Val("statusRekordu"),
            Kod = e.Val("kod"),
            KodWersji = e.Val("kodWersji"),
            WersjaHomologacji = e.Val("wersjaHomologacji"),
            KodWariantu = e.Val("kodWariantu"),
            NazwaWariantu = e.Val("nazwaWariantu"),
            Zrodlo = e.Val("zrodlo")
        };

        private static HomologacjaTypDto ToHomologacjaTyp(this XElement e) => new()
        {
            DataOd = e.Val("dataOd"),
            DataDo = e.Val("dataDo"),
            StatusRekordu = e.Val("statusRekordu"),
            Kod = e.Val("kod"),
            WartoscOpisowa = e.Val("wartoscOpisowa"),
            Zrodlo = e.Val("zrodlo")
        };

        private static HomologacjaKategoriaDto ToHomologacjaKategoria(this XElement e) => new()
        {
            DataOd = e.Val("dataOd"),
            DataDo = e.Val("dataDo"),
            StatusRekordu = e.Val("statusRekordu"),
            KodKatHom = e.Val("kodKatHom"),
            WartoscOpisowa = e.Val("wartoscOpisowa"),
            Kod = e.Val("kod")
        };

        private static HomologacjaITSDto ToHomologacjaITS(this XElement e) => new()
        {
            DataOd = e.Val("dataOd"),
            DataDo = e.Val("dataDo"),
            StatusRekordu = e.Val("statusRekordu"),
            IdentyfikatorPozycjiKatalogowej = e.Val("identyfikatorPozycjiKatalogowej"),
            NumerSwiadectwa = e.Val("numerSwiadectwa")
        };

        private static MarkaModelDto ToMarkaModel(this XElement e) => new()
        {
            DataOd = e.Val("dataOd"),
            DataDo = e.Val("dataDo"),
            StatusRekordu = e.Val("statusRekordu"),
            KodMarki = e.Val("kodMarki"),
            WartoscOpisowa = e.Val("wartoscOpisowa"),
            Kod = e.Val("kod"),
            PozycjeSzczegolowe = e.Val("pozycjeSzczegolowe")
        };

        private static RodzajPodrodzajDto ToRodzajPodrodzaj(this XElement e) => new()
        {
            DataOd = e.Val("dataOd"),
            DataDo = e.Val("dataDo"),
            StatusRekordu = e.Val("statusRekordu"),
            KodRodzaj = e.Val("kodRodzaj"),
            Rodzaj = e.Val("rodzaj"),
            KodPodrodzaj = e.Val("kodPodrodzaj"),
            Podrodzaj = e.Val("podrodzaj"),
            Wersja = e.Val("wersja"),
            Zrodlo = e.Val("zrodlo")
        };

        private static KodRppDto ToKodRpp(this XElement e) => new()
        {
            DataOd = e.Val("dataOd"),
            DataDo = e.Val("dataDo"),
            StatusRekordu = e.Val("statusRekordu"),
            KodRPP = e.Val("kodRPP"),
            Rodzaj = e.Val("rodzaj"),
            Podrodzaj = e.Val("podrodzaj"),
            Przeznaczenie = e.Val("przeznaczenie"),
            Wersja = e.Val("wersja"),
            Kod = e.Val("kod"),
            Zrodlo = e.Val("zrodlo")
        };

        private static PierwszaRejestracjaRozszerzonaDto ToPierwszaRejestracja(this XElement e) => new()
        {
            IdentyfikatorSystemowyPierwszejRejestracjiPojazdu = e.Val("identyfikatorSystemowyPierwszejRejestracjiPojazdu"),
            DataPierwszejRejestracjiWKraju = e.Val("dataPierwszejRejestracjiWKraju"),
            DataPierwszejRejestracjiZaGranica = e.Val("dataPierwszejRejestracjiZaGranica"),
            DataPierwszejRejestracji = e.Val("dataPierwszejRejestracji")
        };

        private static DaneOpisujacePojazdRozszerzoneDto ToDaneOpisujace(this XElement e) => new()
        {
            IdentyfikatorSystemowyDanychPojazdu = e.Val("identyfikatorSystemowyDanychPojazdu"),
            Marka = e.Desc("marka")?.ToMarkaModel(),
            Model = e.Desc("model")?.ToMarkaModel(),
            Rodzaj = e.Desc("rodzaj")?.ToRodzajPodrodzaj(),
            Podrodzaj = e.Desc("podrodzaj")?.ToRodzajPodrodzaj(),
            Przeznaczenie = e.Desc("przeznaczenie")?.ToSlownikZakresowy(),
            KodRPP = e.Desc("kodRPP")?.ToKodRpp(),
            PochodzeniePojazdu = e.Desc("pochodzeniePojazdu")?.ToSlownikRozszerzony(),
            CzyWybityNumerIdentyfikacyjny = e.Desc("czyWybityNumerIdentyfikacyjny")?.ToSlownikZakresowy(),
            RodzajTabliczkiZnamionowej = e.Desc("rodzajTabliczkiZnamionowej")?.ToSlownikZakresowy(),
            SposobProdukcji = e.Desc("sposobProdukcji")?.ToSlownikRozszerzony(),
            NumerPodwoziaNadwoziaRamy = e.Val("numerPodwoziaNadwoziaRamy"),
            RokProdukcji = e.Val("rokProdukcji").ToInt(),
            RodzajKodowaniaRPP = e.Desc("rodzajKodowaniaRPP")?.ToKodRpp()
        };

        private static OrganDto ToOrgan(this XElement e) => new()
        {
            DataOd = e.Val("dataOd"),
            DataDo = e.Val("dataDo"),
            StatusRekordu = e.Val("statusRekordu"),
            Kod = e.Val("kod"),
            Nazwa = e.Val("nazwa"),
            NumerEwidencyjny = e.Val("numerEwidencyjny"),
            IdentyfikatorREGON = e.Val("identyfikatorREGON"),
            REGON = e.Val("REGON"),
            NazwaOrganuWydajacego = e.Val("nazwaOrganuWydajacego"),
            Typ = e.Desc("typ")?.ToSlownikZakresowy()
        };

        private static StanOznaczeniaDto ToStanOznaczenia(this XElement e) => new()
        {
            IdentyfikatorSystemowyStanuOznaczeniaPojazdu = e.Val("identyfikatorSystemowyStanuOznaczeniaPojazdu"),
            DataPoczatkuObowiazywania = e.Val("dataPoczatkuObowiazywania"),
            Stan = e.Desc("stanOznaczenia")?.ToSlownikZakresowy(),
            OrganUstanawiajacyStan = e.Desc("organUstanawiajacyStan")?.ToOrgan(),
            DataOdnotowaniaStanu = e.Val("dataOdnotowaniaStanu")
        };

        private static OznaczeniePojazduDto ToOznaczeniePojazdu(this XElement e) => new()
        {
            IdentyfikatorSystemowyOznaczenia = e.Val("identyfikatorSystemowyOznaczenia"),
            TypOznaczenia = e.Desc("typOznaczenia")?.ToSlownikRozszerzony(),
            NumerOznaczenia = e.Val("numerOznaczenia"),
            RodzajTablicyRejestracyjnej = e.Desc("rodzajTablicyRejestracyjnej")?.ToSlownikZakresowy(),
            WzorTablicyRejestracyjnej = e.Desc("wzorTablicyRejestracyjnej")?.ToSlownikZakresowy(),
            KolorTablicyRejestracyjnej = e.Desc("kolorTablicyRejestracyjnej")?.ToSlownikZakresowy(),
            CzyWtornik = e.Val("czyWtornik"),
            StanOznaczenia = e.Desc("stanOznaczenia")?.ToStanOznaczenia()
        };

        private static ZmianaWlasnosciDto ToZmianaWlasnosci(this XElement e) => new()
        {
            SposobZmianyPrawWlasnosci = e.Desc("sposobZmianyPrawWlasnosci")?.ToSlownikRozszerzony(),
            DataOdnotowania = e.Val("dataOdnotowania")
        };

        private static PodmiotDto ToPodmiot(this XElement e) => new()
        {
            IdentyfikatorSystemowyPodmiotu = e.Val("identyfikatorSystemowyPodmiotu"),
            WariantPodmiotu = e.Val("wariantPodmiotu"),
            Firma = e.Desc("firma")?.ToFirma()
        };

        private static FirmaDto ToFirma(this XElement e) => new()
        {
            REGON = e.Val("REGON"),
            NazwaFirmy = e.Val("nazwaFirmy"),
            NazwaFirmyDrukowana = e.Val("nazwaFirmyDrukowana"),
            FormaWlasnosci = e.Desc("formaWlasnosci")?.ToSlownikZakresowy(),
            IdentyfikatorSystemowyREGON = e.Val("identyfikatorSystemowyREGON"),
            Adres = e.Desc("adres")?.ToAdres()
        };

        private static AdresDto ToAdres(this XElement e) => new()
        {
            Kraj = e.Desc("kraj")?.ToKraj(),
            KodTeryt = e.Val("kodTeryt"),
            KodTerytWojewodztwa = e.Val("kodTerytWojewodztwa"),
            NazwaWojewodztwaStanu = e.Val("nazwaWojewodztwaStanu"),
            KodTerytPowiatu = e.Val("kodTerytPowiatu"),
            NazwaPowiatuDzielnicy = e.Val("nazwaPowiatuDzielnicy"),
            KodTerytGminy = e.Val("kodTerytGminy"),
            NazwaGminy = e.Val("nazwaGminy"),
            KodRodzajuGminy = e.Val("kodRodzajuGminy"),
            KodPocztowy = e.Val("kodPocztowy"),
            KodTerytMiejscowosci = e.Val("kodTerytMiejscowosci"),
            NazwaMiejscowosci = e.Val("nazwaMiejscowosci"),
            NazwaMiejscowosciPodst = e.Val("nazwaMiejscowosciPodst"),
            KodTerytUlicy = e.Val("kodTerytUlicy"),
            UlicaCecha = e.Desc("ulicaCecha")?.ToSlownikZakresowy(),
            NazwaUlicy = e.Val("nazwaUlicy"),
            NumerDomu = e.Val("numerDomu")
        };

        private static KrajDto ToKraj(this XElement e) => new()
        {
            DataOd = e.Val("dataOd"),
            DataDo = e.Val("dataDo"),
            StatusRekordu = e.Val("statusRekordu"),
            KodNumeryczny = e.Val("kodNumeryczny"),
            KodIsoAlfa2 = e.Val("kodIsoAlfa2"),
            KodIsoAlfa3 = e.Val("kodIsoAlfa3"),
            KodMks = e.Val("kodMks"),
            CzyNalezyDoUE = e.Val("czyNalezyDoUE").ToBool(),
            Nazwa = e.Val("nazwa"),
            Obywatelstwo = e.Val("obywatelstwo"),
            DataAktualizacji = e.Val("dataAktualizacji")
        };

        private static HomologacjaRozszerzonaDto ToHomologacjaRozszerzona(this XElement e) => new()
        {
            IdentyfikatorSystemowyHomologacjiPojazdu = e.Val("identyfikatorSystemowyHomologacjiPojazdu"),
            IdentyfikatorPozycjiKatalogowej = e.Val("identyfikatorPozycjiKatalogowej"),
            WersjaPojazdu = e.Desc("wersjaPojazdu")?.ToHomologacjaPozycja(),
            WariantPojazdu = e.Desc("wariantPojazdu")?.ToHomologacjaPozycja(),
            TypPojazdu = e.Desc("typPojazdu")?.ToHomologacjaTyp(),
            NumerDokumentuHomologacji = e.Val("numerDokumentuHomologacji"),
            KodKategoriiHomologacji = e.Desc("kodKategoriiHomologacji")?.ToHomologacjaKategoria(),
            HomologacjaITS = e.Desc("homologacjaITS")?.ToHomologacjaITS(),
            TypWartoscOpisowa = e.Val("typWartoscOpisowa"),
            WariantWartoscOpisowa = e.Val("wariantWartoscOpisowa"),
            WersjaWartoscOpisowa = e.Val("wersjaWartoscOpisowa")
        };

        private static DaneTechnicznePojazduRozszerzoneDto ToDaneTechniczne(this XElement e) => new()
        {
            IdentyfikatorSystemowyDanychTechnicznych = e.Val("identyfikatorSystemowyDanychTechnicznych"),
            PojemnoscSilnika = e.Val("pojemnoscSilnika").ToInt(),
            MocSilnika = e.Val("mocSilnika").ToInt(),
            MasaWlasna = e.Val("masaWlasna").ToInt(),
            MasaCalkowita = e.Val("maksymalnaMasaCalkowita").ToInt(),
            DopuszczalnaMasaCalkowita = e.Val("dopuszczalnaMasaCalkowita").ToInt(),
            DopuszczalnaMasaCalkowitaZespoluPojazdow = e.Val("dopuszczalnaMasaCalkowitaZestawu").ToInt(),
            DopuszczalnaLadownoscCalkowita = e.Val("dopuszczalnaLadownosc").ToInt(),
            MaksymalnaMasaCalkowitaCiagnietejPrzyczepyZHamulcem = e.Val("dopuszczalnaMasaCalkowitaPrzyczepyZHam").ToInt(),
            MaksymalnaMasaCalkowitaCiagnietejPrzyczepyBezHamulca = e.Val("dopuszczalnaMasaCalkowitaPrzyczepyBezHam").ToInt(),
            LiczbaOsi = e.Val("liczbaOsi").ToInt(),
            LiczbaMiejscSiedzacych = e.Val("liczbaMiejscSiedzacych").ToInt(),
            MaksymalnyDopuszczalnyNaciskOsi = e.Val("masaNaOs").ToDecimal(),
            PoziomEmisjiSpalinEuroDlaGmin = e.Desc("poziomEmisjiSpalinEURODlaGmin")?.ToSlownikRozszerzony(),
            PaliwoPodstawowe = e.Desc("paliwoPodstawowe")?.ToPaliwoPodstawowe()
        };

        private static OznaczeniePojazduDto ToOznaczeniePojazduSimple(this XElement e) => new()
        {
            IdentyfikatorSystemowyOznaczenia = e.Val("identyfikatorSystemowyOznaczenia"),
            TypOznaczenia = e.Desc("typOznaczenia")?.ToSlownikRozszerzony(),
            NumerOznaczenia = e.Val("numerOznaczenia")
        };

        private static ZakladUbezpieczenDto ToZu(this XElement e) => new()
        {
            IdentyfikatorSystemowyZakladuUbezpieczen = e.Val("identyfikatorSystemowyZakladuUbezpieczen"),
            IdentyfikatorBiznesowyZakladuUbezpieczen = e.Val("identyfikatorBiznesowyZakladuUbezpieczen"),
            OdmowaUdostepnienia = e.Val("odmowaUdostepnienia").ToBool(),
            NazwaZakladuUbezpieczen = e.Val("nazwaZakladuUbezpieczen"),
            NazwaHandlowaZakladuUbezpieczeniowego = e.Val("nazwaHandlowaZakladuUbezpieczeniowego")
        };

        private static WariantUbezpieczeniaDto ToWariantUbezpieczenia(this XElement e) => new()
        {
            DataPoczatkuWariantu = e.Val("dataPoczatkuWariantu"),
            DataKoncaWariantu = e.Val("dataKoncaWariantu")
        };
    }
}
