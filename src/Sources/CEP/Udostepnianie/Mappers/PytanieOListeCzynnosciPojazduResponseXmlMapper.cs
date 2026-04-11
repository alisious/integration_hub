// IntegrationHub.Sources.CEP.Udostepnianie.Mappers/PytanieOListeCzynnosciPojazduResponseXmlMapper.cs
using System.Xml.Linq;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Mappers
{
    public static class PytanieOListeCzynnosciPojazduResponseXmlMapper
    {
        private static readonly XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
        private static readonly XNamespace elem = "http://elementy.cep.udo.api.cepik.coi.gov.pl";

        public static PytanieOListeCzynnosciPojazduResponse Parse(string xml)
        {
            var doc = XDocument.Parse(xml);
            var body = doc.Root?.Element(soap + "Body");
            var rezultat = body?.Element(elem + "pytanieOListeCzynnosciPojazduRezultat");

            var resp = new PytanieOListeCzynnosciPojazduResponse();

            // daneRezultatu → meta
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

            // czynnoscCEP (wiele)
            foreach (var c in rezultat?.ElementsAnyNs("czynnoscCEP") ?? Enumerable.Empty<XElement>())
            {
                var dto = new CzynnoscCepDto
                {
                    IdentyfikatorCzynnosci = c.ValueOf("identyfikatorCzynnosci"),
                    IdentyfikatorSystemowyPojazdu = c.ValueOf("identyfikatorSystemowyPojazdu"),
                    DataStanu = c.ValueOf("dataStanu"),
                    DataOdnotowania = c.ValueOf("dataOdnotowania"),
                    CzyKorekta = c.ValueOf("czyKorekta")?.ToBool()
                };

                // rodzajCzynnosci
                var rc = c.ElementAnyNs("rodzajCzynnosci");
                if (rc is not null)
                {
                    dto.RodzajCzynnosci = new RodzajCzynnosciDto
                    {
                        DataOd = rc.ValueOf("dataOd"),
                        DataDo = rc.ValueOf("dataDo"),
                        Status = rc.ValueOf("statusRekordu"),
                        Kod = rc.ValueOf("kodCzynnosci"),              // także jako „Kod” (słownikowo)
                        KodCzynnosci = rc.ValueOf("kodCzynnosci"),
                        Modul = rc.ValueOf("modul"),
                        Opis = rc.ValueOf("opis"),
                        WartoscOpisowaSkrocona = null,                 // brak w źródle
                        WartoscOpisowa = rc.ValueOf("opis")
                    };
                }

                // podmiotUprawniony → OrganRozszerzonyDto
                var pu = c.ElementAnyNs("podmiotUprawniony");
                if (pu is not null)
                {
                    dto.PodmiotUprawniony = new OrganRozszerzonyDto
                    {
                        DataOd = pu.ValueOf("dataOd"),
                        DataDo = pu.ValueOf("dataDo"),
                        StatusRekordu = pu.ValueOf("statusRekordu"),
                        Kod = pu.ValueOf("kod"),
                        Nazwa = pu.ValueOf("nazwa"),
                        NumerEwidencyjny = pu.ValueOf("numerEwidencyjny"),
                        IdentyfikatorREGON = pu.ValueOf("identyfikatorREGON"),
                        REGON = pu.ValueOf("REGON"),
                        NazwaOrganuWydajacego = pu.ValueOf("nazwaOrganuWydajacego"),
                        Typ = new SlownikZakresowyDto
                        {
                            DataOd = pu.ElementAnyNs("typ")?.ValueOf("dataOd"),
                            DataDo = pu.ElementAnyNs("typ")?.ValueOf("dataDo"),
                            Status = pu.ElementAnyNs("typ")?.ValueOf("status"),
                            Kod = pu.ElementAnyNs("typ")?.ValueOf("kod"),
                            WartoscOpisowaSkrocona = pu.ElementAnyNs("typ")?.ValueOf("wartoscOpisowaSkrocona")
                        }
                    };
                }

                resp.CzynnoscCep.Add(dto);
            }

            return resp;
        }
    }
}
