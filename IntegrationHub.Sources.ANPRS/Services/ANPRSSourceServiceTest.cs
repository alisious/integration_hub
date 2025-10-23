using System;
using System.Threading;
using System.Threading.Tasks;
using IntegrationHub.Sources.ANPRS.Client;
using IntegrationHub.Sources.ANPRS.Config;
using IntegrationHub.Sources.ANPRS.Contracts;

namespace IntegrationHub.Sources.ANPRS.Services
{

    public sealed class ANPRSSourceServiceTest : IANPRSSourceService
    {
        private readonly ANPRSConfig _cfg;

        public ANPRSSourceServiceTest( ANPRSConfig cfg)
        {
            
            _cfg = cfg;
        }

        public Task<EventContentResponse?> GetEventAsync(Guid id, int version = 2, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<PhotoResponse?> GetPhotosAsync(Guid id, int version = 2, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
