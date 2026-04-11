using Dapper;
using IntegrationHub.Infrastructure.Cepik;
using IntegrationHub.Infrastructure.Abstractions;
using System.Data;

namespace IntegrationHub.Infrastructure.Cepik;

public sealed class CepikDictionaryRepository : ICepikDictionaryRepository
{
    private readonly IDbConnectionFactory _factory;

    public CepikDictionaryRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<DictionaryItemDto>> GetAsync(
        string idSlownika, DateTime? onDate = null, CancellationToken ct = default)
    {
        const string sql = @"
                    SELECT s.Kod, s.WartoscOpisowa
                    FROM cepik.SLOWNIKI s
                    WHERE s.IdSlownika = @Id
                    AND (@OnDate IS NULL OR @OnDate BETWEEN ISNULL(s.DataOd,'00010101') AND ISNULL(s.DataDo,'99991231'))
                    ORDER BY s.WartoscOpisowa;";

        using var conn = _factory.Create();
        var rows = await conn.QueryAsync<DictionaryItemDto>(
            new CommandDefinition(sql, new { Id = idSlownika, OnDate = onDate }, cancellationToken: ct));
        return rows.AsList();
    }
}
