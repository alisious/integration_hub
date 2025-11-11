using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.Sources.CEP.UpKi.Helpers
{



    internal static class EnvelopeXml
    {
        public static string X(string? s) => SecurityElement.Escape(s ?? string.Empty) ?? string.Empty;

        /// <summary>Łączy stały header SOAP + otwarcie BODY oraz tag root.</summary>
        public static void BeginEnvelope(StringBuilder sb)
        {
            //sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:upr=\"http://uprawnieniakierowcow-na-date-zu.tru.api.cepik.coi.gov.pl\">");
            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append($"<upr:DaneDokumentuRequest>");
        }

        /// <summary>Zamyka tag root, BODY i Envelope.</summary>
        public static void EndEnvelope(StringBuilder sb)
        {
            sb.Append($"</upr:DaneDokumentuRequest>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");
        }

        public static void AppendDanePesel(StringBuilder sb, string? numerPesel, string? dataZapytania)
        {
            sb.Append("<danePesel>");
            sb.Append($"<numerPesel>{EnvelopeXml.X(numerPesel)}</numerPesel>");
            sb.Append($"<dataZapytania>{EnvelopeXml.X(dataZapytania)}</dataZapytania>");
            sb.Append("</danePesel>");
        }

        public static void AppendDaneOsoby(StringBuilder sb, string? imiePierwsze, string? nazwisko, string? dataUrodzenia, string? dataZapytania)
        {
            sb.Append("<daneOsoby>");
            sb.Append($"<imiePierwsze>{EnvelopeXml.X(imiePierwsze)}</imiePierwsze>");
            sb.Append($"<nazwisko>{EnvelopeXml.X(nazwisko)}</nazwisko>");
            sb.Append($"<dataUrodzenia>{EnvelopeXml.X(dataUrodzenia)}</dataUrodzenia>");
            sb.Append($"<dataZapytania>{EnvelopeXml.X(dataZapytania)}</dataZapytania>");
            sb.Append("</daneOsoby>");
        }
        public static void AppendIfValue(StringBuilder sb, string tag, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                sb.Append('<').Append(tag).Append('>').Append(EnvelopeXml.X(value)).Append("</").Append(tag).Append('>');
        }
    }



}
