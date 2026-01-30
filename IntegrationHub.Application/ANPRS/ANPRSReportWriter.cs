using IntegrationHub.Domain.Contracts.ANPRS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace IntegrationHub.Application.ANPRS
{
    public sealed class ANPRSReportWriter : IANPRSReportWriter
    {
        private readonly ILogger<ANPRSReportWriter> _log;
        private readonly IHostEnvironment _env;
        private readonly IConfiguration _cfg;

        public ANPRSReportWriter(
            ILogger<ANPRSReportWriter> log,
            IHostEnvironment env,
            IConfiguration cfg)
        {
            _log = log;
            _env = env;
            _cfg = cfg;
        }

        public async Task<ReportFileLink> WriteVehiclesInPointAsync(
            string country, string system, string bcp,
            double? latitude, double? longitude,
            DateTime dateFrom, DateTime dateTo,
            string? userName, string? unitName,
            IEnumerable<VehicleInPointDto> events,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var ts = now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

            // Fallback: najpierw próbujemy "ANPRS:Reports", jeśli brak to "ExternalServices:ANPRS:Reports"
            string? relFolder =
                _cfg["ANPRS:Reports:ReportsFolderRelativePath"]
                ?? _cfg["ExternalServices:ANPRS:Reports:ReportsFolderRelativePath"]
                ?? "anprs_reports";

            var absFolder = Path.Combine(_env.ContentRootPath, relFolder);
            Directory.CreateDirectory(absFolder);

            var baseName = $"vehicles-in-point-bcp-{ts}";
            var htmlPath = Path.Combine(absFolder, $"{baseName}.html");

            var html = BuildHtml(country, system, bcp, latitude, longitude, dateFrom.ToString("yyyy-MM-dd HH:mm:ss"), dateTo.ToString("yyyy-MM-dd HH:mm:ss"), now, userName, unitName, events);
            await File.WriteAllTextAsync(htmlPath, html, Encoding.UTF8, ct);

            return new ReportFileLink
            {
                FileName = $"{baseName}.html",
                Url = $"/ANPRS/reports/files/{baseName}.html"
            };
        }

        public async Task<ReportFileLink> WriteLicensePlateAsync(
            string numberPlate,
            DateTime dateFrom, DateTime dateTo,
            string? userName, string? unitName,
            IEnumerable<LicenseplateDto> events,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var ts = now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

            string? relFolder =
                _cfg["ANPRS:Reports:ReportsFolderRelativePath"]
                ?? _cfg["ExternalServices:ANPRS:Reports:ReportsFolderRelativePath"]
                ?? "anprs_reports";

            var absFolder = Path.Combine(_env.ContentRootPath, relFolder);
            Directory.CreateDirectory(absFolder);

            var baseName = $"license-plate-{ts}";
            var htmlPath = Path.Combine(absFolder, $"{baseName}.html");

            var html = BuildLicensePlateHtml(numberPlate, dateFrom, dateTo, now, userName, unitName, events);
            await File.WriteAllTextAsync(htmlPath, html, Encoding.UTF8, ct);

            return new ReportFileLink
            {
                FileName = $"{baseName}.html",
                Url = $"/ANPRS/reports/files/{baseName}.html"
            };
        }

        // HTML raportu wg numeru rejestracyjnego (wzór: ZPO – Autor, Zakres dat, zdarzenia ze zdjęciami).
        private static string BuildLicensePlateHtml(
            string numberPlate,
            DateTime dateFrom, DateTime dateTo, DateTime now,
            string? userName, string? unitName,
            IEnumerable<LicenseplateDto> events)
        {
            var list = events.ToList();
            var count = list.Count;
            var statusText = count == 0
                ? "Brak zdarzeń w ANPRS wg. zadanych kryteriów."
                : $"Pobrano dane zdarzeń z ANPRS. Liczba zdarzeń: {count}.";

            var dateFromStr = dateFrom.ToString("yyyy-MM-dd");
            var dateToStr = dateTo.ToString("yyyy-MM-dd");
            var nowStr = now.ToString("yyyy-MM-dd HH:mm");

            var sb = new StringBuilder();
            sb.Append("""
                
            <!DOCTYPE html>
            <html lang="pl">
            <head>
            <meta charset="UTF-8">
            <title>ANPRS: Raport zdarzeń dotyczących pojazdu</title>
            <style>
            body { font-family: Arial, sans-serif; margin: 40px; }
            .report-header table { width: 100%; margin-bottom: 40px; }
            .report-header td { padding: 6px 8px; }
            .event { page-break-after: always; margin-bottom: 60px; }
            .event-details table { width: 100%; border-collapse: collapse; }
            .event-details td { padding: 4px 8px; }
            .event-images img { display: block; margin-top: 12px; margin-bottom: 12px; width: 793px; max-width: 100%; height: auto; }
            @media print { @page { size: A4 portrait; margin: 15mm; } }
            </style>
            </head>
            <body>
            <h1>ANPRS: Raport zdarzeń dotyczących pojazdu</h1>
            <div class="report-header">
            <table>
            <tr><td><strong>Autor:</strong> 
            """);
            sb.Append(E(userName ?? "")).Append("</td><td></td></tr>\n");
            sb.Append("<tr><td><strong>Data raportu:</strong> ").Append(E(nowStr)).Append("</td><td></td></tr>\n");
            sb.Append("<tr><td><strong>Jednostka organizacyjna:</strong> ").Append(E(unitName ?? "")).Append("</td><td></td></tr>\n");
            sb.Append("<tr><td><strong>Numer rejestracyjny:</strong> ").Append(E(numberPlate)).Append("</td><td></td></tr>\n");
            // Typ pojazdu i Kraj pojazdu z pierwszego zdarzenia (jeśli jest)
            var first = list.FirstOrDefault();
            sb.Append("<tr><td><strong>Typ pojazdu:</strong> ").Append(E(first?.VehicleType ?? "")).Append("</td><td></td></tr>\n");
            sb.Append("<tr><td><strong>Kraj pojazdu:</strong> ").Append(E(first?.Country ?? "")).Append("</td><td></td></tr>\n");
            sb.Append("<tr><td><strong>Zakres dat:</strong> ").Append(E(dateFromStr)).Append(" – ").Append(E(dateToStr)).Append("</td><td></td></tr>\n");
            sb.Append("<tr><td><strong>Status raportu:</strong> ").Append(E(statusText)).Append("</td><td></td></tr>\n");
            sb.Append("</table>\n</div>\n");

            foreach (var ev in list)
            {
                sb.Append("<div class=\"event\">\n<div class=\"event-details\">\n<table>\n");
                sb.Append("<tr><td><strong>Lokalizacja:</strong> ").Append(E(ev.Location)).Append("</td><td></td></tr>\n");
                sb.Append("<tr><td><strong>Data zdarzenia:</strong> ").Append(E(ev.EventDate.ToString("yyyy-MM-dd HH:mm:ss"))).Append("</td><td></td></tr>\n");
                sb.Append("<tr><td><strong>Kraj BCP:</strong> ").Append(E(ev.BcpCountry)).Append("</td><td></td></tr>\n");
                sb.Append("<tr><td><strong>BCP:</strong> ").Append(E(ev.Bcp)).Append("</td><td></td></tr>\n");
                sb.Append("<tr><td><strong>Latitude:</strong> ").Append(ev.Latitude.ToString(CultureInfo.InvariantCulture)).Append("</td><td></td></tr>\n");
                sb.Append("<tr><td><strong>Longitude:</strong> ").Append(ev.Longitude.ToString(CultureInfo.InvariantCulture)).Append("</td><td></td></tr>\n");
                sb.Append("</table>\n</div>\n");
                if (ev.Photos != null && ev.Photos.Count > 0)
                {
                    sb.Append("<div class=\"event-images\">\n");
                    foreach (var p in ev.Photos)
                    {
                        if (!string.IsNullOrWhiteSpace(p.Zdjecie))
                            sb.Append("<img src=\"data:image/jpeg;base64,").Append(p.Zdjecie).Append("\" />\n");
                    }
                    sb.Append("</div>\n");
                }
                sb.Append("</div>\n");
            }

            sb.Append("</body>\n</html>");
            return sb.ToString();

            static string E(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
        }

        // HTML: szerokość "strony" 210 mm (A4), z wewnętrznym paddingiem ~15 mm.
        private static string BuildHtml(
            string country, string system, string bcp,
            double? latitude, double? longitude,
            string dateFrom, string dateTo, DateTime now,
            string? userName, string? unitName,
            IEnumerable<VehicleInPointDto> events)
        {
            var sb = new StringBuilder();
            sb.Append("""
            <!DOCTYPE html>
            <html lang="pl">
            <head>
            <meta charset="utf-8" />
            <title>ANPRS - Zdarzenia w punkcie</title>
            <style>
            :root { --gap: 8px; --badge-bg: #eee; --badge: #444; }
            /* Kontener udający stronę A4 na ekranie */
            body { background:#f4f4f4; margin:0; padding:16px; }
            .page { width:210mm; margin: 0 auto; background:#fff; padding: 15mm; box-sizing: border-box; }
            h1 { margin: 0 0 16px; }
            .section { margin-bottom: 16px; }
            .meta table { border-collapse: collapse; }
            .meta td { padding: 2px 8px 2px 0; vertical-align: top; }
            .event { border: 1px solid #ddd; padding: 12px; margin-bottom: 12px; page-break-inside: avoid; }
            .event-header { display: grid; grid-template-columns: repeat(5, 1fr); gap: var(--gap); font-size: 14px; }
            .badge { background: var(--badge-bg); color: var(--badge); padding: 2px 6px; border-radius: 4px; display: inline-block; margin-left: 8px; font-size: 12px; }
            .photos { display: grid; grid-template-columns: 1fr; gap: var(--gap); margin-top: 8px; }
            .photos img { width: 100%; height: auto; }

            /* Druk: A4 portrait, te same marginesy co w kontenerze */
            @media print {
                @page { size: A4 portrait; margin: 15mm; }
                body { background:#fff; margin: 0; padding: 0; }
                .page { width:auto; margin:0; padding:0; }
            }
            </style>
            </head>
            <body>
            <div class="page">
            <h1>ANPRS - Zdarzenia w punkcie.</h1>
            <div class="section meta">
            <table>
            <tr><td><strong>Kraj:</strong></td><td>
            """);

            sb.Append(E(country)).Append("</td></tr><tr><td><strong>System:</strong></td><td>")
              .Append(E(system)).Append("</td></tr><tr><td><strong>BCP:</strong></td><td>")
              .Append(E(bcp)).Append("</td></tr><tr><td><strong>Współrzędne punktu:</strong></td><td>latitude: ")
              .Append(latitude?.ToString(CultureInfo.InvariantCulture) ?? "")
              .Append(", longitude: ")
              .Append(longitude?.ToString(CultureInfo.InvariantCulture) ?? "")
              .Append("</td></tr><tr><td><strong>Okres raportu:</strong></td><td>")
              .Append(E(dateFrom)).Append(" - ").Append(E(dateTo))
              .Append("</td></tr><tr><td><strong>Data bieżąca:</strong></td><td>")
              .Append(now.ToString("yyyy-MM-dd HH:mm:ss"))
              .Append("</td></tr><tr><td><strong>Nazwa użytkownika:</strong></td><td>")
              .Append(E(userName ?? ""))
              .Append("</td></tr><tr><td><strong>Jednostka organizacyjna:</strong></td><td>")
              .Append(E(unitName ?? ""))
              .Append("</td></tr><tr><td><strong>Liczba zdarzeń spełniających zadane kryteria:</strong></td><td>")
              .Append(events.Count())
              .Append(".</td></tr></table></div>");

            foreach (var ev in events)
            {
                sb.Append("<div class=\"event\"><div class=\"event-header\">");
                sb.Append("<div><strong>Numer rej.:</strong> ").Append(E(Get(ev, "Number"))).Append("</div>");
                sb.Append("<div><strong>Typ pojazdu:</strong> ").Append(E(Get(ev, "VehicleType"))).Append("</div>");
                sb.Append("<div><strong>Lok. numeru:</strong> ").Append(E(Get(ev, "Location"))).Append("</div>");
                sb.Append("<div><strong>Kraj poj.:</strong> ").Append(E(Get(ev, "Country"))).Append("</div>");
                sb.Append("<div><strong>Data zdarzenia:</strong> ").Append(E(Get(ev, "EventDate"))).Append("</div>");
                sb.Append("</div>");
                sb.Append("<div style=\"margin-top:4px\"><span class=\"badge\">PhotosComplete: ")
                  .Append(E(ev.PhotosComplete ?? "Brak danych")).Append("</span></div>");

                if (ev.Photos != null && ev.Photos.Count > 0)
                {
                    sb.Append("<div class=\"photos\">");
                    foreach (var p in ev.Photos)
                    {
                        //var base64 = Get(p, "ContentBase64");
                        if (!string.IsNullOrWhiteSpace(p.Zdjecie))
                            sb.Append("<img src=\"data:image/jpeg;base64,").Append(p.Zdjecie).Append("\" />");
                    }
                    sb.Append("</div>");
                }

                sb.Append("</div>");
            }

            sb.Append("</div></body></html>");
            return sb.ToString();

            static string E(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
            static string Get(object o, string prop)
                => o.GetType().GetProperty(prop)?.GetValue(o)?.ToString() ?? "";
        }
    }
}
