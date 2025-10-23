//csharp IntegrationHub.Domain\Anprs\IAnprsDictionaryRepository.cs
using IntegrationHub.Domain.Contracts.ANPRS;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationHub.Domain.Interfaces.ANPRS;

/// <summary>
/// Abstrakcja repozytorium pracuj¹ca na domenowych DTO.
/// Implementacje (Infrastructure) zale¿¹ od Domain, nie odwrotnie.
/// </summary>
public interface IANPRSDictionaryRepository
{
    Task UpsertCountriesAsync(IEnumerable<string> countryCodes, CancellationToken ct = default);
    Task UpsertBcpAsync(IEnumerable<BcpRowDto> rows, CancellationToken ct = default);
    Task ReloadSystemsAsync(string countryCode, IEnumerable<SystemRowDto> rows, CancellationToken ct = default);
    Task<IEnumerable<SystemRowDto>> GetSystemsByCountryAsync(string countryCode, CancellationToken ct = default);

    /// <summary>
    /// Odczyt listy kodów krajów z lokalnej bazy (domenowy typ: string).
    /// </summary>
    Task<IEnumerable<string>> GetCountryCodesAsync(CancellationToken ct = default);
    Task<IEnumerable<BcpRowDto>> GetBcpAsync(CancellationToken ct = default);
}