//csharp IntegrationHub.Application.ANPRS\ANPRSReportsFacade.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntegrationHub.Application.Mappers.ANPRS;
using IntegrationHub.Domain.Contracts.ANPRS;
using IntegrationHub.Sources.ANPRS.Services;
using Microsoft.Extensions.Logging;

namespace IntegrationHub.Application.ANPRS
{
    public sealed class ANPRSReportsFacade : IANPRSReportsFacade
    {
        private readonly IANPRSReportsService _source;
        private readonly ILogger<ANPRSReportsFacade> _logger;

        public ANPRSReportsFacade(IANPRSReportsService source, ILogger<ANPRSReportsFacade> logger)
        {
            _source = source;
            _logger = logger;
        }

        public async Task<IEnumerable<VehicleInPointDto>> GetVehiclesInPointAsync(
            string country,
            string system,
            string bcp,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken ct = default)
        {
            var resp = await _source.GetVehiclesInPointWithGeoAsync(country, system, bcp, dateFrom, dateTo, ct);
            if (resp is null) return Enumerable.Empty<VehicleInPointDto>();

            var list = VehicleInPointMapper.Map(resp).ToList();
            return list;
        }

        public async Task<IEnumerable<LicenseplateDto>> GetLicensePlateWithGeoAsync(
          string numberPlate,
          DateTime dateFrom,
          DateTime dateTo,
          CancellationToken ct = default)
        {
            var resp = await _source.GetLicensePlateWithGeoAsync(numberPlate, dateFrom, dateTo, ct);
            if (resp is null) return Enumerable.Empty<LicenseplateDto>();

            var list = LicenseplateMapper.Map(resp).ToList();
            return list;
        }
    }
}