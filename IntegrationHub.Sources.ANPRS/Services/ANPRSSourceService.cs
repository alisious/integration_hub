using System;
using System.Threading;
using System.Threading.Tasks;
using IntegrationHub.Sources.ANPRS.Client;
using IntegrationHub.Sources.ANPRS.Config;
using IntegrationHub.Sources.ANPRS.Contracts;

namespace IntegrationHub.Sources.ANPRS.Services
{
    public interface IANPRSSourceService
    {
        Task<EventContentResponse?> GetEventAsync(Guid id, int version = 2, CancellationToken ct = default);
        Task<PhotoResponse?> GetPhotosAsync(Guid id, int version = 2, CancellationToken ct = default);
    }

    public sealed class ANPRSSourceService : IANPRSSourceService
    {
        private readonly ANPRSHttpClient _client;
        private readonly ANPRSConfig _cfg;

        public ANPRSSourceService(ANPRSHttpClient client, ANPRSConfig cfg)
        {
            _client = client;
            _cfg = cfg;
        }

        public Task<EventContentResponse?> GetEventAsync(Guid id, int version = 2, CancellationToken ct = default)
        {
            var url = $"{_cfg.SourceServiceUrl}/Event/{id}";
            return _client.GetAsync<EventContentResponse>(url, ct, ("X-Event-Version", version.ToString()));
        }

        public Task<PhotoResponse?> GetPhotosAsync(Guid id, int version = 2, CancellationToken ct = default)
        {
            var url = $"{_cfg.SourceServiceUrl}/Photos/{id}";
            return _client.GetAsync<PhotoResponse>(url, ct, ("X-Photo-Version", version.ToString()));
        }
    }
}
