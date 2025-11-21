using System;
using System.Text;
using IntegrationHub.Common.Helpers;
using IntegrationHub.Infrastructure.Audit;
using IntegrationHub.Sources.KSIP.Contracts;

namespace IntegrationHub.Sources.KSIP.Helpers
{
    public static class SprawdzenieOsobyEnvelope
    {
        /// <summary>
        /// Buduje kopertę SOAP dla operacji SprawdzenieOsoby w ruchu drogowym.
        /// </summary>
        public static string Create(
            SprawdzenieOsobyRequest body,
            string requestId,
            string ksipUnitId,
            string systemName = "ŻW",
            string applicationName = "ŻW",
            string moduleName = "ZW-KSIP",
            string terminalName = "ZW-KSIP")
        {
            if (body is null)
                throw new ArgumentNullException(nameof(body));

            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("RequestId nie może być puste.", nameof(requestId));

            if (string.IsNullOrWhiteSpace(body.UserId))
                throw new ArgumentException("UserId w SprawdzenieOsobyRequest nie może być puste.", nameof(body));

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var sb = new StringBuilder();
            // Nagłówek SOAP + deklaracje przestrzeni nazw
            sb.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:spr=\"http://policja.gov.pl/RuchDrogowy/SprawdzenieOsoby\">");
            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append("<spr:SprawdzenieOsobyRequest>");
            sb.Append("<spr:RequestHeader>");
            EnvelopeXmlHelper.AppendIfValue(sb, "spr:RequestID", requestId);
            EnvelopeXmlHelper.AppendIfValue(sb, "spr:Timestamp", timestamp);
            sb.Append("<spr:AuditRecord>");
            sb.Append($"<spr:SystemName>{systemName}</spr:SystemName>");
            sb.Append($"<spr:ApplicationName>{applicationName}</spr:ApplicationName>");
            sb.Append($"<spr:ModuleName>{moduleName}</spr:ModuleName>");
            EnvelopeXmlHelper.AppendIfValue(sb, "spr:TerminalName", terminalName);
            sb.Append("<spr:UserProfile>");
            EnvelopeXmlHelper.AppendIfValue(sb, "spr:UserID", body.UserId);
            EnvelopeXmlHelper.AppendIfValue(sb, "spr:UnitID", ksipUnitId);
            sb.Append("</spr:UserProfile>");
            sb.Append("</spr:AuditRecord>");
            sb.Append("</spr:RequestHeader>");
            sb.Append(GenerateRequestBodyXml(body));
            sb.Append("</spr:SprawdzenieOsobyRequest>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");

            return sb.ToString();
        }


        private static string GenerateRequestBodyXml(SprawdzenieOsobyRequest body)
        {
            var hasPesel = !string.IsNullOrWhiteSpace(body.NrPesel);

            var sb = new StringBuilder();
            sb.Append("<spr:RequestBody>");
            if (hasPesel)
            {
                EnvelopeXmlHelper.AppendIfValue(sb, "spr:NrPESEL", body.NrPesel);
            }
            else
            {
                sb.Append("<spr:Person>");
                sb.Append("<spr:PersonName>");
                EnvelopeXmlHelper.AppendIfValue(sb, "spr:FirstName", body.FirstName);
                EnvelopeXmlHelper.AppendIfValue(sb, "spr:LastName", body.LastName);
                sb.Append("</spr:PersonName>");
                EnvelopeXmlHelper.AppendIfValue(sb, "spr:BirthDate", body.BirthDate);
                sb.Append("</spr:Person>");
            }
            sb.Append("</spr:RequestBody>");
            return sb.ToString();
        }
    }
}
