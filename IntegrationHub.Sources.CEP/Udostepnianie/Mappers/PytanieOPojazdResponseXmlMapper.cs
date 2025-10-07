// IntegrationHub.Sources.CEP.Udostepnianie.Mappers/PytanieOPojazdResponseXmlMapper.cs
using System.Xml.Linq;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Mappers
{
    public static class PytanieOPojazdResponseXmlMapper
    {
        private static readonly XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
        private static readonly XNamespace elem = "http://elementy.cep.udo.api.cepik.coi.gov.pl";

        public static PytanieOPojazdResponse Parse(string xml)
        {
            var doc = XDocument.Parse(xml);
            var body = doc.Root?.Element(soap + "Body");
            var rezultat = body?.Element(elem + "pytanieOPojazdRezultat");

            var resp = new PytanieOPojazdResponse();

            // daneRezultatu -> Meta
            var daneRez = rezultat?.ElementAnyNs("daneRezultatu");
            if (daneRez is not null)
            {
                resp.Meta = new PytanieMeta
                {
                    IdentyfikatorTransakcji = daneRez.ValueOf("identyfikatorTransakcji"),
                    IloscZwroconychRekordow = daneRez.ValueOf("iloscZwroconychRekordow")?.ToInt(),
                    ZnacznikCzasowy = daneRez.ValueOf("znacznikCzasowy"),
                    IdentyfikatorSystemuZewnetrznego = daneRez.ValueOf("identyfikatorSystemuZewnetrznego"),
                    ZnakSprawy = daneRez.ValueOf("znakSprawy"),
                    Wnioskodawca = daneRez.ValueOf("wnioskodawca")
                };
            }

            // pojazd
            var poj = rezultat?.ElementAnyNs("pojazd");
            if (poj is null) return resp;

            var p = new PojazdDto();

            // aktualnyIdentyfikatorPojazdu
            var aid = poj.ElementAnyNs("aktualnyIdentyfikatorPojazdu");
            if (aid is not null)
            {
                p.AktualnyIdPojazdu = new AktualnyIdPojazduDto
                {
                    IdentyfikatorSystemowyPojazdu = aid.ValueOf("identyfikatorSystemowyPojazdu"),
                    TokenAktualnosci = aid.ValueOf("tokenAktualnosci")
                };
            }

            // stanPojazdu
            var st = poj.ElementAnyNs("stanPojazdu");
            if (st is not null)
            {
                var stan = st.ElementAnyNs("stanPojazdu");
                p.StanPojazdu = new StanPojazduDto
                {
                    IdStanuRejestracji = st.ValueOf("identyfikatorSystemowyStanuRejestracjiPojazdu"),
                    DataPoczatkuObowiazywania = st.ValueOf("dataPoczatkuObowiazywaniaStanu"),
                    Kod = stan?.ValueOf("kod"),
                    OpisSkrocony = stan?.ValueOf("wartoscOpisowaSkrocona"),
                    Opis = stan?.ValueOf("wartoscOpisowa"),
                    Status = stan?.ValueOf("status")
                };
            }

            // daneOpisujacePojazd
            var dop = poj.ElementAnyNs("daneOpisujacePojazd");
            if (dop is not null)
            {
                p.DaneOpisujace = new DaneOpisujacePojazdDto
                {
                    Marka = dop.ElementAnyNs("marka")?.ValueOf("wartoscOpisowa"),
                    KodMarki = dop.ElementAnyNs("marka")?.ValueOf("kod"),
                    Model = dop.ElementAnyNs("model")?.ValueOf("wartoscOpisowa"),
                    KodModelu = dop.ElementAnyNs("model")?.ValueOf("kod"),
                    Rodzaj = dop.ElementAnyNs("rodzaj")?.ValueOf("rodzaj"),
                    Podrodzaj = dop.ElementAnyNs("podrodzaj")?.ValueOf("podrodzaj"),
                    NumerVin = dop.ValueOf("numerPodwoziaNadwoziaRamy"),
                    RokProdukcji = dop.ValueOf("rokProdukcji")?.ToInt(),
                    KodRpp = dop.ElementAnyNs("kodRPP")?.ValueOf("kodRPP")
                };
            }

            // homologacjaPojazdu
            var hom = poj.ElementAnyNs("homologacjaPojazdu");
            if (hom is not null)
            {
                p.Homologacja = new HomologacjaBasicDto
                {
                    IdHomologacji = hom.ValueOf("identyfikatorSystemowyHomologacjiPojazdu"),
                    WersjaKod = hom.ElementAnyNs("wersjaPojazdu")?.ValueOf("kod"),
                    WariantKod = hom.ElementAnyNs("wariantPojazdu")?.ValueOf("kod"),
                    TypKod = hom.ElementAnyNs("typPojazdu")?.ValueOf("kod")
                };
            }

            // danePierwszejRejestracji
            var pr = poj.ElementAnyNs("danePierwszejRejestracji");
            if (pr is not null)
            {
                p.PierwszaRejestracja = new PierwszaRejestracjaDto
                {
                    WKraju = pr.ValueOf("dataPierwszejRejestracjiWKraju"),
                    ZaGranica = pr.ValueOf("dataPierwszejRejestracjiZaGranica"),
                    DataPierwszej = pr.ValueOf("dataPierwszejRejestracji")
                };
            }

            // badanieTechniczne
            var bt = poj.ElementAnyNs("badanieTechniczne");
            if (bt is not null)
            {
                p.BadanieTechniczne = new BadanieTechniczneDto
                {
                    IdBadania = bt.ValueOf("identyfikatorSystemowyBadaniaTechnicznego"),
                    TerminKolejnegoBadania = bt.ElementAnyNs("terminKolejnegoBadaniaTechnicznego")?.ValueOf("dataKolejnegoBadania")
                };
            }

            // daneTechnicznePojazdu
            var dt = poj.ElementAnyNs("daneTechnicznePojazdu");
            if (dt is not null)
            {
                p.DaneTechniczne = new DaneTechniczneDto
                {
                    PojemnoscSilnika = dt.ValueOf("pojemnoscSilnika")?.ToDecimal(),
                    MocSilnika = dt.ValueOf("mocSilnika")?.ToDecimal(),
                    MasaWlasna = dt.ValueOf("masaWlasna")?.ToInt()
                };
            }

            // danePojazduSprowadzonego
            var spr = poj.ElementAnyNs("danePojazduSprowadzonego");
            if (spr is not null)
            {
                p.Sprowadzony = new PojazdSprowadzonyDto
                {
                    Id = spr.ValueOf("identyfikatorSystemowyPojazduSprowadzonego"),
                    NumerRejZagraniczny = spr.ValueOf("numerRejestracyjnyZagraniczny"),
                    KrajKodAlfa2 = spr.ElementAnyNs("krajZagranicznejRejestracji")?.ValueOf("kodIsoAlfa2"),
                    KrajNazwa = spr.ElementAnyNs("krajZagranicznejRejestracji")?.ValueOf("nazwa"),
                    PoprzedniVin = spr.ValueOf("poprzedniVINZagranicznejRejestracji")
                };
            }

            // rejestracjaPojazdu (wiele)
            foreach (var r in poj.ElementsAnyNs("rejestracjaPojazdu"))
            {
                p.Rejestracje.Add(new RejestracjaDto
                {
                    IdRejestracji = r.ValueOf("identyfikatorSystemowyRejestracji"),
                    OrganKod = r.ElementAnyNs("organRejestrujacy")?.ValueOf("kod"),
                    OrganNazwa = r.ElementAnyNs("organRejestrujacy")?.ValueOf("nazwa"),
                    DataRejestracji = r.ValueOf("dataRejestracjiPojazdu"),
                    TypKod = r.ElementAnyNs("typrejestracji")?.ValueOf("kod"),
                    TypOpisSkrocony = r.ElementAnyNs("typrejestracji")?.ValueOf("wartoscOpisowaSkrocona")
                });
            }

            // dokumentPojazdu (wiele)
            foreach (var d in poj.ElementsAnyNs("dokumentPojazdu"))
            {
                p.Dokumenty.Add(new DokumentPojazduDto
                {
                    IdDokumentu = d.ValueOf("identyfikatorSystemowyDokumentuPojazdu"),
                    TypKod = d.ElementAnyNs("typDokumentu")?.ValueOf("kod"),
                    TypOpisSkrocony = d.ElementAnyNs("typDokumentu")?.ValueOf("wartoscOpisowaSkrocona"),
                    SeriaNumer = d.ValueOf("dokumentSeriaNumer"),
                    DataWydania = d.ValueOf("dataWydaniaDokumentu"),
                    CzyWtornik = d.ValueOf("czyWtornik"),
                    Aktualny = d.ValueOf("czyAktualny"),
                    NumerTablicy = d.ElementAnyNs("oznaczeniePojazdu")?.ValueOf("numerOznaczenia"),
                    RodzajTablicyKod = d.ElementAnyNs("rodzajTablicyRejestracyjnej")?.ValueOf("kod")
                });
            }

            // oc
            var oc = poj.ElementAnyNs("danePolisyOC");
            if (oc is not null)
            {
                p.PolisaOc = new PolisaOcDto
                {
                    IdPolisy = oc.ValueOf("identyfikatorPolisy"),
                    NumerPolisy = oc.ValueOf("numerPolisy"),
                    DataOd = oc.ValueOf("dataPoczatkuObowiazywaniaPolisy"),
                    DataDo = oc.ValueOf("dataKoncaObowiazywaniaPolisy")
                };
            }

            // aktualny nr rej
            var anr = poj.ElementAnyNs("oznaczeniePojazduAktualnyNrRejestracyjny");
            p.AktualnyNumerRejestracyjny = anr?.ValueOf("numerOznaczenia");

            resp.Pojazd = p;
            return resp;
        }
    }
}
