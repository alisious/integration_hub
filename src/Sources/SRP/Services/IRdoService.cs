using IntegrationHub.Common.Primitives;
using IntegrationHub.Sources.SRP.Contracts;


namespace IntegrationHub.Sources.SRP.Services
{
    public interface IRdoService
    {
        Task<Result<GetCurrentPhotoResponse, Error>> GetCurrentPhotoAsync(GetCurrentPhotoRequest body, string? requestId = null, CancellationToken ct = default);
        Task<Result<GetCurrentIdByPeselResponse, Error>> GetCurrentIdByPeselAsync(GetCurrentIdByPeselRequest body, string? requestId = null, CancellationToken ct = default);
    }
}
