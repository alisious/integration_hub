// IntegrationHub.Sources.CEP.Udostepnianie.Builders/CepRequestEnvelopeHelper.cs
using System.Security;
using System.Text;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Helpers
{
    public static class CepUdostepnianieEnvelopeHelper
    {
        public static string PreparePytanieOPojazdEnvelope(
            PytanieOPojazdRequest body,
            string requestId)
        {
            static string X(string? s) => SecurityElement.Escape(s ?? string.Empty) ?? string.Empty;

            var sb = new StringBuilder(1024);
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:elem=\"http://elementy.cep.udo.api.cepik.coi.gov.pl\">");
            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append("<elem:pytanieOPojazd>");

            // --- parametryPytania (stałe: "ŻW") ---
            sb.Append("<parametryPytania>");
            sb.Append("<identyfikatorSystemuZewnetrznego>ŻW</identyfikatorSystemuZewnetrznego>");
            sb.Append("<wnioskodawca>");
            sb.Append($"<znakSprawy>{X(requestId)}</znakSprawy>");
            sb.Append("<wnioskodawca>ŻW</wnioskodawca>");
            sb.Append("</wnioskodawca>");
            sb.Append("</parametryPytania>");

            // --- parametryPojazdu ---
            sb.Append("<parametryPojazdu>");

            // parametryCzasowe (wg. Twojego fragmentu; data opcjonalna, wyszukiwanie domyślnie false)
            sb.Append("<parametryCzasowe>");
            if (!string.IsNullOrWhiteSpace(body.DataPrezentacji))
                sb.Append($"<dataPrezentacji>{X(body.DataPrezentacji)}</dataPrezentacji>");
            sb.Append($"<wyszukiwaniePoDanychHistorycznych>{(body.WyszukiwaniePoDanychHistorycznych ? "true" : "false")}</wyszukiwaniePoDanychHistorycznych>");
            sb.Append("</parametryCzasowe>");

            var hasIdSystemowy = !string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPojazdu);
            var hasRejestr = !string.IsNullOrWhiteSpace(body.NumerRejestracyjny);
            var hasVin = !string.IsNullOrWhiteSpace(body.NumerPodwoziaNadwoziaRamy);
            var hasDok = body.ParametryDokumentuPojazdu is { TypDokumentu: { Length: > 0 }, DokumentSeriaNumer: { Length: > 0 } };

            if (hasIdSystemowy)
            {
                // wariant: identyfikatorSystemowyPojazdu
                sb.Append($"<identyfikatorSystemowyPojazdu>{X(body.IdentyfikatorSystemowyPojazdu)}</identyfikatorSystemowyPojazdu>");
            }
            else
            {
                sb.Append("<parametryDanychPojazdu>");

                if (hasRejestr)
                    sb.Append($"<parametryOznaczeniaPojazdu><numerRejestracyjny>{X(body.NumerRejestracyjny)}</numerRejestracyjny></parametryOznaczeniaPojazdu>");

                if (hasDok && body.ParametryDokumentuPojazdu is not null)
                {
                    sb.Append("<parametryDokumentuPojazdu>");
                        sb.Append("<typDokumentu>");
                            sb.Append($"<kod>{X(body.ParametryDokumentuPojazdu.TypDokumentu)}</kod>");
                        sb.Append("</typDokumentu>");
                        sb.Append($"<dokumentSeriaNumer>{X(body.ParametryDokumentuPojazdu.DokumentSeriaNumer)}</dokumentSeriaNumer>");
                    sb.Append("</parametryDokumentuPojazdu>");
                }

                if (hasVin)
                {
                    sb.Append("<parametryOpisujacePojazd>");
                    sb.Append($"<numerPodwoziaNadwoziaRamy>{X(body.NumerPodwoziaNadwoziaRamy)}</numerPodwoziaNadwoziaRamy>");
                    sb.Append("</parametryOpisujacePojazd>");
                }
                sb.Append("</parametryDanychPojazdu>");
            }

            sb.Append("</parametryPojazdu>");
            sb.Append("</elem:pytanieOPojazd>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");

            return sb.ToString();
        }

        public static string PreparePytanieOPojazdRozszerzoneEnvelope(
            PytanieOPojazdRequest body,
            string requestId)
        {
            static string X(string? s) => SecurityElement.Escape(s ?? string.Empty) ?? string.Empty;

            var sb = new StringBuilder(1024);
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:elem=\"http://elementy.cep.udo.api.cepik.coi.gov.pl\">");
            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append("<elem:pytanieOPojazdRozszerzone>"); // ⟵ jedyna zmiana vs wersja podstawowa

            // --- parametryPytania (stałe: "ŻW") ---
            sb.Append("<parametryPytania>");
            sb.Append("<identyfikatorSystemuZewnetrznego>ŻW</identyfikatorSystemuZewnetrznego>");
            sb.Append("<wnioskodawca>");
            sb.Append($"<znakSprawy>{X(requestId)}</znakSprawy>");
            sb.Append("<wnioskodawca>ŻW</wnioskodawca>");
            sb.Append("</wnioskodawca>");
            sb.Append("</parametryPytania>");

            // --- parametryPojazdu ---
            sb.Append("<parametryPojazdu>");

            // parametryCzasowe
            sb.Append("<parametryCzasowe>");
            if (!string.IsNullOrWhiteSpace(body.DataPrezentacji))
                sb.Append($"<dataPrezentacji>{X(body.DataPrezentacji)}</dataPrezentacji>");
            sb.Append($"<wyszukiwaniePoDanychHistorycznych>{(body.WyszukiwaniePoDanychHistorycznych ? "true" : "false")}</wyszukiwaniePoDanychHistorycznych>");
            sb.Append("</parametryCzasowe>");

            var hasIdSystemowy = !string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPojazdu);
            var hasRejestr = !string.IsNullOrWhiteSpace(body.NumerRejestracyjny);
            var hasVin = !string.IsNullOrWhiteSpace(body.NumerPodwoziaNadwoziaRamy);
            var hasDok = body.ParametryDokumentuPojazdu is { TypDokumentu: { Length: > 0 }, DokumentSeriaNumer: { Length: > 0 } };

            if (hasIdSystemowy)
            {
                sb.Append($"<identyfikatorSystemowyPojazdu>{X(body.IdentyfikatorSystemowyPojazdu)}</identyfikatorSystemowyPojazdu>");
            }
            else
            {
                sb.Append("<parametryDanychPojazdu>");

                if (hasRejestr)
                    sb.Append($"<parametryOznaczeniaPojazdu><numerRejestracyjny>{X(body.NumerRejestracyjny)}</numerRejestracyjny></parametryOznaczeniaPojazdu>");

                if (hasDok && body.ParametryDokumentuPojazdu is not null)
                {
                    sb.Append("<parametryDokumentuPojazdu>");
                    sb.Append("<typDokumentu>");
                    sb.Append($"<kod>{X(body.ParametryDokumentuPojazdu.TypDokumentu)}</kod>");
                    sb.Append("</typDokumentu>");
                    sb.Append($"<dokumentSeriaNumer>{X(body.ParametryDokumentuPojazdu.DokumentSeriaNumer)}</dokumentSeriaNumer>");
                    sb.Append("</parametryDokumentuPojazdu>");
                }

                if (hasVin)
                {
                    sb.Append("<parametryOpisujacePojazd>");
                    sb.Append($"<numerPodwoziaNadwoziaRamy>{X(body.NumerPodwoziaNadwoziaRamy)}</numerPodwoziaNadwoziaRamy>");
                    sb.Append("</parametryOpisujacePojazd>");
                }
                sb.Append("</parametryDanychPojazdu>");
            }

            sb.Append("</parametryPojazdu>");
            sb.Append("</elem:pytanieOPojazdRozszerzone>"); // ⟵ zamknięcie podmienione analogicznie
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");

            return sb.ToString();
        }
    }
}
