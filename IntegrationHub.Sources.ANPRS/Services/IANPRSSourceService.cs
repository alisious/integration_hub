
using IntegrationHub.Sources.ANPRS.Contracts;

namespace IntegrationHub.Sources.ANPRS.Services
{
    public interface IANPRSSourceService
    {
        Task<EventContentResponse?> GetEventAsync(Guid id, int version = 2, CancellationToken ct = default);
        Task<PhotoResponse?> GetPhotosAsync(Guid id, int version = 2, CancellationToken ct = default);
    }
}
