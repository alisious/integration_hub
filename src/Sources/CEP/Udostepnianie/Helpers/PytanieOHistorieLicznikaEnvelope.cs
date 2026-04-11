// IntegrationHub.Sources.CEP.Udostepnianie.Helpers/PytanieOHistorieLicznikaEnvelope.cs
using System.Text;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Helpers
{
    public static class PytanieOHistorieLicznikaEnvelope
    {
        public static string Create(PytanieOHistorieLicznikaRequest body, string requestId)
        {
            var sb = new StringBuilder(512);
            EnvelopeXml.BeginEnvelope(sb, "pytanieOHistorieLicznika");
            EnvelopeXml.AppendParametryPytania(sb, requestId);

            if (!string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPojazdu))
            {
                sb.Append("<identyfikatorSystemowyPojazdu>");
                sb.Append(EnvelopeXml.X(body.IdentyfikatorSystemowyPojazdu));
                sb.Append("</identyfikatorSystemowyPojazdu>");
            }

            EnvelopeXml.EndEnvelope(sb, "pytanieOHistorieLicznika");
            return sb.ToString();
        }
    }
}
