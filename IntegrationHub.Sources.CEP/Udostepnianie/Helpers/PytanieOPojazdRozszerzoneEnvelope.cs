// IntegrationHub.Sources.CEP.Udostepnianie.Helpers/PytanieOPojazdRozszerzoneEnvelope.cs
using System.Text;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Helpers
{
    public static class PytanieOPojazdRozszerzoneEnvelope
    {
        public static string Create(PytanieOPojazdRequest body, string requestId)
        {
            var sb = new StringBuilder(1024);
            EnvelopeXml.BeginEnvelope(sb, "pytanieOPojazdRozszerzone");
            EnvelopeXml.AppendParametryPytania(sb, requestId);

            sb.Append("<parametryPojazdu>");
            EnvelopeXml.AppendParametryCzasowe(sb, body.DataPrezentacji, body.WyszukiwaniePoDanychHistorycznych);

            var hasIdSystemowy = !string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPojazdu);
            var hasRejestr = !string.IsNullOrWhiteSpace(body.NumerRejestracyjny);
            var hasVin = !string.IsNullOrWhiteSpace(body.NumerPodwoziaNadwoziaRamy);
            var hasDok = body.ParametryDokumentuPojazdu is { TypDokumentu: { Length: > 0 }, DokumentSeriaNumer: { Length: > 0 } };
            var hasIdSystemowyPodmiotu = !string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPodmiotu);
            var hasNumerRejestracyjnyZagraniczny = !string.IsNullOrWhiteSpace(body.NumerRejestracyjnyZagraniczny);

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

                if (hasNumerRejestracyjnyZagraniczny)
                {
                    sb.Append("<parametryPojazduSprowadzonego>");
                    EnvelopeXml.AppendIfValue(sb, "numerRejestracyjnyZagraniczny", body.NumerRejestracyjnyZagraniczny);
                    sb.Append("</parametryPojazduSprowadzonego>");
                }


                if (hasIdSystemowyPodmiotu)
                {
                    sb.Append("<parametryPodmiotu>");
                    EnvelopeXml.AppendIfValue(sb, "identyfikatorSystemowyPodmiotu", body.IdentyfikatorSystemowyPodmiotu);
                    sb.Append("</parametryPodmiotu>");
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
            EnvelopeXml.EndEnvelope(sb, "pytanieOPojazdRozszerzone");
            return sb.ToString();
        }
    }
}
