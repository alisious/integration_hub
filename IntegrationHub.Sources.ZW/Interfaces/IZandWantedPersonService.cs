using IntegrationHub.Common.Primitives;
using IntegrationHub.Sources.ZW.Contracts;

namespace IntegrationHub.Sources.ZW.Interfaces
{
    public interface IZandWantedPersonService
    {
        Task<Result<IReadOnlyList<ZandWantedPersonDto>, Error>>GetByPeselAsync(string pesel, CancellationToken ct = default);
       

    }
}
