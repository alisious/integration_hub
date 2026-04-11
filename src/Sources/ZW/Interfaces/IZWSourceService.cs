using IntegrationHub.Common.Primitives;
using IntegrationHub.Sources.ZW.Contracts;

namespace IntegrationHub.Sources.ZW.Interfaces;

public interface IZWSourceService
{
    Task<Result<IReadOnlyList<WantedPersonResponse>, Error>> GetWantedPersonsByPeselAsync(string pesel, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PrivateWeaponHolderResponse>, Error>> GetWeaponHolderByPeselAsync(
        PrivateWeaponHolderRequest request,
        CancellationToken ct = default);
    Task<Result<SoldierResponse, Error>> GetSoldierByPeselAsync(
        SoldierRequest request,
        CancellationToken ct = default);
    Task<Result<PrivateWeaponHolderResponse, Error>> GetWeaponHolderByAddressAsync(
        WeaponAddressSearchRequest request,
        CancellationToken ct = default);
}
