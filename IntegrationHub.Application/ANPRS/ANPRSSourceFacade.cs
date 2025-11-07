using IntegrationHub.Application.Mappers.ANPRS;
using IntegrationHub.Sources.ANPRS.Contracts;
using IntegrationHub.Sources.ANPRS.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationHub.Application.ANPRS
{

    public sealed class EventPhotosResult
    {
        public required IEnumerable<VehiclePhotoRowDto> Photos { get; init; }
        public required string PhotosComplete { get; init; } // "yes" | "no" | "Brak danych"
        public int Version { get; init; }
    }


    /// <summary>
    /// Facade delegating to the concrete ANPRSSourceService.
    /// Replace stubs below with exact public signatures from IntegrationHub.Sources.ANPRS.Services.ANPRSSourceService.
    /// The facade should expose only those methods.
    /// </summary>
    public class ANPRSSourceFacade : IANPRSSourceFacade
    {
        private readonly IANPRSSourceService _service;
        private readonly ILogger<ANPRSSourceFacade>? _logger;

        public ANPRSSourceFacade(IANPRSSourceService service, ILogger<ANPRSSourceFacade>? logger = null)
        {
            _service = service;
            _logger = logger;
        }

        // EXAMPLE: replace this with the real method(s) from ANPRSSourceService
        // Example shows mapping PhotoResponse -> VehiclePhotosDto using VehiclePhotosMapper.
        public async Task<IEnumerable<VehiclePhotoRowDto>> GetPhotosAsync(Guid id, CancellationToken ct = default)
        {
            var raw = await _service.GetPhotosAsync(id, ct);
            var dto = VehiclePhotosMapper.Map(raw.Data);
            _logger?.LogDebug("X-Photo-Version={Version}; photos={Count}", raw.Version, dto.Count());
            return dto;
        }

        public async Task<(IReadOnlyList<VehiclePhotoRowDto> Photos, string PhotosComplete)> GetPhotosWithMetaAsync(Guid id, CancellationToken ct = default)
        {
            var (raw, _, complete) = await _service.GetPhotosAsync(id, ct);
            var photos = VehiclePhotosMapper.Map(raw).ToList();
            return (photos, string.IsNullOrWhiteSpace(complete) ? "Brak danych" : complete);
        }
        // TODO: Add the remaining methods — exactly matching ANPRSSourceService public signatures.
        // For each method: delegate call to _service and apply mapping/translation if needed.
        //
        // Example pattern:
        // public Task<SomeResponse?> SomeMethodAsync(string param, CancellationToken ct = default)
        //     => _service.SomeMethodAsync(param, ct);
    }
}