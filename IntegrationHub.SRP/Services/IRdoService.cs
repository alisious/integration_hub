using IntegrationHub.Common.Primitives;
using IntegrationHub.SRP.Contracts;

namespace IntegrationHub.SRP.Services
{
    public interface IRdoService
    {
        Task<Result<GetCurrentPhotoResponse, Error>> GetCurrentPhotoAsync(GetCurrentPhotoRequest body, string? requestId = null, CancellationToken ct = default);
        Task<Result<GetCurrentIdByPeselResponse, Error>> GetCurrentIdByPeselAsync(GetCurrentIdByPeselRequest body, string? requestId = null, CancellationToken ct = default);
    }
}
