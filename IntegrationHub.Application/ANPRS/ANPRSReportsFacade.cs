//csharp IntegrationHub.Application.ANPRS\ANPRSReportsFacade.cs
using IntegrationHub.Application.Mappers.ANPRS;
using IntegrationHub.Domain.Contracts.ANPRS;
using IntegrationHub.Sources.ANPRS.Contracts;
using IntegrationHub.Sources.ANPRS.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationHub.Application.ANPRS
{
    public sealed class ANPRSReportsFacade : IANPRSReportsFacade
    {
        private readonly IANPRSReportsService _reportsService;
        private readonly IANPRSSourceFacade _sourceFacade;
        private readonly ILogger<ANPRSReportsFacade> _logger;
        private readonly IANPRSReportWriter _writer;
        private readonly IConfiguration _cfg;

        public ANPRSReportsFacade(IANPRSReportsService reportsService, IANPRSSourceFacade sourceFacade, IANPRSReportWriter writer, ILogger<ANPRSReportsFacade> logger, IConfiguration cfg)
        {
            _reportsService = reportsService;
            _sourceFacade = sourceFacade;
            _logger = logger;
            _writer = writer;
            _cfg = cfg; 
        }

        public async Task<IEnumerable<VehicleInPointDto>> GetVehiclesInPointAsync(
            string country,
            string system,
            string bcpId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken ct = default)
        {
            var resp = await _reportsService.GetVehiclesInPointWithGeoAsync(country, system, bcpId, dateFrom, dateTo, ct);
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
            var resp = await _reportsService.GetLicensePlateWithGeoAsync(numberPlate, dateFrom, dateTo, ct);
            if (resp is null) return Enumerable.Empty<LicenseplateDto>();

            var list = LicenseplateMapper.Map(resp).ToList();
            return list;
        }

        public async Task<ReportFileLink> GenerateVehiclesInPointReportWithPhotosAsync(
            string country, string system, string bcpId,
            DateTime dateFrom, DateTime dateTo,
            string? userName, string? unitName,
            CancellationToken ct)
        {
            // 1) U¯YJ ISTNIEJ¥CEGO MAPOWANIA (Twoja metoda w TEJ facadzie)
            var list = (await GetVehiclesInPointAsync(country, system, bcpId, dateFrom, dateTo, ct))
                       .ToList();

            // 2) Doci¹ganie zdjêæ i meta (Complete)
            int maxParallel = _cfg.GetValue<int?>("ExternalServices:ANPRS:Reports:MaxParallelPhotoFetch") ?? 8;
            int maxPhotosPerEvent = _cfg.GetValue<int?>("ExternalServices:ANPRS:Reports:MaxPhotosPerEvent") ?? 4;

            var throttler = new SemaphoreSlim(Math.Max(1, maxParallel));
            var tasks = list.Select(async ev =>
            {
                await throttler.WaitAsync(ct);
                try
                {
                    var meta = await _sourceFacade.GetPhotosWithMetaAsync(ev.Id, ct);
                    var photos = meta.Photos.Take(Math.Max(0, maxPhotosPerEvent)).ToList();
                    ev.Photos = photos;
                    ev.PhotosComplete = meta.PhotosComplete;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Photo fetch failed for event {Id}", ev.Id);
                    ev.Photos = new List<VehiclePhotoRowDto>();
                    ev.PhotosComplete = "Brak danych";
                }
                finally { throttler.Release(); }
            });
            await Task.WhenAll(tasks);

            ///ToDO: latitude, longitude  
            ///Pobraæ z pierwszego elementu listy, jeœli istnieje
            double? latitude = list.FirstOrDefault()?.Latitude;
            double? longitude = list.FirstOrDefault()?.Longitude;

            ///Docelowo z lokalnego repozytorium
            ///Dopisaæ metodê do pobierania lokalnego BCP na podstawie country, system,bcpId    


            // 3) Zapisz HTML (A4 width) i zwróæ link
            var link = await _writer.WriteVehiclesInPointAsync(
                country, system, bcpId, latitude, longitude,
                dateFrom, dateTo,
                userName, unitName,
                list,
                ct);

            _logger.LogInformation("ANPRS report generated (HTML): {File}", link.FileName);
            return link;
        }

    }
}