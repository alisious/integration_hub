// IntegrationHub.Sources.CEP.Udostepnianie.Mappers/PytanieODokumentPojazduResponseXmlMapper.cs
using System.Xml.Linq;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Mappers
{
    public static class PytanieODokumentPojazduResponseXmlMapper
    {
        private static readonly XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
        private static readonly XNamespace elem = "http://elementy.cep.udo.api.cepik.coi.gov.pl";

        public static PytanieODokumentPojazduResponse Parse(string xml)
        {
            var doc = XDocument.Parse(xml);
            var body = doc.Root?.Element(soap + "Body");
            var rezultat = body?.Element(elem + "pytanieODokumentPojazduRezultat");

            var resp = new PytanieODokumentPojazduResponse();

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

            var p = new PojazdDokumentResponse();

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

            // dokumentPojazdu
            var d = poj.ElementAnyNs("dokumentPojazdu");
            if (d is not null)
            {
                var docDto = new DokumentPojazduFullDto
                {
                    IdentyfikatorSystemowyDokumentuPojazdu = d.ValueOf("identyfikatorSystemowyDokumentuPojazdu"),
                    DokumentSeriaNumer = d.ValueOf("dokumentSeriaNumer"),
                    CzyWtornik = d.ValueOf("czyWtornik"),
                    DataWydaniaDokumentu = d.ValueOf("dataWydaniaDokumentu"),

                    TypDokumentu = new TypKodOpisDto
                    {
                        DataOd = d.ElementAnyNs("typDokumentu")?.ValueOf("dataOd"),
                        DataDo = d.ElementAnyNs("typDokumentu")?.ValueOf("dataDo"),
                        Kod = d.ElementAnyNs("typDokumentu")?.ValueOf("kod"),
                        WartoscOpisowaSkrocona = d.ElementAnyNs("typDokumentu")?.ValueOf("wartoscOpisowaSkrocona"),
                        WartoscOpisowa = d.ElementAnyNs("typDokumentu")?.ValueOf("wartoscOpisowa"),
                        DataAktualizacji = d.ElementAnyNs("typDokumentu")?.ValueOf("dataAktualizacji"),
                        Status = d.ElementAnyNs("typDokumentu")?.ValueOf("status")
                    },

                    OrganWydajacyDokument = ParseOrgan(d.ElementAnyNs("organWydajacyDokument")),
                    HomologacjaPojazdu = ParseHomologacja(d.ElementAnyNs("homologacjaPojazdu")),
                    DaneOpisujacePojazd = ParseDaneOpisujace(d.ElementAnyNs("daneOpisujacePojazd"))
                };

                // stanDokumentu (wiele)
                foreach (var sd in d.ElementsAnyNs("stanDokumentu"))
                {
                    var stan = new StanDokumentuDto
                    {
                        IdentyfikatorSystemowyStanuDokumentuPojazdu = sd.ValueOf("identyfikatorSystemowyStanuDokumentuPojazdu"),
                        DataPoczatkuObowiazywania = sd.ValueOf("dataPoczatkuObowiazywania"),
                        Stan = new TypKodOpisDto
                        {
                            DataOd = sd.ElementAnyNs("stanDokumentu")?.ValueOf("dataOd"),
                            DataDo = sd.ElementAnyNs("stanDokumentu")?.ValueOf("dataDo"),
                            Kod = sd.ElementAnyNs("stanDokumentu")?.ValueOf("kod"),
                            WartoscOpisowaSkrocona = sd.ElementAnyNs("stanDokumentu")?.ValueOf("wartoscOpisowaSkrocona"),
                            DataAktualizacji = sd.ElementAnyNs("stanDokumentu")?.ValueOf("dataAktualizacji"),
                            Status = sd.ElementAnyNs("stanDokumentu")?.ValueOf("status")
                        },
                        OrganUstanawiajacyStan = ParseOrgan(sd.ElementAnyNs("organUstanawiajacyStan")),
                        DataOdnotowaniaStanu = sd.ValueOf("dataOdnotowaniaStanu")
                    };
                    docDto.StanyDokumentu.Add(stan);
                }

                // oznaczeniePojazdu (wiele)
                foreach (var oz in d.ElementsAnyNs("oznaczeniePojazdu"))
                {
                    var ozn = new OznaczeniePojazduDokumentDto
                    {
                        IdentyfikatorSystemowyOznaczenia = oz.ValueOf("identyfikatorSystemowyOznaczenia"),
                        TypOznaczenia = new TypKodOpisDto
                        {
                            DataOd = oz.ElementAnyNs("typOznaczenia")?.ValueOf("dataOd"),
                            DataDo = oz.ElementAnyNs("typOznaczenia")?.ValueOf("dataDo"),
                            Kod = oz.ElementAnyNs("typOznaczenia")?.ValueOf("kod"),
                            WartoscOpisowaSkrocona = oz.ElementAnyNs("typOznaczenia")?.ValueOf("wartoscOpisowaSkrocona"),
                            WartoscOpisowa = oz.ElementAnyNs("typOznaczenia")?.ValueOf("wartoscOpisowa"),
                            DataAktualizacji = oz.ElementAnyNs("typOznaczenia")?.ValueOf("dataAktualizacji"),
                            Status = oz.ElementAnyNs("typOznaczenia")?.ValueOf("status")
                        },
                        NumerOznaczenia = oz.ValueOf("numerOznaczenia"),
                        CzyWtornik = oz.ValueOf("czyWtornik"),
                        RodzajTablicyRejestracyjnej = ToTyp(oz.ElementAnyNs("rodzajTablicyRejestracyjnej")),
                        WzorTablicyRejestracyjnej = ToTyp(oz.ElementAnyNs("wzorTablicyRejestracyjnej")),
                        KolorTablicyRejestracyjnej = ToTyp(oz.ElementAnyNs("kolorTablicyRejestracyjnej")),
                        StanOznaczenia = ParseStanOznaczenia(oz.ElementAnyNs("stanOznaczenia"))
                    };
                    docDto.OznaczeniaPojazdu.Add(ozn);
                }

                p.DokumentPojazdu = docDto;
            }

            resp.Pojazd = p;
            return resp;
        }

        private static OrganDokumentDto? ParseOrgan(XElement? x)
        {
            if (x is null) return null;
            return new OrganDokumentDto
            {
                Kod = x.ValueOf("kod"),
                Nazwa = x.ValueOf("nazwa"),
                NumerEwidencyjny = x.ValueOf("numerEwidencyjny"),
                REGON = x.ValueOf("REGON"),
                NazwaOrganuWydajacego = x.ValueOf("nazwaOrganuWydajacego"),
                Typ = ToTyp(x.ElementAnyNs("typ"))
            };
        }

        private static HomologacjaDokumentDto? ParseHomologacja(XElement? x)
        {
            if (x is null) return null;
            return new HomologacjaDokumentDto
            {
                IdentyfikatorSystemowyHomologacjiPojazdu = x.ValueOf("identyfikatorSystemowyHomologacjiPojazdu"),
                IdentyfikatorPozycjiKatalogowej = x.ValueOf("identyfikatorPozycjiKatalogowej"),
                NumerDokumentuHomologacji = x.ValueOf("numerDokumentuHomologacji"),
                WersjaKod = x.ElementAnyNs("wersjaPojazdu")?.ValueOf("kod"),
                WariantKod = x.ElementAnyNs("wariantPojazdu")?.ValueOf("kod"),
                TypKod = x.ElementAnyNs("typPojazdu")?.ValueOf("kod")
            };
        }

        private static DaneOpisujacePojazdDokumentDto? ParseDaneOpisujace(XElement? x)
        {
            if (x is null) return null;
            return new DaneOpisujacePojazdDokumentDto
            {
                IdentyfikatorSystemowyDanychPojazdu = x.ValueOf("identyfikatorSystemowyDanychPojazdu"),
                Marka = x.ElementAnyNs("marka")?.ValueOf("wartoscOpisowa"),
                KodMarki = x.ElementAnyNs("marka")?.ValueOf("kod"),
                Model = x.ElementAnyNs("model")?.ValueOf("wartoscOpisowa"),
                KodModelu = x.ElementAnyNs("model")?.ValueOf("kod"),
                Rodzaj = x.ElementAnyNs("rodzaj")?.ValueOf("rodzaj"),
                Podrodzaj = x.ElementAnyNs("podrodzaj")?.ValueOf("podrodzaj"),
                KodRPP = x.ElementAnyNs("kodRPP")?.ValueOf("kodRPP"),
                NumerPodwoziaNadwoziaRamy = x.ValueOf("numerPodwoziaNadwoziaRamy"),
                RokProdukcji = x.ValueOf("rokProdukcji")?.ToInt()
            };
        }

        private static StanOznaczeniaDokumentuDto? ParseStanOznaczenia(XElement? x)
        {
            if (x is null) return null;
            return new StanOznaczeniaDokumentuDto
            {
                IdentyfikatorSystemowyStanuOznaczeniaPojazdu = x.ValueOf("identyfikatorSystemowyStanuOznaczeniaPojazdu"),
                DataPoczatkuObowiazywania = x.ValueOf("dataPoczatkuObowiazywania"),
                Stan = ToTyp(x.ElementAnyNs("stanOznaczenia")),
                OrganUstanawiajacyStan = ParseOrgan(x.ElementAnyNs("organUstanawiajacyStan")),
                DataOdnotowaniaStanu = x.ValueOf("dataOdnotowaniaStanu")
            };
        }

        private static TypKodOpisDto? ToTyp(XElement? x)
        {
            if (x is null) return null;
            return new TypKodOpisDto
            {
                DataOd = x.ValueOf("dataOd"),
                DataDo = x.ValueOf("dataDo"),
                Kod = x.ValueOf("kod"),
                WartoscOpisowaSkrocona = x.ValueOf("wartoscOpisowaSkrocona"),
                WartoscOpisowa = x.ValueOf("wartoscOpisowa"),
                DataAktualizacji = x.ValueOf("dataAktualizacji"),
                Status = x.ValueOf("status")
            };
        }
    }
}
