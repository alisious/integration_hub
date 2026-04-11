// IntegrationHub.Sources.CEP.Udostepnianie.Mappers/PytanieOHistorieLicznikaResponseXmlMapper.cs
using System.Xml.Linq;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Mappers
{
    public static class PytanieOHistorieLicznikaResponseXmlMapper
    {
        private static readonly XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
        private static readonly XNamespace elem = "http://elementy.cep.udo.api.cepik.coi.gov.pl";

        public static PytanieOHistorieLicznikaResponse Parse(string xml)
        {
            var doc = XDocument.Parse(xml);
            var body = doc.Root?.Element(soap + "Body");
            var rezultat = body?.Element(elem + "pytanieOHistorieLicznikaRezultat");

            var resp = new PytanieOHistorieLicznikaResponse();

            // meta (daneRezultatu)
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
                    Wnioskodawca = daneRez.ValueOf("wnioskodawca"),
                };
            }

            // aktualnyIdentyfikatorPojazdu
            var aid = rezultat?.ElementAnyNs("aktualnyIdentyfikatorPojazdu");
            if (aid is not null)
            {
                resp.AktualnyIdPojazdu = new AktualnyIdPojazduDto
                {
                    IdentyfikatorSystemowyPojazdu = aid.ValueOf("identyfikatorSystemowyPojazdu"),
                    TokenAktualnosci = aid.ValueOf("tokenAktualnosci")
                };
            }

            // licznikDrogi -> historycznyStanLicznika (wiele)
            var licznik = rezultat?.ElementAnyNs("licznikDrogi");
            if (licznik is not null)
            {
                var ld = new LicznikDrogiDto();

                foreach (var h in licznik.ElementsAnyNs("historycznyStanLicznika"))
                {
                    var item = new HistorycznyStanLicznikaDto
                    {
                        IdentyfikatorSystemowyStanuLicznika = h.ValueOf("identyfikatorSystemowyStanuLicznika"),
                        StanLicznika = new StanLicznikaDto
                        {
                            IdentyfikatorSystemowyStanuLicznika = h.ValueOf("identyfikatorSystemowyStanuLicznika"),
                            WartoscStanuLicznika = h.ValueOf("wartoscStanuLicznika")?.ToInt(),
                            JednostkaStanuLicznika = new SlownikZakresowyDto
                            {
                                DataOd = h.ElementAnyNs("jednostkaStanuLicznika")?.ValueOf("dataOd"),
                                DataDo = h.ElementAnyNs("jednostkaStanuLicznika")?.ValueOf("dataDo"),
                                Kod = h.ElementAnyNs("jednostkaStanuLicznika")?.ValueOf("kod"),
                                WartoscOpisowaSkrocona = h.ElementAnyNs("jednostkaStanuLicznika")?.ValueOf("wartoscOpisowaSkrocona"),
                                Status = h.ElementAnyNs("jednostkaStanuLicznika")?.ValueOf("status"),
                            },
                            DataSpisaniaLicznika = h.ValueOf("dataSpisaniaLicznika"),
                            PodmiotWprowadzajacy = MapSkp(h.ElementAnyNs("podmiotWprowadzajacy")),
                            DataOdnotowania = h.ValueOf("dataOdnotowania")
                        },
                        SygnalZmiany = MapSygnal(h.ElementAnyNs("sygnalZmianyStanuLicznika")),
                        DaneCzynnosci = MapCzynnosci(h.ElementAnyNs("daneCzynnosci")),
                    };

                    // informacjeSKP (to szerszy blok – mapujemy do StanLicznika.PodmiotWprowadzajacy / oraz InformacjeSkpDto zgodnie z ResponseDto)
                    var infoSkp = h.ElementAnyNs("informacjeSKP");
                    if (infoSkp is not null)
                    {
                        item.InformacjeSkp = new InformacjeSkpDto
                        {
                            IdentyfikatorSystemowyInformacjiSkp = infoSkp.ValueOf("identyfikatorSystemowyInformacjiSKP"),
                            IdentyfikatorCzynnosci = infoSkp.ValueOf("identyfikatorCzynnosci"),
                            StacjaKontroliPojazdow = MapSkp(infoSkp.ElementAnyNs("stacjaKontroliPojazdow")),
                            // ... ew. inne pola jeśli są w odpowiedzi
                        };
                    }

                    ld.HistoryczneStanyLicznika.Add(item);
                }

                resp.LicznikDrogi = ld;
            }

            return resp;
        }

        private static SkpPodmiotDto? MapSkp(XElement? skp)
        {
            if (skp is null) return null;
            return new SkpPodmiotDto
            {
                DataOd = skp.ValueOf("dataOd"),
                DataDo = skp.ValueOf("dataDo"),
                StatusRekordu = skp.ValueOf("statusRekordu"),
                Kod = skp.ValueOf("kod"),
                Nazwa = skp.ValueOf("nazwa"),
                NumerEwidencyjny = skp.ValueOf("numerEwidencyjny"),
                IdentyfikatorREGON = skp.ValueOf("identyfikatorREGON"),
                REGON = skp.ValueOf("REGON"),
                Typ = new SlownikZakresowyDto
                {
                    DataOd = skp.ElementAnyNs("typ")?.ValueOf("dataOd"),
                    DataDo = skp.ElementAnyNs("typ")?.ValueOf("dataDo"),
                    Kod = skp.ElementAnyNs("typ")?.ValueOf("kod"),
                    WartoscOpisowaSkrocona = skp.ElementAnyNs("typ")?.ValueOf("wartoscOpisowaSkrocona"),
                    Status = skp.ElementAnyNs("typ")?.ValueOf("status")
                }
            };
        }

        private static SygnalZmianyLicznikaDto? MapSygnal(XElement? s)
        {
            if (s is null) return null;
            return new SygnalZmianyLicznikaDto
            {
                NieprawidlowoscWZmianieStanuLicznika = s.ValueOf("nieprawidlowoscWZmianieStanuLicznika").ToBool(),
                ZmianaStanuLicznika = s.ValueOf("zmianaStanuLicznika")?.ToInt(),
                JednostkaStanuLicznika = new SlownikZakresowyDto
                {
                    DataOd = s.ElementAnyNs("jednostkaStanuLicznika")?.ValueOf("dataOd"),
                    DataDo = s.ElementAnyNs("jednostkaStanuLicznika")?.ValueOf("dataDo"),
                    Kod = s.ElementAnyNs("jednostkaStanuLicznika")?.ValueOf("kod"),
                    WartoscOpisowaSkrocona = s.ElementAnyNs("jednostkaStanuLicznika")?.ValueOf("wartoscOpisowaSkrocona"),
                    Status = s.ElementAnyNs("jednostkaStanuLicznika")?.ValueOf("status")
                }
            };
        }

        private static DaneCzynnosciMinimalDto? MapCzynnosci(XElement? d)
        {
            if (d is null) return null;
            return new DaneCzynnosciMinimalDto
            {
                IdentyfikatorCzynnosci = d.ValueOf("identyfikatorCzynnosci"),
                RodzajCzynnosciKod = d.ElementAnyNs("rodzajCzynnosci")?.ValueOf("kodCzynnosci"),
                RodzajCzynnosciOpis = d.ElementAnyNs("rodzajCzynnosci")?.ValueOf("opis")
            };
        }
    }
}
