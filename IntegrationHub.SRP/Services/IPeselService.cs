using IntegrationHub.Common.Primitives;
using IntegrationHub.SRP.Contracts;

namespace IntegrationHub.SRP.Services
{
    public interface IPeselService
    {
        Task<Result<SearchPersonResponse, Error>> SearchPersonAsync(SearchPersonRequest body, string? requestId = null, CancellationToken ct = default);
        Task<Result<GetPersonByPeselResponse, Error>> GetPersonByPeselAsync(GetPersonByPeselRequest body, string? requestId = null, CancellationToken ct = default);
    }
}
