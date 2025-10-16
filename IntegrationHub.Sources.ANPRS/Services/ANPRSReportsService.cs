using System;
using System.Threading;
using System.Threading.Tasks;
using IntegrationHub.Sources.ANPRS.Client;
using IntegrationHub.Sources.ANPRS.Config;
using IntegrationHub.Sources.ANPRS.Contracts;

namespace IntegrationHub.Sources.ANPRS.Services
{
    public interface IANPRSReportsService
    {
        Task<LicensePlateReportResponse?> GetLicensePlateWithGeoAsync(
            string numberPlate, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default);

        Task<VehiclesInPointResponse?> GetVehiclesInPointWithGeoAsync(
            string country, string system, string bcp, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default);
    }

    public sealed class ANPRSReportsService : IANPRSReportsService
    {
        private readonly ANPRSHttpClient _client;
        private readonly ANPRSConfig _cfg;

        public ANPRSReportsService(ANPRSHttpClient client, ANPRSConfig cfg)
        {
            _client = client;
            _cfg = cfg;
        }

        public Task<LicensePlateReportResponse?> GetLicensePlateWithGeoAsync(
            string numberPlate, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default)
        {
            var url =
                $"{_cfg.ReportsServiceUrl}/LicenseplateWithGeo" +
                $"?numberPlate={Uri.EscapeDataString(numberPlate)}" +
                $"&dateFrom={dateFrom:yyyy-MM-dd}" +
                $"&dateTo={dateTo:yyyy-MM-dd}";

            return _client.GetAsync<LicensePlateReportResponse>(url, ct);
        }

        public Task<VehiclesInPointResponse?> GetVehiclesInPointWithGeoAsync(
            string country, string system, string bcp, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default)
        {
            var url =
                $"{_cfg.ReportsServiceUrl}/VehiclesInPointWithGeo" +
                $"?country={Uri.EscapeDataString(country)}" +
                $"&system={Uri.EscapeDataString(system)}" +
                $"&bcp={Uri.EscapeDataString(bcp)}" +
                $"&dateFrom={dateFrom:yyyy-MM-dd HH:mm:ss}" +
                $"&dateTo={dateTo:yyyy-MM-dd HH:mm:ss}";

            return _client.GetAsync<VehiclesInPointResponse>(url, ct);
        }
    }
}
