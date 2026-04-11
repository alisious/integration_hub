using IntegrationHub.Domain.Contracts.ANPRS;

namespace IntegrationHub.Application.ANPRS
{
    public interface IANPRSReportWriter
    {
        Task<ReportFileLink> WriteVehiclesInPointAsync(
            string country, string system, string bcp,
            double? latitude, double? longitude,
            DateTime dateFrom, DateTime dateTo,
            string? userName, string? unitName,
            IEnumerable<VehicleInPointDto> events,
            CancellationToken ct);

        Task<ReportFileLink> WriteLicensePlateAsync(
            string numberPlate,
            DateTime dateFrom, DateTime dateTo,
            string? userName, string? unitName,
            IEnumerable<LicenseplateDto> events,
            CancellationToken ct);
    }
}
