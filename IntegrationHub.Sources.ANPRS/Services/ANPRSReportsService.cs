// IntegrationHub.Sources.ANPRS/Services/ANPRSReportsService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using IntegrationHub.Sources.ANPRS.Client;
using IntegrationHub.Sources.ANPRS.Config;
using IntegrationHub.Sources.ANPRS.Contracts;
using IntegrationHub.Infrastructure.Audit;

namespace IntegrationHub.Sources.ANPRS.Services
{
    public sealed class ANPRSReportsService : IANPRSReportsService
    {
        private const string Source = "ANPRS";
        private readonly ANPRSHttpClient _client;
        private readonly ANPRSConfig _cfg;
        private readonly ISourceCallAuditor _auditor;

        public ANPRSReportsService(ANPRSHttpClient client, ANPRSConfig cfg, ISourceCallAuditor auditor)
        {
            _client = client;
            _cfg = cfg;
            _auditor = auditor;
        }

        public Task<VehiclesInPointResponse?> GetVehiclesInPointWithGeoAsync(
            string country, string system, string bcp,
            DateTime dateFrom, DateTime dateTo,
            CancellationToken ct = default)
        {
            var url = $"{_cfg.ReportsServiceUrl}/VehiclesInPointWithGeo" +
                      $"?country={Uri.EscapeDataString(country)}" +
                      $"&system={Uri.EscapeDataString(system)}" +
                      $"&bcp={Uri.EscapeDataString(bcp)}" +
                      $"&dateFrom={dateFrom:yyyy-MM-dd HH:mm:ss}" +
                      $"&dateTo={dateTo:yyyy-MM-dd HH:mm:ss}";

            return _auditor.InvokeAsync<VehiclesInPointResponse>(
                source: Source,
                endpointUrl: url,
                action: "GET /Reports/VehiclesInPointWithGeo",
                call: () => _client.GetAsync<VehiclesInPointResponse>(url, ct),
                ct: ct,
                requestBody: null,
                addOutgoingHeader: id => _client.SetCorrelationIdHeader(id)
            );
        }

        public Task<LicensePlateReportResponse?> GetLicensePlateWithGeoAsync(
            string numberPlate, DateTime dateFrom, DateTime dateTo,
            CancellationToken ct = default)
        {
            var url = $"{_cfg.ReportsServiceUrl}/LicenseplateWithGeo" +
                      $"?numberPlate={Uri.EscapeDataString(numberPlate)}" +
                      $"&dateFrom={dateFrom:yyyy-MM-dd}" +
                      $"&dateTo={dateTo:yyyy-MM-dd}";

            return _auditor.InvokeAsync<LicensePlateReportResponse>(
                source: Source,
                endpointUrl: url,
                action: "GET /Reports/LicenseplateWithGeo",
                call: () => _client.GetAsync<LicensePlateReportResponse>(url, ct),
                ct: ct,
                requestBody: null,
                addOutgoingHeader: id => _client.SetCorrelationIdHeader(id)
            );
        }
    }
}
