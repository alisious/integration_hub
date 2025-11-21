using IntegrationHub.Common.Primitives;
using IntegrationHub.Sources.ZW.Contracts;

namespace IntegrationHub.Sources.ZW.Interfaces
{
    public interface IZandWantedPersonService
    {
        Task<Result<IReadOnlyList<ZandWantedPersonDto>, Error>>GetByPeselAsync(string pesel, CancellationToken ct = default);
        Task<Result<IReadOnlyList<BronOsobaResponse>, Error>> GetBronOsobaByPeselAsync(
            BronOsobaRequest request,
            CancellationToken ct = default);
        Task<Result<OsobaZolnierzResponse, Error>> GetOsobaZolnierzByPeselAsync(
            OsobaZolnierzRequest request,
            CancellationToken ct = default);

    }
}
