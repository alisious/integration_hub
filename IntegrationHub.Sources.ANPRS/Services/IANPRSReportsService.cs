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
}
