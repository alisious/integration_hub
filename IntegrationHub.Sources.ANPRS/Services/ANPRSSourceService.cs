// IntegrationHub.Sources.ANPRS/Services/ANPRSSourceService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using IntegrationHub.Sources.ANPRS.Client;
using IntegrationHub.Sources.ANPRS.Config;
using IntegrationHub.Sources.ANPRS.Contracts;
using IntegrationHub.Infrastructure.Audit;

namespace IntegrationHub.Sources.ANPRS.Services
{
    public sealed class ANPRSSourceService : IANPRSSourceService
    {
        private const string Source = "ANPRS";
        private readonly ANPRSHttpClient _client;
        private readonly ANPRSConfig _cfg;
        private readonly ISourceCallAuditor _auditor;

        public ANPRSSourceService(ANPRSHttpClient client, ANPRSConfig cfg, ISourceCallAuditor auditor)
        {
            _client = client;
            _cfg = cfg;
            _auditor = auditor;
        }

        public Task<EventContentResponse?> GetEventAsync(Guid id, int version = 2, CancellationToken ct = default)
        {
            var url = $"{_cfg.SourceServiceUrl}/Event/{id}";
            return _auditor.InvokeAsync<EventContentResponse>(
                source: Source,
                endpointUrl: url,
                action: "GET /Source/Event",
                call: () => _client.GetAsync<EventContentResponse>(url, ct, ("X-Event-Version", version.ToString())),
                ct: ct,
                requestBody: null,
                addOutgoingHeader: id2 => _client.SetCorrelationIdHeader(id2));
        }

        public Task<PhotoResponse?> GetPhotosAsync(Guid id, int version = 2, CancellationToken ct = default)
        {
            var url = $"{_cfg.SourceServiceUrl}/Photos/{id}";
            return _auditor.InvokeAsync<PhotoResponse>(
                source: Source,
                endpointUrl: url,
                action: "GET /Source/Photos",
                call: () => _client.GetAsync<PhotoResponse>(url, ct, ("X-Photo-Version", version.ToString())),
                ct: ct,
                requestBody: null,
                addOutgoingHeader: id2 => _client.SetCorrelationIdHeader(id2));
        }
    }
}
