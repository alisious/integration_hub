//csharp IntegrationHub.Application.ANPRS\IANPRSReportsFacade.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IntegrationHub.Domain.Contracts.ANPRS;

namespace IntegrationHub.Application.ANPRS
{
    public interface IANPRSReportsFacade
    {
        Task<IEnumerable<VehicleInPointDto>> GetVehiclesInPointAsync(
            string country,
            string system,
            string bcp,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken ct = default);

        Task<IEnumerable<LicenseplateDto>> GetLicensePlateWithGeoAsync(
            string numberPlate,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken ct = default);
    }
}