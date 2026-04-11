using IntegrationHub.Common.Primitives;
using IntegrationHub.PIESP.Models;

namespace IntegrationHub.PIESP.Services;

/// <summary>
/// Serwis do pobierania słowników z bazy danych.
/// </summary>
public interface IDictService
{
    /// <summary>
    /// Pobiera wszystkie elementy słownika.
    /// </summary>
    Task<Result<IReadOnlyList<DictItem>, Error>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Pobiera wszystkie elementy dla danego słownika (po DI_DID).
    /// </summary>
    Task<Result<IReadOnlyList<DictItem>, Error>> GetByDictIdAsync(string dictId, CancellationToken ct = default);

    /// <summary>
    /// Pobiera element słownika po identyfikatorze (DI_ID).
    /// </summary>
    Task<Result<DictItem?, Error>> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Pobiera element słownika po kodzie i ID słownika (DI_CODE i DI_DID).
    /// </summary>
    Task<Result<DictItem?, Error>> GetByCodeAsync(string dictId, string code, CancellationToken ct = default);
}
