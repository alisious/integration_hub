using IntegrationHub.Common.Primitives;
using IntegrationHub.Sources.SRP.Contracts;

namespace IntegrationHub.Sources.SRP.Services
{
    public interface IPeselService
    {
        Task<Result<SearchPersonResponse, Error>> SearchPersonAsync(SearchPersonRequest body, string? requestId = null, CancellationToken ct = default);
        Task<Result<GetPersonByPeselResponse, Error>> GetPersonByPeselAsync(GetPersonByPeselRequest body, string? requestId = null, CancellationToken ct = default);
    }
}
