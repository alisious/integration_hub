using System.Threading;
using System.Threading.Tasks;
using IntegrationHub.Common.Primitives;
using IntegrationHub.Sources.KSIP.Contracts;

namespace IntegrationHub.Sources.KSIP.Services
{
    public interface IKSIPService
    {
        Task<Result<SprawdzenieOsobyResponse, Error>> SprawdzenieOsobyWRuchuDrogowymAsync(
            SprawdzenieOsobyRequest body,
            string requestId,
            CancellationToken ct = default);
    }
}
