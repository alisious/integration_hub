// IntegrationHub.Sources.CEP.Udostepnianie.Mappers/FaultResponseToXmlMapper.cs
using System.Xml.Linq;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Mappers
{
    /// <summary>
    /// Parser odpowiedzi SOAP → FaultResponse. Zwraca null, gdy w treści brak węzła &lt;Fault&gt;.
    /// </summary>
    public static class FaultResponseXmlMapper
    {
        private static readonly XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";

        public static FaultResponse? ParseOrNull(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) return null;

            XDocument doc;
            try { doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo); }
            catch { return null; } // niepoprawny XML → nie rozpoznajemy Fault

            var body = doc.Root?.Element(soap + "Body");
            var fault = body?.Elements().FirstOrDefault(e => e.Name.LocalName == "Fault");
            if (fault is null) return null;

            var fr = new FaultResponse
            {
                FaultCode = fault.Elements().FirstOrDefault(e => e.Name.LocalName == "faultcode")?.Value?.Trim(),
                FaultString = fault.Elements().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value?.Trim()
            };

            // detail -> exc:cepikException -> komunikaty -> (typ, kod, komunikat, szczegoly)
            var detail = fault.ElementAnyNs("detail");
            var komunikaty = detail?.Desc("cepikException")?.Desc("komunikaty")
                             ?? detail?.Desc("komunikaty"); // fallback gdyby różniło się zagnieżdżenie

            fr.Typ = komunikaty.ValueOf("typ");
            fr.Kod = komunikaty.ValueOf("kod");
            fr.Komunikat = komunikaty.ValueOf("komunikat");
            fr.Szczegoly = komunikaty.ValueOf("szczegoly");

            return fr;
        }
    }
}

