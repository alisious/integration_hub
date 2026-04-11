// IntegrationHub.Sources.CEP.Udostepnianie.Mappers/XmlLinqSafe.cs
using System.Globalization;
using System.Xml.Linq;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Mappers
{
    internal static class XmlLinqSafe
    {
        public static XElement? ElementAnyNs(this XElement parent, string local) =>
            parent.Elements().FirstOrDefault(e => e.Name.LocalName == local);

        public static IEnumerable<XElement> ElementsAnyNs(this XElement parent, string local) =>
            parent.Elements().Where(e => e.Name.LocalName == local);

        public static XElement? Desc(this XElement parent, string local) => parent.ElementAnyNs(local);
        public static IEnumerable<XElement> DescMany(this XElement parent, string local) => parent.ElementsAnyNs(local);

        public static string? ValueOf(this XElement? parent, string local) =>
            parent?.ElementsAnyNs(local).FirstOrDefault()?.Value;

        public static int? ToInt(this string? s) =>
            int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : null;

        public static decimal? ToDecimal(this string? s) =>
            decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;

        public static bool? ToBool(this string? s)
        {
            if (s == null) return null;
            if (bool.TryParse(s, out var b)) return b;
            if (s == "1" || s.Equals("T", StringComparison.OrdinalIgnoreCase) || s.Equals("TAK", StringComparison.OrdinalIgnoreCase)) return true;
            if (s == "0" || s.Equals("N", StringComparison.OrdinalIgnoreCase) || s.Equals("NIE", StringComparison.OrdinalIgnoreCase)) return false;
            return null;
        }
    }
}
