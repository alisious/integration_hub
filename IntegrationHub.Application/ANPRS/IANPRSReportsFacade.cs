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
            string bcpId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken ct = default);

        Task<IEnumerable<LicenseplateDto>> GetLicensePlateWithGeoAsync(
            string numberPlate,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken ct = default);

        Task<ReportFileLink> GenerateVehiclesInPointReportWithPhotosAsync(
        string country, string system, string bcpId,
        DateTime dateFrom, DateTime dateTo,
        string? userName, string? unitName,
        CancellationToken ct);

        Task<ReportFileLink> GenerateLicensePlateReportWithPhotosAsync(
            string numberPlate,
            DateTime dateFrom, DateTime dateTo,
            string? userName, string? unitName,
            CancellationToken ct);
    }
}