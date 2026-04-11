// IntegrationHub.Sources.CEP.Udostepnianie.Mappers/PytanieOPodmiotResponseXmlMapper.cs
using System.Xml.Linq;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Mappers
{
    public static class PytanieOPodmiotResponseXmlMapper
    {
        private static readonly XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
        private static readonly XNamespace elem = "http://elementy.cep.udo.api.cepik.coi.gov.pl";

        public static PytanieOPodmiotResponse Parse(string xml)
        {
            var doc = XDocument.Parse(xml);
            var body = doc.Root?.Element(soap + "Body");
            var rezultat = body?.Element(elem + "pytanieOPodmiotRezultat");

            var resp = new PytanieOPodmiotResponse();

            // --- Meta ---
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

            var podmiot = rezultat?.ElementAnyNs("podmiot");
            if (podmiot is null) return resp;

            var root = new PodmiotAnyDto
            {
                IdentyfikatorSystemowyPodmiotu = podmiot.ValueOf("identyfikatorSystemowyPodmiotu"),
                WariantPodmiotu = podmiot.ValueOf("wariantPodmiotu")
            };

            // --- OSOBA ---
            var os = podmiot.ElementAnyNs("osoba");
            if (os is not null)
            {
                root.Osoba = new OsobaDto
                {
                    PESEL = os.ValueOf("PESEL"),
                    ImiePierwsze = os.ValueOf("imiePierwsze"),
                    Nazwisko = os.ValueOf("nazwisko"),
                    DataUrodzenia = os.ValueOf("dataUrodzenia"),
                    MiejsceUrodzeniaKod = os.ValueOf("miejsceUrodzeniaKod"),
                    MiejsceUrodzenia = os.ValueOf("miejsceUrodzenia"),
                    Adres = MapAdres(os.ElementAnyNs("adres"))
                };
            }

            // --- FIRMA ---
            var fm = podmiot.ElementAnyNs("firma");
            if (fm is not null)
            {
                root.Firma = new FirmaDto
                {
                    REGON = fm.ValueOf("REGON"),
                    NazwaFirmy = fm.ValueOf("nazwaFirmy"),
                    NazwaFirmyDrukowana = fm.ValueOf("nazwaFirmyDrukowana"),
                    IdentyfikatorSystemowyREGON = fm.ValueOf("identyfikatorSystemowyREGON"),
                    FormaWlasnosci = MapSlownikZakresowy(fm.ElementAnyNs("formaWlasnosci")),
                    Adres = MapAdres(fm.ElementAnyNs("adres"))
                };
            }

            resp.Podmiot = root;
            return resp;
        }

        // ===== helpers =====
        private static AdresDto? MapAdres(XElement? adres)
        {
            if (adres is null) return null;

            return new AdresDto
            {
                Kraj = adres.ElementAnyNs("kraj") is { } kraj
                    ? new KrajDto
                    {
                        DataOd = kraj.ValueOf("dataOd"),
                        DataDo = kraj.ValueOf("dataDo"),
                        StatusRekordu = kraj.ValueOf("statusRekordu"),
                        KodNumeryczny = kraj.ValueOf("kodNumeryczny"),
                        KodIsoAlfa2 = kraj.ValueOf("kodIsoAlfa2"),
                        KodIsoAlfa3 = kraj.ValueOf("kodIsoAlfa3"),
                        KodMks = kraj.ValueOf("kodMks"),
                        CzyNalezyDoUE = kraj.ValueOf("czyNalezyDoUE").ToBool(),
                        Nazwa = kraj.ValueOf("nazwa"),
                        Obywatelstwo = kraj.ValueOf("obywatelstwo"),
                        DataAktualizacji = kraj.ValueOf("dataAktualizacji")
                    }
                    : null,
                KodTeryt = adres.ValueOf("kodTeryt"),
                KodTerytWojewodztwa = adres.ValueOf("kodTerytWojewodztwa"),
                NazwaWojewodztwaStanu = adres.ValueOf("nazwaWojewodztwaStanu"),
                KodTerytPowiatu = adres.ValueOf("kodTerytPowiatu"),
                NazwaPowiatuDzielnicy = adres.ValueOf("nazwaPowiatuDzielnicy"),
                KodTerytGminy = adres.ValueOf("kodTerytGminy"),
                NazwaGminy = adres.ValueOf("nazwaGminy"),
                KodRodzajuGminy = adres.ValueOf("kodRodzajuGminy"),
                KodPocztowy = adres.ValueOf("kodPocztowy"),
                KodTerytMiejscowosci = adres.ValueOf("kodTerytMiejscowosci"),
                NazwaMiejscowosci = adres.ValueOf("nazwaMiejscowosci"),
                NazwaMiejscowosciPodst = adres.ValueOf("nazwaMiejscowosciPodst"),
                KodTerytUlicy = adres.ValueOf("kodTerytUlicy"),
                UlicaCecha = MapSlownikZakresowy(adres.ElementAnyNs("ulicaCecha")),
                NazwaUlicy = adres.ValueOf("nazwaUlicy"),
                NumerDomu = adres.ValueOf("numerDomu"),
            };
        }

        private static SlownikZakresowyDto? MapSlownikZakresowy(XElement? x)
        {
            if (x is null) return null;
            return new SlownikZakresowyDto
            {
                DataOd = x.ValueOf("dataOd"),
                DataDo = x.ValueOf("dataDo"),
                Status = x.ValueOf("status"),
                Kod = x.ValueOf("kod"),
                WartoscOpisowaSkrocona = x.ValueOf("wartoscOpisowaSkrocona"),
                WartoscOpisowa = x.ValueOf("wartoscOpisowa")
            };
        }
    }
}
