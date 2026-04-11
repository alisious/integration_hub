// IntegrationHub.Sources.CEP.Udostepnianie.Helpers/PytanieOListeCzynnosciPojazduEnvelope.cs
using System.Text;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Helpers
{
    public static class PytanieOListeCzynnosciPojazduEnvelope
    {
        public static string Create(PytanieOListeCzynnosciPojazduRequest body, string requestId)
        {
            var sb = new StringBuilder(512);
            EnvelopeXml.BeginEnvelope(sb, "pytanieOListeCzynnosciPojazdu");
            EnvelopeXml.AppendParametryPytania(sb, requestId);

            sb.Append("<parametryCzynnosci>");
            if (!string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPojazdu))
                sb.Append($"<identyfikatorSystemowyPojazdu>{EnvelopeXml.X(body.IdentyfikatorSystemowyPojazdu)}</identyfikatorSystemowyPojazdu>");
            sb.Append("</parametryCzynnosci>");

            EnvelopeXml.EndEnvelope(sb, "pytanieOListeCzynnosciPojazdu");
            return sb.ToString();
        }
    }
}
