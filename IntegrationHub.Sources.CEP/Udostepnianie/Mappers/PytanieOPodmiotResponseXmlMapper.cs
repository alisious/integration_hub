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

            var podmiot = rezultat?.ElementAnyNs("podmiot");
            if (podmiot is null) return resp;

            var p = new PodmiotOsobaDto
            {
                IdentyfikatorSystemowyPodmiotu = podmiot.ValueOf("identyfikatorSystemowyPodmiotu"),
                WariantPodmiotu = podmiot.ValueOf("wariantPodmiotu")
            };

            var os = podmiot.ElementAnyNs("osoba");
            if (os is not null)
            {
                var adres = os.ElementAnyNs("adres");
                p.Osoba = new OsobaDto
                {
                    PESEL = os.ValueOf("PESEL"),
                    ImiePierwsze = os.ValueOf("imiePierwsze"),
                    Nazwisko = os.ValueOf("nazwisko"),
                    DataUrodzenia = os.ValueOf("dataUrodzenia"),
                    MiejsceUrodzeniaKod = os.ValueOf("miejsceUrodzeniaKod"),
                    MiejsceUrodzenia = os.ValueOf("miejsceUrodzenia"),
                    Adres = adres is null ? null : new AdresDto
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
                        UlicaCecha = adres.ElementAnyNs("ulicaCecha") is { } u
                            ? new SlownikZakresowyDto
                            {
                                DataOd = u.ValueOf("dataOd"),
                                DataDo = u.ValueOf("dataDo"),
                                Kod = u.ValueOf("kod"),
                                WartoscOpisowaSkrocona = u.ValueOf("wartoscOpisowaSkrocona"),
                                WartoscOpisowa = u.ValueOf("wartoscOpisowa"),
                                Status = u.ValueOf("status")
                            }
                            : null,
                        NazwaUlicy = adres.ValueOf("nazwaUlicy"),
                        NumerDomu = adres.ValueOf("numerDomu"),
                    }
                };
            }

            resp.Podmiot = p;
            return resp;
        }
    }
}
