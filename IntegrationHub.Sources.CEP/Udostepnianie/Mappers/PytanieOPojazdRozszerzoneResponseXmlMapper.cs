// IntegrationHub.Sources.CEP.Udostepnianie.Mappers/PytanieOPojazdRozszerzoneResponseXmlMapper.cs
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
            if (rezultat is null) return resp;

            // ===== daneRezultatu -> Meta
            var daneRez = rezultat.ElementAnyNs("daneRezultatu");
            if (daneRez != null)
            {
                resp.Meta = new MetaRozszerzone
                {
                    IdentyfikatorTransakcji = daneRez.ValueOf("identyfikatorTransakcji"),
                    IloscZwroconychRekordow = daneRez.ValueOf("iloscZwroconychRekordow").ToInt(),
                    ZnacznikCzasowy = daneRez.ValueOf("znacznikCzasowy"),
                    IdentyfikatorSystemuZewnetrznego = daneRez.ValueOf("identyfikatorSystemuZewnetrznego"),
                    ZnakSprawy = daneRez.ValueOf("znakSprawy"),
                    Wnioskodawca = daneRez.ValueOf("wnioskodawca")
                };
            }

            // ===== opcjonalne echo parametrow
            var echo = rezultat.ElementAnyNs("parametryZapytania");
            if (echo != null)
            {
                resp.ParametryZapytania = new ParametryZapytaniaRozszerzone
                {
                    NumerRejestracyjny = echo.Desc("parametryPojazdu")?.Desc("parametryDanychPojazdu")
                        ?.Desc("parametryOznaczeniaPojazdu")?.ValueOf("numerRejestracyjny"),
                    ZnakSprawy = echo.Desc("parametryPytania")?.Desc("wnioskodawca")?.ValueOf("znakSprawy"),
                    Wnioskodawca = echo.Desc("parametryPytania")?.Desc("wnioskodawca")?.ValueOf("wnioskodawca"),
                    IdentyfikatorSystemuZewnetrznego = echo.Desc("parametryPytania")?.ValueOf("identyfikatorSystemuZewnetrznego")
                };
            }

            // ===== pojazdRozszerzone
            var poj = rezultat.ElementAnyNs("pojazdRozszerzone");
            if (poj == null) return resp;

            var p = new PojazdRozszerzoneDto();

            // aktualnyIdentyfikatorPojazdu
            var aid = poj.Desc("aktualnyIdentyfikatorPojazdu");
            if (aid != null)
            {
                p.AktualnyIdentyfikatorPojazdu = new AktualnyIdPojazduDto
                {
                    IdentyfikatorSystemowyPojazdu = aid.ValueOf("identyfikatorSystemowyPojazdu"),
                    TokenAktualnosci = aid.ValueOf("tokenAktualnosci")
                };
            }

            // stanPojazdu (rozszerzony)
            var st = poj.Desc("stanPojazdu");
            if (st != null)
            {
                p.StanPojazdu = new StanPojazduRozszerzonyDto
                {
                    IdentyfikatorSystemowyStanuRejestracjiPojazdu = st.ValueOf("identyfikatorSystemowyStanuRejestracjiPojazdu"),
                    DataPoczatkuObowiazywaniaStanu = st.ValueOf("dataPoczatkuObowiazywaniaStanu"),
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
                    IdentyfikatorSystemowyDanychPojazdu = dop.ValueOf("identyfikatorSystemowyDanychPojazdu"),
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
                    NumerPodwoziaNadwoziaRamy = dop.ValueOf("numerPodwoziaNadwoziaRamy"),
                    RokProdukcji = dop.ValueOf("rokProdukcji").ToInt(),
                    RodzajKodowaniaRPP = dop.Desc("rodzajKodowaniaRPP")?.ToKodRpp()
                };
            }

            // homologacjaPojazdu
            var hom = poj.Desc("homologacjaPojazdu");
            if (hom != null)
            {
                p.HomologacjaPojazdu = new HomologacjaRozszerzonaDto
                {
                    IdentyfikatorSystemowyHomologacjiPojazdu = hom.ValueOf("identyfikatorSystemowyHomologacjiPojazdu"),
                    IdentyfikatorPozycjiKatalogowej = hom.ValueOf("identyfikatorPozycjiKatalogowej"),
                    WersjaPojazdu = hom.Desc("wersjaPojazdu")?.ToHomologacjaPozycja(),
                    WariantPojazdu = hom.Desc("wariantPojazdu")?.ToHomologacjaPozycja(),
                    TypPojazdu = hom.Desc("typPojazdu")?.ToHomologacjaTyp(),
                    NumerDokumentuHomologacji = hom.ValueOf("numerDokumentuHomologacji"),
                    KodKategoriiHomologacji = hom.Desc("kodKategoriiHomologacji")?.ToHomologacjaKategoria(),
                    HomologacjaITS = hom.Desc("homologacjaITS")?.ToHomologacjaITS(),
                    TypWartoscOpisowa = hom.ValueOf("typWartoscOpisowa"),
                    WariantWartoscOpisowa = hom.ValueOf("wariantWartoscOpisowa"),
                    WersjaWartoscOpisowa = hom.ValueOf("wersjaWartoscowa") ?? hom.ValueOf("wersjaWartoscOpisowa")
                };
            }

            // danePierwszejRejestracji
            var pr = poj.Desc("danePierwszejRejestracji");
            if (pr != null)
            {
                p.DanePierwszejRejestracji = new PierwszaRejestracjaRozszerzonaDto
                {
                    IdentyfikatorSystemowyPierwszejRejestracjiPojazdu = pr.ValueOf("identyfikatorSystemowyPierwszejRejestracjiPojazdu"),
                    DataPierwszejRejestracjiWKraju = pr.ValueOf("dataPierwszejRejestracjiWKraju"),
                    DataPierwszejRejestracjiZaGranica = pr.ValueOf("dataPierwszejRejestracjiZaGranica"),
                    DataPierwszejRejestracji = pr.ValueOf("dataPierwszejRejestracji")
                };
            }

            // informacjeSKP
            var skp = poj.Desc("informacjeSKP");
            if (skp != null)
            {
                p.InformacjeSkp = new InformacjeSkpDto
                {
                    IdentyfikatorSystemowyInformacjiSkp = skp.ValueOf("identyfikatorSystemowyInformacjiSKP"),
                    IdentyfikatorCzynnosci = skp.ValueOf("identyfikatorCzynnosci"),
                    StacjaKontroliPojazdow = skp.Desc("stacjaKontroliPojazdow")?.ToSkpPodmiot(),
                    RodzajCzynnosciSkp = skp.Desc("rodzajCzynnosciSKP")?.ToSlownikZakresowy(),
                    NumerZaswiadczenia = skp.ValueOf("numerZaswiadczenia"),
                    WynikCzynnosci = skp.Desc("wynikCzynnosci")?.ToSlownikRozszerzony(),
                    WpisDoDokumentuPojazdu = skp.ValueOf("wpisDoDokumentuPojazdu").ToBool(),
                    WydanieZaswiadczenia = skp.ValueOf("wydanieZaswiadczenia").ToBool(),
                    DataGodzWykonaniaCzynnosciSkp = skp.ValueOf("dataGodzWykonaniaCzynnosciSKP"),
                    TrybAwaryjny = skp.ValueOf("trybAwaryjny").ToBool(),
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
                    IdentyfikatorSystemowyDanychTechnicznych = dt.ValueOf("identyfikatorSystemowyDanychTechnicznych"),
                    PojemnoscSilnika = dt.ValueOf("pojemnoscSilnika").ToInt(),
                    MocSilnika = dt.ValueOf("mocSilnika").ToInt(),
                    MasaWlasna = dt.ValueOf("masaWlasna").ToInt(),
                    MasaCalkowita = dt.ValueOf("maksymalnaMasaCalkowita").ToInt(),
                    DopuszczalnaMasaCalkowita = dt.ValueOf("dopuszczalnaMasaCalkowita").ToInt(),
                    DopuszczalnaMasaCalkowitaZespoluPojazdow = dt.ValueOf("dopuszczalnaMasaCalkowitaZestawu").ToInt(),
                    DopuszczalnaLadownoscCalkowita = dt.ValueOf("dopuszczalnaLadownosc").ToInt(),
                    MaksymalnaMasaCalkowitaCiagnietejPrzyczepyZHamulcem = dt.ValueOf("dopuszczalnaMasaCalkowitaPrzyczepyZHam").ToInt(),
                    MaksymalnaMasaCalkowitaCiagnietejPrzyczepyBezHamulca = dt.ValueOf("dopuszczalnaMasaCalkowitaPrzyczepyBezHam").ToInt(),
                    LiczbaOsi = dt.ValueOf("liczbaOsi").ToInt(),
                    LiczbaMiejscSiedzacych = dt.ValueOf("liczbaMiejscSiedzacych").ToInt(),
                    MaksymalnyDopuszczalnyNaciskOsi = dt.ValueOf("masaNaOs").ToDecimal(),
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
                    IdentyfikatorSystemowyPojazduSprowadzonego = spr.ValueOf("identyfikatorSystemowyPojazduSprowadzonego"),
                    NumerRejestracyjnyZagraniczny = spr.ValueOf("numerRejestracyjnyZagraniczny"),
                    KrajZagranicznejRejestracji = spr.Desc("krajZagranicznejRejestracji")?.ToKraj(),
                    PoprzedniVinZagranicznejRejestracji = spr.ValueOf("poprzedniVINZagranicznejRejestracji")
                };
            }

            // rejestracjaPojazdu (wiele)
            foreach (var re in poj.DescMany("rejestracjaPojazdu"))
            {
                p.RejestracjePojazdu.Add(new RejestracjaPojazduRozszerzonaDto
                {
                    IdentyfikatorSystemowyRejestracji = re.ValueOf("identyfikatorSystemowyRejestracji"),
                    OrganRejestrujacy = re.Desc("organRejestrujacy")?.ToOrgan(),
                    DataRejestracjiPojazdu = re.ValueOf("dataRejestracjiPojazdu"),
                    TypRejestracji = re.Desc("typrejestracji")?.ToSlownikZakresowy()
                });
            }

            // dokumentPojazdu (wiele)
            foreach (var d in poj.DescMany("dokumentPojazdu"))
            {
                var dokDto = new DokumentPojazduRozszerzonyDto
                {
                    IdentyfikatorSystemowyDokumentuPojazdu = d.ValueOf("identyfikatorSystemowyDokumentuPojazdu"),
                    TypDokumentu = d.Desc("typDokumentu")?.ToSlownikRozszerzony(),
                    DokumentSeriaNumer = d.ValueOf("dokumentSeriaNumer"),
                    CzyWtornik = d.ValueOf("czyWtornik"),
                    DataWydaniaDokumentu = d.ValueOf("dataWydaniaDokumentu"),
                    OrganWydajacyDokument = d.Desc("organWydajacyDokument")?.ToOrgan(),
                    DanePierwszejRejestracji = d.Desc("danePierwszejRejestracji")?.ToPierwszaRejestracja(),
                    DaneOpisujacePojazd = d.Desc("daneOpisujacePojazd")?.ToDaneOpisujace(),
                    HomologacjaPojazdu = d.Desc("homologacjaPojazdu")?.ToHomologacjaRozszerzona(),
                    DaneTechnicznePojazdu = d.Desc("daneTechnicznePojazdu")?.ToDaneTechniczne(),
                    StanDokumentu = d.Desc("stanDokumentu")?.ToStanOznaczenia(),
                    CzyAktualny = d.ValueOf("czyAktualny")
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
                    IdentyfikatorSystemowyWlasnosci = nvp.ValueOf("identyfikatorSystemowyWlasnosci"),
                    KodWlasnosci = nvp.Desc("kodWlasnosci")?.ToSlownikZakresowy(),
                    DataZmianyPrawWlasnosci = nvp.ValueOf("dataZmianyPrawWlasnosci"),
                    DataOdnotowania = nvp.ValueOf("dataOdnotowania"),
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
                    IdentyfikatorPolisy = oc.ValueOf("identyfikatorPolisy"),
                    NumerPolisy = oc.ValueOf("numerPolisy"),
                    DataZawarciaPolisy = oc.ValueOf("dataZawarciaPolisy"),
                    DataPoczatkuObowiazywaniaPolisy = oc.ValueOf("dataPoczatkuObowiazywaniaPolisy"),
                    DataKoncaObowiazywaniaPolisy = oc.ValueOf("dataKoncaObowiazywaniaPolisy"),
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
                    IdentyfikatorSystemowyOznaczenia = anr.ValueOf("identyfikatorSystemowyOznaczenia"),
                    TypOznaczenia = anr.Desc("typOznaczenia")?.ToSlownikRozszerzony(),
                    NumerOznaczenia = anr.ValueOf("numerOznaczenia")
                };
            }

            // aktualnyStanLicznika
            var asl = poj.Desc("aktualnyStanLicznika");
            if (asl != null)
            {
                p.AktualnyStanLicznika = new AktualnyStanLicznikaDto
                {
                    HistoriaLicznikaWymagaWeryfikacji = asl.ValueOf("historiaLicznikaWymagaWeryfikacji").ToBool(),
                    LicznikWymieniony = asl.ValueOf("licznikWymieniony").ToBool(),
                    StanLicznika = asl.Desc("stanLicznika")?.ToStanLicznika()
                };
            }

            resp.Pojazd = p;
            return resp;
        }

        // ===== typed mappers (specyficzne dla „Rozszerzone”) =====

        private static SlownikZakresowyDto ToSlownikZakresowy(this XElement e) => new()
        {
            DataOd = e.ValueOf("dataOd"),
            DataDo = e.ValueOf("dataDo"),
            Status = e.ValueOf("status"),
            Kod = e.ValueOf("kod"),
            WartoscOpisowaSkrocona = e.ValueOf("wartoscOpisowaSkrocona"),
            WartoscOpisowa = e.ValueOf("wartoscOpisowa")
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
                Zrodlo = e.ValueOf("zrodlo"),
                Oznaczenie = e.ValueOf("oznaczenie")
            };
        }

        private static SkpPodmiotDto ToSkpPodmiot(this XElement e) => new()
        {
            DataOd = e.ValueOf("dataOd"),
            DataDo = e.ValueOf("dataDo"),
            StatusRekordu = e.ValueOf("statusRekordu"),
            Kod = e.ValueOf("kod"),
            Nazwa = e.ValueOf("nazwa"),
            NumerEwidencyjny = e.ValueOf("numerEwidencyjny"),
            IdentyfikatorREGON = e.ValueOf("identyfikatorREGON"),
            REGON = e.ValueOf("REGON"),
            Typ = e.Desc("typ")?.ToSlownikZakresowy()
        };

        private static TerminKolejnegoBadaniaDto ToTerminKolejnegoBadania(this XElement e) => new()
        {
            DataKolejnegoBadania = e.ValueOf("dataKolejnegoBadania")
        };

        private static StanLicznikaDto ToStanLicznika(this XElement e) => new()
        {
            IdentyfikatorSystemowyStanuLicznika = e.ValueOf("identyfikatorSystemowyStanuLicznika"),
            WartoscStanuLicznika = e.ValueOf("wartoscStanuLicznika").ToInt(),
            JednostkaStanuLicznika = e.Desc("jednostkaMiary")?.ToSlownikZakresowy(),
            DataSpisaniaLicznika = e.ValueOf("dataSpisaniaLicznika"),
            PodmiotWprowadzajacy = e.Desc("podmiotWprowadzajacy")?.ToSkpPodmiot(),
            DataOdnotowania = e.ValueOf("dataOdnotowania")
        };

        private static PaliwoPodstawoweDto ToPaliwoPodstawowe(this XElement e) => new()
        {
            RodzajPaliwa = e.Desc("rodzajPaliwa")?.ToRodzajPaliwa()
        };

        private static RodzajPaliwaDto ToRodzajPaliwa(this XElement e) => new()
        {
            KodPaliwa = e.ValueOf("kodPaliwa"),
            WartoscOpisowa = e.ValueOf("wartoscOpisowa"),
            Kod = e.ValueOf("kod"),
            DataOd = e.ValueOf("dataOd"),
            DataDo = e.ValueOf("dataDo"),
            Status = e.ValueOf("status"),
            Zrodlo = e.ValueOf("zrodlo")
        };

        private static HomologacjaPozycjaDto ToHomologacjaPozycja(this XElement e) => new()
        {
            DataOd = e.ValueOf("dataOd"),
            DataDo = e.ValueOf("dataDo"),
            StatusRekordu = e.ValueOf("statusRekordu"),
            Kod = e.ValueOf("kod"),
            KodWersji = e.ValueOf("kodWersji"),
            WersjaHomologacji = e.ValueOf("wersjaHomologacji"),
            KodWariantu = e.ValueOf("kodWariantu"),
            NazwaWariantu = e.ValueOf("nazwaWariantu"),
            Zrodlo = e.ValueOf("zrodlo")
        };

        private static HomologacjaTypDto ToHomologacjaTyp(this XElement e) => new()
        {
            DataOd = e.ValueOf("dataOd"),
            DataDo = e.ValueOf("dataDo"),
            StatusRekordu = e.ValueOf("statusRekordu"),
            Kod = e.ValueOf("kod"),
            WartoscOpisowa = e.ValueOf("wartoscOpisowa"),
            Zrodlo = e.ValueOf("zrodlo")
        };

        private static HomologacjaKategoriaDto ToHomologacjaKategoria(this XElement e) => new()
        {
            DataOd = e.ValueOf("dataOd"),
            DataDo = e.ValueOf("dataDo"),
            StatusRekordu = e.ValueOf("statusRekordu"),
            KodKatHom = e.ValueOf("kodKatHom"),
            WartoscOpisowa = e.ValueOf("wartoscOpisowa"),
            Kod = e.ValueOf("kod")
        };

        private static HomologacjaITSDto ToHomologacjaITS(this XElement e) => new()
        {
            DataOd = e.ValueOf("dataOd"),
            DataDo = e.ValueOf("dataDo"),
            StatusRekordu = e.ValueOf("statusRekordu"),
            IdentyfikatorPozycjiKatalogowej = e.ValueOf("identyfikatorPozycjiKatalogowej"),
            NumerSwiadectwa = e.ValueOf("numerSwiadectwa")
        };

        private static MarkaModelDto ToMarkaModel(this XElement e) => new()
        {
            DataOd = e.ValueOf("dataOd"),
            DataDo = e.ValueOf("dataDo"),
            StatusRekordu = e.ValueOf("statusRekordu"),
            KodMarki = e.ValueOf("kodMarki"),
            WartoscOpisowa = e.ValueOf("wartoscOpisowa"),
            Kod = e.ValueOf("kod"),
            PozycjeSzczegolowe = e.ValueOf("pozycjeSzczegolowe")
        };

        private static RodzajPodrodzajDto ToRodzajPodrodzaj(this XElement e) => new()
        {
            DataOd = e.ValueOf("dataOd"),
            DataDo = e.ValueOf("dataDo"),
            StatusRekordu = e.ValueOf("statusRekordu"),
            KodRodzaj = e.ValueOf("kodRodzaj"),
            Rodzaj = e.ValueOf("rodzaj"),
            KodPodrodzaj = e.ValueOf("kodPodrodzaj"),
            Podrodzaj = e.ValueOf("podrodzaj"),
            Wersja = e.ValueOf("wersja"),
            Zrodlo = e.ValueOf("zrodlo")
        };

        private static KodRppDto ToKodRpp(this XElement e) => new()
        {
            DataOd = e.ValueOf("dataOd"),
            DataDo = e.ValueOf("dataDo"),
            StatusRekordu = e.ValueOf("statusRekordu"),
            KodRPP = e.ValueOf("kodRPP"),
            Rodzaj = e.ValueOf("rodzaj"),
            Podrodzaj = e.ValueOf("podrodzaj"),
            Przeznaczenie = e.ValueOf("przeznaczenie"),
            Wersja = e.ValueOf("wersja"),
            Kod = e.ValueOf("kod"),
            Zrodlo = e.ValueOf("zrodlo")
        };

        private static PierwszaRejestracjaRozszerzonaDto ToPierwszaRejestracja(this XElement e) => new()
        {
            IdentyfikatorSystemowyPierwszejRejestracjiPojazdu = e.ValueOf("identyfikatorSystemowyPierwszejRejestracjiPojazdu"),
            DataPierwszejRejestracjiWKraju = e.ValueOf("dataPierwszejRejestracjiWKraju"),
            DataPierwszejRejestracjiZaGranica = e.ValueOf("dataPierwszejRejestracjiZaGranica"),
            DataPierwszejRejestracji = e.ValueOf("dataPierwszejRejestracji")
        };

        private static DaneOpisujacePojazdRozszerzoneDto ToDaneOpisujace(this XElement e) => new()
        {
            IdentyfikatorSystemowyDanychPojazdu = e.ValueOf("identyfikatorSystemowyDanychPojazdu"),
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
            NumerPodwoziaNadwoziaRamy = e.ValueOf("numerPodwoziaNadwoziaRamy"),
            RokProdukcji = e.ValueOf("rokProdukcji").ToInt(),
            RodzajKodowaniaRPP = e.Desc("rodzajKodowaniaRPP")?.ToKodRpp()
        };

        private static OrganRozszerzonyDto ToOrgan(this XElement e) => new()
        {
            DataOd = e.ValueOf("dataOd"),
            DataDo = e.ValueOf("dataDo"),
            StatusRekordu = e.ValueOf("statusRekordu"),
            Kod = e.ValueOf("kod"),
            Nazwa = e.ValueOf("nazwa"),
            NumerEwidencyjny = e.ValueOf("numerEwidencyjny"),
            IdentyfikatorREGON = e.ValueOf("identyfikatorREGON"),
            REGON = e.ValueOf("REGON"),
            NazwaOrganuWydajacego = e.ValueOf("nazwaOrganuWydajacego"),
            Typ = e.Desc("typ")?.ToSlownikZakresowy()
        };

        private static StanOznaczeniaRozszerzoneDto ToStanOznaczenia(this XElement e) => new()
        {
            IdentyfikatorSystemowyStanuOznaczeniaPojazdu = e.ValueOf("identyfikatorSystemowyStanuOznaczeniaPojazdu"),
            DataPoczatkuObowiazywania = e.ValueOf("dataPoczatkuObowiazywania"),
            Stan = e.Desc("stanOznaczenia")?.ToSlownikZakresowy(),
            OrganUstanawiajacyStan = e.Desc("organUstanawiajacyStan")?.ToOrgan(),
            DataOdnotowaniaStanu = e.ValueOf("dataOdnotowaniaStanu")
        };

        private static OznaczeniePojazduRozszerzoneDto ToOznaczeniePojazdu(this XElement e) => new()
        {
            IdentyfikatorSystemowyOznaczenia = e.ValueOf("identyfikatorSystemowyOznaczenia"),
            TypOznaczenia = e.Desc("typOznaczenia")?.ToSlownikRozszerzony(),
            NumerOznaczenia = e.ValueOf("numerOznaczenia"),
            RodzajTablicyRejestracyjnej = e.Desc("rodzajTablicyRejestracyjnej")?.ToSlownikZakresowy(),
            WzorTablicyRejestracyjnej = e.Desc("wzorTablicyRejestracyjnej")?.ToSlownikZakresowy(),
            KolorTablicyRejestracyjnej = e.Desc("kolorTablicyRejestracyjnej")?.ToSlownikZakresowy(),
            CzyWtornik = e.ValueOf("czyWtornik"),
            StanOznaczenia = e.Desc("stanOznaczenia")?.ToStanOznaczenia()
        };

        private static ZmianaWlasnosciDto ToZmianaWlasnosci(this XElement e) => new()
        {
            SposobZmianyPrawWlasnosci = e.Desc("sposobZmianyPrawWlasnosci")?.ToSlownikRozszerzony(),
            DataOdnotowania = e.ValueOf("dataOdnotowania")
        };

        private static PodmiotDto ToPodmiot(this XElement e) => new()
        {
            IdentyfikatorSystemowyPodmiotu = e.ValueOf("identyfikatorSystemowyPodmiotu"),
            WariantPodmiotu = e.ValueOf("wariantPodmiotu"),
            Firma = e.Desc("firma")?.ToFirma()
        };

        private static FirmaDto ToFirma(this XElement e) => new()
        {
            REGON = e.ValueOf("REGON"),
            NazwaFirmy = e.ValueOf("nazwaFirmy"),
            NazwaFirmyDrukowana = e.ValueOf("nazwaFirmyDrukowana"),
            FormaWlasnosci = e.Desc("formaWlasnosci")?.ToSlownikZakresowy(),
            IdentyfikatorSystemowyREGON = e.ValueOf("identyfikatorSystemowyREGON"),
            Adres = e.Desc("adres")?.ToAdres()
        };

        private static AdresDto ToAdres(this XElement e) => new()
        {
            Kraj = e.Desc("kraj")?.ToKraj(),
            KodTeryt = e.ValueOf("kodTeryt"),
            KodTerytWojewodztwa = e.ValueOf("kodTerytWojewodztwa"),
            NazwaWojewodztwaStanu = e.ValueOf("nazwaWojewodztwaStanu"),
            KodTerytPowiatu = e.ValueOf("kodTerytPowiatu"),
            NazwaPowiatuDzielnicy = e.ValueOf("nazwaPowiatuDzielnicy"),
            KodTerytGminy = e.ValueOf("kodTerytGminy"),
            NazwaGminy = e.ValueOf("nazwaGminy"),
            KodRodzajuGminy = e.ValueOf("kodRodzajuGminy"),
            KodPocztowy = e.ValueOf("kodPocztowy"),
            KodTerytMiejscowosci = e.ValueOf("kodTerytMiejscowosci"),
            NazwaMiejscowosci = e.ValueOf("nazwaMiejscowosci"),
            NazwaMiejscowosciPodst = e.ValueOf("nazwaMiejscowosciPodst"),
            KodTerytUlicy = e.ValueOf("kodTerytUlicy"),
            UlicaCecha = e.Desc("ulicaCecha")?.ToSlownikZakresowy(),
            NazwaUlicy = e.ValueOf("nazwaUlicy"),
            NumerDomu = e.ValueOf("numerDomu")
        };

        private static KrajDto ToKraj(this XElement e) => new()
        {
            DataOd = e.ValueOf("dataOd"),
            DataDo = e.ValueOf("dataDo"),
            StatusRekordu = e.ValueOf("statusRekordu"),
            KodNumeryczny = e.ValueOf("kodNumeryczny"),
            KodIsoAlfa2 = e.ValueOf("kodIsoAlfa2"),
            KodIsoAlfa3 = e.ValueOf("kodIsoAlfa3"),
            KodMks = e.ValueOf("kodMks"),
            CzyNalezyDoUE = e.ValueOf("czyNalezyDoUE").ToBool(),
            Nazwa = e.ValueOf("nazwa"),
            Obywatelstwo = e.ValueOf("obywatelstwo"),
            DataAktualizacji = e.ValueOf("dataAktualizacji")
        };

        private static HomologacjaRozszerzonaDto ToHomologacjaRozszerzona(this XElement e) => new()
        {
            IdentyfikatorSystemowyHomologacjiPojazdu = e.ValueOf("identyfikatorSystemowyHomologacjiPojazdu"),
            IdentyfikatorPozycjiKatalogowej = e.ValueOf("identyfikatorPozycjiKatalogowej"),
            WersjaPojazdu = e.Desc("wersjaPojazdu")?.ToHomologacjaPozycja(),
            WariantPojazdu = e.Desc("wariantPojazdu")?.ToHomologacjaPozycja(),
            TypPojazdu = e.Desc("typPojazdu")?.ToHomologacjaTyp(),
            NumerDokumentuHomologacji = e.ValueOf("numerDokumentuHomologacji"),
            KodKategoriiHomologacji = e.Desc("kodKategoriiHomologacji")?.ToHomologacjaKategoria(),
            HomologacjaITS = e.Desc("homologacjaITS")?.ToHomologacjaITS(),
            TypWartoscOpisowa = e.ValueOf("typWartoscOpisowa"),
            WariantWartoscOpisowa = e.ValueOf("wariantWartoscOpisowa"),
            WersjaWartoscOpisowa = e.ValueOf("wersjaWartoscOpisowa")
        };

        private static DaneTechnicznePojazduRozszerzoneDto ToDaneTechniczne(this XElement e) => new()
        {
            IdentyfikatorSystemowyDanychTechnicznych = e.ValueOf("identyfikatorSystemowyDanychTechnicznych"),
            PojemnoscSilnika = e.ValueOf("pojemnoscSilnika").ToInt(),
            MocSilnika = e.ValueOf("mocSilnika").ToInt(),
            MasaWlasna = e.ValueOf("masaWlasna").ToInt(),
            MasaCalkowita = e.ValueOf("maksymalnaMasaCalkowita").ToInt(),
            DopuszczalnaMasaCalkowita = e.ValueOf("dopuszczalnaMasaCalkowita").ToInt(),
            DopuszczalnaMasaCalkowitaZespoluPojazdow = e.ValueOf("dopuszczalnaMasaCalkowitaZestawu").ToInt(),
            DopuszczalnaLadownoscCalkowita = e.ValueOf("dopuszczalnaLadownosc").ToInt(),
            MaksymalnaMasaCalkowitaCiagnietejPrzyczepyZHamulcem = e.ValueOf("dopuszczalnaMasaCalkowitaPrzyczepyZHam").ToInt(),
            MaksymalnaMasaCalkowitaCiagnietejPrzyczepyBezHamulca = e.ValueOf("dopuszczalnaMasaCalkowitaPrzyczepyBezHam").ToInt(),
            LiczbaOsi = e.ValueOf("liczbaOsi").ToInt(),
            LiczbaMiejscSiedzacych = e.ValueOf("liczbaMiejscSiedzacych").ToInt(),
            MaksymalnyDopuszczalnyNaciskOsi = e.ValueOf("masaNaOs").ToDecimal(),
            PoziomEmisjiSpalinEuroDlaGmin = e.Desc("poziomEmisjiSpalinEURODlaGmin")?.ToSlownikRozszerzony(),
            PaliwoPodstawowe = e.Desc("paliwoPodstawowe")?.ToPaliwoPodstawowe()
        };

        private static OznaczeniePojazduRozszerzoneDto ToOznaczeniePojazduSimple(this XElement e) => new()
        {
            IdentyfikatorSystemowyOznaczenia = e.ValueOf("identyfikatorSystemowyOznaczenia"),
            TypOznaczenia = e.Desc("typOznaczenia")?.ToSlownikRozszerzony(),
            NumerOznaczenia = e.ValueOf("numerOznaczenia")
        };

        private static ZakladUbezpieczenDto ToZu(this XElement e) => new()
        {
            IdentyfikatorSystemowyZakladuUbezpieczen = e.ValueOf("identyfikatorSystemowyZakladuUbezpieczen"),
            IdentyfikatorBiznesowyZakladuUbezpieczen = e.ValueOf("identyfikatorBiznesowyZakladuUbezpieczen"),
            OdmowaUdostepnienia = e.ValueOf("odmowaUdostepnienia").ToBool(),
            NazwaZakladuUbezpieczen = e.ValueOf("nazwaZakladuUbezpieczen"),
            NazwaHandlowaZakladuUbezpieczeniowego = e.ValueOf("nazwaHandlowaZakladuUbezpieczeniowego")
        };

        private static WariantUbezpieczeniaDto ToWariantUbezpieczenia(this XElement e) => new()
        {
            DataPoczatkuWariantu = e.ValueOf("dataPoczatkuWariantu"),
            DataKoncaWariantu = e.ValueOf("dataKoncaWariantu")
        };
    }
}
