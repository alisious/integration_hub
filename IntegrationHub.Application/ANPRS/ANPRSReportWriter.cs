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
