namespace IntegrationHub.Infrastructure.Cepik;

public sealed record DictionaryItemDto(string Kod, string WartoscOpisowa);

public interface ICepikDictionaryRepository
{
    Task<IReadOnlyList<DictionaryItemDto>> GetAsync(
        string idSlownika,
        DateTime? onDate = null,
        CancellationToken ct = default);
}
