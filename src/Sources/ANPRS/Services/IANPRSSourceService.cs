
using IntegrationHub.Sources.ANPRS.Contracts;

namespace IntegrationHub.Sources.ANPRS.Services
{
    public interface IANPRSSourceService
    {
        Task<EventContentResponse?> GetEventAsync(Guid id, int version = 2, CancellationToken ct = default);
        Task<(PhotoResponse? Data, int Version, string? Complete)> GetPhotosAsync(Guid id, CancellationToken ct = default);
    }
}
