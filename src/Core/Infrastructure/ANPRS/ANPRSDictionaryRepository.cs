// IntegrationHub.Infrastructure.Anprs/AnprsDictionaryRepository.cs
using System.Data;
using System.Linq;
using Dapper;
using Microsoft.Data.SqlClient;
using IntegrationHub.Infrastructure.Abstractions;
using IntegrationHub.Domain.Contracts.ANPRS;
using IntegrationHub.Domain.Interfaces.ANPRS;

namespace IntegrationHub.Infrastructure.ANPRS;

public sealed class ANPRSDictionaryRepository : IANPRSDictionaryRepository
{
    private readonly IDbConnectionFactory _factory;
    public ANPRSDictionaryRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task UpsertCountriesAsync(IEnumerable<string> countryCodes, CancellationToken ct = default)
    {
        using var conn = _factory.Create();
        var tvp = BuildCountriesTvp(countryCodes);

        var p = new DynamicParameters();
        p.Add("@Rows", tvp.AsTableValuedParameter("anprs.CountryImportTT"));
        p.Add("@Source", "ANPRS");

        await conn.ExecuteAsync(
            new CommandDefinition("anprs.UpsertCountries", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task UpsertBcpAsync(IEnumerable<BcpRowDto> rows, CancellationToken ct = default)
    {
        using var conn = _factory.Create();
        var tvp = BuildBcpTvp(rows);

        var p = new DynamicParameters();
        p.Add("@Rows", tvp.AsTableValuedParameter("anprs.BcpImportTT"));
        p.Add("@Source", "ANPRS");

        await conn.ExecuteAsync(
            new CommandDefinition("anprs.RebuildBcp", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task ReloadSystemsAsync(string countryCode, IEnumerable<SystemRowDto> rows, CancellationToken ct = default)
    {
        using var conn = _factory.Create();

        var tvp = BuildSystemsTvp(rows);

        var p = new DynamicParameters();
        p.Add("@CountryCode", countryCode?.Trim().ToUpperInvariant());
        p.Add("@Rows", tvp.AsTableValuedParameter("anprs.SystemImportTT"));
        p.Add("@Source", "ANPRS");

        await conn.ExecuteAsync(
            new CommandDefinition("anprs.RebuildSystems", p, commandType: CommandType.StoredProcedure, cancellationToken: ct));
    }

    public async Task<IEnumerable<SystemRowDto>> GetSystemsByCountryAsync(string countryCode, CancellationToken ct = default)
    {
        using var conn = _factory.Create();
        var sql = @"
        SELECT SystemCode, [Description]
        FROM anprs.Systems
        WHERE CountryCode = @Country
        ORDER BY SystemCode";
        return await conn.QueryAsync<SystemRowDto>(
            new CommandDefinition(sql, new { Country = countryCode }, cancellationToken: ct));
    }

    /// <summary>
    /// Odczyt kodów krajów z tabeli anprs.Countries (domenowe stringi).
    /// </summary>
    public async Task<IEnumerable<string>> GetCountryCodesAsync(CancellationToken ct = default)
    {
        using var conn = _factory.Create();
        var sql = @"
        SELECT CountryCode
        FROM anprs.Countries
        ORDER BY CountryCode";
        var result = await conn.QueryAsync<string>(new CommandDefinition(sql, cancellationToken: ct));
        return result.Select(s => (s ?? string.Empty).Trim().ToUpperInvariant());
    }

    public async Task<IEnumerable<BcpRowDto>> GetBcpAsync(CancellationToken ct = default)
    {
        using var conn = _factory.Create();
        var sql = @"
        SELECT BcpId, CountryCode, SystemCode, Name, Type, Latitude, Longitude
        FROM anprs.Bcp
        ORDER BY BcpId";
        return await conn.QueryAsync<BcpRowDto>(new CommandDefinition(sql, cancellationToken: ct));
        
    }


    private static DataTable BuildSystemsTvp(IEnumerable<SystemRowDto> rows)
    {
        var t = new DataTable();
        t.Columns.Add("SystemCode", typeof(string));
        t.Columns.Add("Description", typeof(string));

        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.SystemCode)) continue;
            t.Rows.Add(r.SystemCode.Trim().ToUpperInvariant(), r.Description?.Trim() ?? string.Empty);
        }
        return t;
    }

    private static DataTable BuildCountriesTvp(IEnumerable<string> codes)
    {
        var t = new DataTable();
        t.Columns.Add("CountryCode", typeof(string));
        foreach (var c in codes)
        {
            if (string.IsNullOrWhiteSpace(c)) continue;
            t.Rows.Add(c.Trim().ToUpperInvariant());
        }
        return t;
    }

    private static DataTable BuildBcpTvp(IEnumerable<BcpRowDto> rows)
    {
        var t = new DataTable();
        t.Columns.Add("BcpId", typeof(string));
        t.Columns.Add("CountryCode", typeof(string));
        t.Columns.Add("SystemCode", typeof(string));
        t.Columns.Add("Name", typeof(string));
        t.Columns.Add("Type", typeof(string));
        t.Columns.Add("Latitude", typeof(decimal));
        t.Columns.Add("Longitude", typeof(decimal));

        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.BcpId)) continue;
            t.Rows.Add(
                r.BcpId.Trim(),
                r.CountryCode.Trim().ToUpperInvariant(),
                r.SystemCode.Trim().ToUpperInvariant(),
                r.Name.Trim(),
                (object?)r.Type ?? DBNull.Value,
                (object?)r.Latitude ?? DBNull.Value,
                (object?)r.Longitude ?? DBNull.Value
            );
        }
        return t;
    }
              
}
