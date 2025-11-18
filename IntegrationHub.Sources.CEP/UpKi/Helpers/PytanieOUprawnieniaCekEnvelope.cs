// IntegrationHub.Sources.CEP.UpKi.Helpers/PytanieOUprawnieniaCekEnvelope.cs
using IntegrationHub.Sources.CEP.UpKi.Contracts;
using System.Security;
using System.Text;

namespace IntegrationHub.Sources.CEP.UpKi.Helpers
{
    /// <summary>
    /// Buduje SOAP Envelope dla operacji pytanieOUprawnieniaCek
    /// zgodnie z uprawnieniakierowcow-cek.tru.api.cepik.coi.gov.pl.
    /// </summary>
    public static class PytanieOUprawnieniaCekEnvelope
    {
        private const string UpkiNamespace = "http://uprawnieniakierowcow-cek.tru.api.cepik.coi.gov.pl";

        /// <summary>
        /// Główna metoda budująca XML requestu.
        /// </summary>
        public static string Create(DaneDokumentuRequest body)
        {
            var sb = new StringBuilder(1024);

            // Nagłówek SOAP + deklaracje przestrzeni nazw
            sb.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" ")
              .Append("xmlns:upr=\"").Append(UpkiNamespace).Append("\">");
            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append("<upr:DaneDokumentuRequest>");

            // dataZapytania (opcjonalna, xsd:date, format: yyyy-MM-dd)
            AppendIfValue(sb, "dataZapytania", body.DataZapytania);

            // CHOICE (dokładnie jedno z poniższych powinno być ustawione – pilnowane w walidatorze)
            AppendIfValue(sb, "numerPesel", body.NumerPesel);
            AppendIfValue(sb, "numerDokumentu", body.NumerDokumentu);
            AppendIfValue(sb, "seriaNumerDokumentu", body.SeriaNumerDokumentu);

            if (body.DaneOsoby is not null &&
                (!string.IsNullOrWhiteSpace(body.DaneOsoby.ImiePierwsze) ||
                 !string.IsNullOrWhiteSpace(body.DaneOsoby.Nazwisko) ||
                 !string.IsNullOrWhiteSpace(body.DaneOsoby.DataUrodzenia)))
            {
                sb.Append("<daneOsoby>");
                AppendIfValue(sb, "imiePierwsze", body.DaneOsoby.ImiePierwsze);
                AppendIfValue(sb, "nazwisko", body.DaneOsoby.Nazwisko);
                AppendIfValue(sb, "dataUrodzenia", body.DaneOsoby.DataUrodzenia); // yyyy-MM-dd
                sb.Append("</daneOsoby>");
            }

            AppendIfValue(sb, "osobaId", body.OsobaId);
            AppendIfValue(sb, "idk", body.Idk);

            sb.Append("</upr:DaneDokumentuRequest>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");

            return sb.ToString();
        }

        private static string X(string? value) =>
            SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;

        private static void AppendIfValue(StringBuilder sb, string tag, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            sb.Append('<').Append(tag).Append('>')
              .Append(X(value))
              .Append("</").Append(tag).Append('>');
        }
    }
}
