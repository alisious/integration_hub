// IntegrationHub.Sources.CEP.Udostepnianie.Helpers/PytanieODokumentPojazduEnvelope.cs
using System.Text;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Helpers
{
    public static class PytanieODokumentPojazduEnvelope
    {
        public static string Create(PytanieODokumentPojazduRequest body, string requestId)
        {
            var sb = new StringBuilder(768);
            EnvelopeXml.BeginEnvelope(sb, "pytanieODokumentPojazdu");
            EnvelopeXml.AppendParametryPytania(sb, requestId);

            sb.Append("<parametryDokumentuPojazdu>");

            if (!string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyDokumentuPojazdu))
            {
                sb.Append($"<identyfikatorSystemowyDokumentuPojazdu>{EnvelopeXml.X(body.IdentyfikatorSystemowyDokumentuPojazdu)}</identyfikatorSystemowyDokumentuPojazdu>");
            }
            else
            {
                sb.Append("<parametryDokumentuPojazdu>");
                sb.Append("<typDokumentu>");
                sb.Append($"<kod>{EnvelopeXml.X(body.TypDokumentu ?? "DICT155_DR")}</kod>");
                sb.Append("</typDokumentu>");
                sb.Append($"<dokumentSeriaNumer>{EnvelopeXml.X(body.DokumentSeriaNumer)}</dokumentSeriaNumer>");
                sb.Append("</parametryDokumentuPojazdu>");
            }

            sb.Append("</parametryDokumentuPojazdu>");
            EnvelopeXml.EndEnvelope(sb, "pytanieODokumentPojazdu");
            return sb.ToString();
        }
    }
}
