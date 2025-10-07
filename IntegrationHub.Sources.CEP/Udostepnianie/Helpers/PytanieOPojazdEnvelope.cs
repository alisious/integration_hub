// IntegrationHub.Sources.CEP.Udostepnianie.Helpers/PytanieOPojazdEnvelope.cs
using System.Text;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Helpers
{
    public static class PytanieOPojazdEnvelope
    {
        public static string Create(PytanieOPojazdRequest body, string requestId)
        {
            var sb = new StringBuilder(1024);
            EnvelopeXml.BeginEnvelope(sb, "pytanieOPojazd");
            EnvelopeXml.AppendParametryPytania(sb, requestId);

            sb.Append("<parametryPojazdu>");
            EnvelopeXml.AppendParametryCzasowe(sb, body.DataPrezentacji, body.WyszukiwaniePoDanychHistorycznych);

            var hasIdSystemowy = !string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPojazdu);
            var hasRejestr = !string.IsNullOrWhiteSpace(body.NumerRejestracyjny);
            var hasVin = !string.IsNullOrWhiteSpace(body.NumerPodwoziaNadwoziaRamy);
            var hasDok = body.ParametryDokumentuPojazdu is { TypDokumentu: { Length: > 0 }, DokumentSeriaNumer: { Length: > 0 } };

            if (hasIdSystemowy)
            {
                sb.Append($"<identyfikatorSystemowyPojazdu>{EnvelopeXml.X(body.IdentyfikatorSystemowyPojazdu)}</identyfikatorSystemowyPojazdu>");
            }
            else
            {
                sb.Append("<parametryDanychPojazdu>");

                if (hasRejestr)
                    sb.Append($"<parametryOznaczeniaPojazdu><numerRejestracyjny>{EnvelopeXml.X(body.NumerRejestracyjny)}</numerRejestracyjny></parametryOznaczeniaPojazdu>");

                if (hasDok && body.ParametryDokumentuPojazdu is not null)
                {
                    sb.Append("<parametryDokumentuPojazdu>");
                    sb.Append("<typDokumentu>");
                    sb.Append($"<kod>{EnvelopeXml.X(body.ParametryDokumentuPojazdu.TypDokumentu!)}</kod>");
                    sb.Append("</typDokumentu>");
                    sb.Append($"<dokumentSeriaNumer>{EnvelopeXml.X(body.ParametryDokumentuPojazdu.DokumentSeriaNumer!)}</dokumentSeriaNumer>");
                    sb.Append("</parametryDokumentuPojazdu>");
                }

                if (hasVin)
                {
                    sb.Append("<parametryOpisujacePojazd>");
                    sb.Append($"<numerPodwoziaNadwoziaRamy>{EnvelopeXml.X(body.NumerPodwoziaNadwoziaRamy)}</numerPodwoziaNadwoziaRamy>");
                    sb.Append("</parametryOpisujacePojazd>");
                }

                sb.Append("</parametryDanychPojazdu>");
            }

            sb.Append("</parametryPojazdu>");
            EnvelopeXml.EndEnvelope(sb, "pytanieOPojazd");
            return sb.ToString();
        }
    }
}
