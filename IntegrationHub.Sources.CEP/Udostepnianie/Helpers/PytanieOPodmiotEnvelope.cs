// IntegrationHub.Sources.CEP.Udostepnianie.Helpers/PytanieOPodmiotEnvelope.cs
using System.Text;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Helpers
{
    public static class PytanieOPodmiotEnvelope
    {
        public static string Create(PytanieOPodmiotRequest body, string requestId)
        {
            var sb = new StringBuilder(512);
            EnvelopeXml.BeginEnvelope(sb, "pytanieOPodmiot");
            EnvelopeXml.AppendParametryPytania(sb, requestId);

            sb.Append("<parametryPodmiotu>");
            sb.Append("<parametryPodmiotu>");
            sb.Append($"<identyfikatorSystemowyPodmiotu>{EnvelopeXml.X(body.IdentyfikatorSystemowyPodmiotu)}</identyfikatorSystemowyPodmiotu>");
            sb.Append("</parametryPodmiotu>");
            sb.Append("</parametryPodmiotu>");

            EnvelopeXml.EndEnvelope(sb, "pytanieOPodmiot");
            return sb.ToString();
        }
    }
}
