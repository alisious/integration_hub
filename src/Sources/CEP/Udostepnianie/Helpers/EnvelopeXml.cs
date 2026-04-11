// IntegrationHub.Sources.CEP.Udostepnianie.Helpers/EnvelopeXml.cs
using System.Security;
using System.Text;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Helpers
{
    internal static class EnvelopeXml
    {
        public static string X(string? s) => SecurityElement.Escape(s ?? string.Empty) ?? string.Empty;

        /// <summary>Łączy stały header SOAP + otwarcie BODY oraz tag root.</summary>
        public static void BeginEnvelope(StringBuilder sb, string rootLocalName)
        {
            //sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:elem=\"http://elementy.cep.udo.api.cepik.coi.gov.pl\">");
            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append($"<elem:{rootLocalName}>");
        }

        /// <summary>Zamyka tag root, BODY i Envelope.</summary>
        public static void EndEnvelope(StringBuilder sb, string rootLocalName)
        {
            sb.Append($"</elem:{rootLocalName}>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");
        }

        /// <summary>Wspólny blok &lt;parametryPytania&gt; z wpisami „ŻW”.</summary>
        public static void AppendParametryPytania(StringBuilder sb, string requestId)
        {
            sb.Append("<parametryPytania>");
            sb.Append("<identyfikatorSystemuZewnetrznego>ŻW</identyfikatorSystemuZewnetrznego>");
            sb.Append("<wnioskodawca>");
            sb.Append($"<znakSprawy>{X(requestId)}</znakSprawy>");
            sb.Append("<wnioskodawca>ŻW</wnioskodawca>");
            sb.Append("</wnioskodawca>");
            sb.Append("</parametryPytania>");
        }

        /// <summary>Wspólny blok &lt;parametryCzasowe&gt; (dla zapytań „o pojazd” / „o pojazd rozszerzone”).</summary>
        public static void AppendParametryCzasowe(StringBuilder sb, string? dataPrezentacji, bool wyszukiwaniePoDanychHistorycznych)
        {
            sb.Append("<parametryCzasowe>");
            if (!string.IsNullOrWhiteSpace(dataPrezentacji))
                sb.Append($"<dataPrezentacji>{X(dataPrezentacji)}</dataPrezentacji>");
            sb.Append($"<wyszukiwaniePoDanychHistorycznych>{(wyszukiwaniePoDanychHistorycznych ? "true" : "false")}</wyszukiwaniePoDanychHistorycznych>");
            sb.Append("</parametryCzasowe>");
        }

        public static void AppendIfValue(StringBuilder sb, string tag, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                sb.Append('<').Append(tag).Append('>').Append(EnvelopeXml.X(value)).Append("</").Append(tag).Append('>');
        }
    }
}
