using System;
using System.Threading;
using System.Threading.Tasks;
using IntegrationHub.Sources.ANPRS.Client;
using IntegrationHub.Sources.ANPRS.Config;
using IntegrationHub.Sources.ANPRS.Contracts;

namespace IntegrationHub.Sources.ANPRS.Services
{


    public sealed class ANPRSReportsServiceTest : IANPRSReportsService
    {
        private readonly ANPRSConfig _cfg;

        public ANPRSReportsServiceTest(ANPRSConfig cfg)
        {
           _cfg = cfg;
        }

        public Task<LicensePlateReportResponse?> GetLicensePlateWithGeoAsync(
            string numberPlate, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<VehiclesInPointResponse?> GetVehiclesInPointWithGeoAsync(
            string country, string system, string bcp, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
