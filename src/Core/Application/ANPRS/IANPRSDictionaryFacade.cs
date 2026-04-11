//csharp IntegrationHub.Application\Anprs\IAnprsDictionaryFacade.cs
using IntegrationHub.Domain.Contracts.ANPRS;


namespace IntegrationHub.Application.ANPRS;

public interface IANPRSDictionaryFacade
{
    /// <summary>
    /// Pobierz listę kodów krajów (domenowe stringi, np. "PLN").
    /// </summary>
    Task<IEnumerable<string>> GetCountriesAsync(CancellationToken ct = default);

    /// <summary>
    /// Pobierz listę BCP jako domenowe DTO (<see cref="BcpRowDto"/>).
    /// </summary>
    Task<IEnumerable<BcpRowDto>> GetBCPAsync(CancellationToken ct = default);

    /// <summary>
    /// Pobierz listę systemów dla kraju jako domenowe DTO (<see cref="SystemRowDto"/>).
    /// </summary>
    Task<IEnumerable<SystemRowDto>> GetSystemsAsync(string country, CancellationToken ct = default);

    /// <summary>Operacje zapisu wyzwalane ręcznie przez operatora.</summary>
    Task SaveCountriesToDbAsync(CancellationToken ct = default);
    Task SaveBcpToDbAsync(CancellationToken ct = default);
    Task SaveSystemsToDbAsync(string country, CancellationToken ct = default);

    /// <summary>
    /// Odczyt lokalny systemów z DB (domenowe DTO).
    /// </summary>
    Task<IEnumerable<SystemRowDto>> GetSystemsLocalAsync(string country, CancellationToken ct = default);

    /// <summary>
    /// Odczyt lokalny listy krajów z DB (domenowe stringi).
    /// </summary>
    Task<IEnumerable<string>> GetCountriesLocalAsync(CancellationToken ct = default);

    /// <summary>
    /// Odczyt lokalny listy BCP z DB (domenowe DTO).
    /// </summary>
    Task<IEnumerable<BcpRowDto>> GetBcpLocalAsync(CancellationToken ct = default);
}