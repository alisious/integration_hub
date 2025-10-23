// IntegrationHub.Application.ANPRS/ANPRSDictionaryFacade.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntegrationHub.Application.Mappers.ANPRS;
using IntegrationHub.Domain.Contracts.ANPRS;
using IntegrationHub.Domain.Interfaces.ANPRS;
using IntegrationHub.Sources.ANPRS.Services;
using Microsoft.Extensions.Logging;

namespace IntegrationHub.Application.ANPRS;

public sealed class ANPRSDictionaryFacade : IANPRSDictionaryFacade
{
    private readonly IANPRSDictionaryService _source;
    private readonly IANPRSDictionaryRepository _repo;
    private readonly ILogger<ANPRSDictionaryFacade> _logger;

    public ANPRSDictionaryFacade(
        IANPRSDictionaryService source,
        IANPRSDictionaryRepository repo,
        ILogger<ANPRSDictionaryFacade> logger)
    {
        _source = source;
        _repo = repo;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> GetCountriesAsync(CancellationToken ct = default)
    {
        var resp = await _source.GetCountriesAsync(ct);
        if (resp is null) return Enumerable.Empty<string>();

        var codes = CountriesMapper.ToCountryCodes(resp).ToList();
        return codes;
    }

    public async Task<IEnumerable<BcpRowDto>> GetBCPAsync(CancellationToken ct = default)
    {
        var resp = await _source.GetBCPAsync(ct);
        if (resp is null) return Enumerable.Empty<BcpRowDto>();

        var rows = BcpMapper.ToBcp(resp).ToList();
        return rows;
    }

    public async Task<IEnumerable<SystemRowDto>> GetSystemsAsync(string country, CancellationToken ct = default)
    {
        var resp = await _source.GetSystemsAsync(country, ct);
        if (resp is null) return Enumerable.Empty<SystemRowDto>();

        var rows = SystemsMapper.ToSystems(resp).ToList();
        return rows;
    }

    public async Task SaveCountriesToDbAsync(CancellationToken ct = default)
    {
        var codes = (await GetCountriesAsync(ct)).ToList();
        if (codes.Count == 0)
        {
            _logger.LogInformation("Countries: brak wierszy do zapisania.");
            return;
        }

        await _repo.UpsertCountriesAsync(codes, ct);
        _logger.LogInformation("Countries saved to DB. Count={Count}", codes.Count);
    }

    public async Task SaveBcpToDbAsync(CancellationToken ct = default)
    {
        var rows = (await GetBCPAsync(ct)).ToList();
        if (rows.Count == 0)
        {
            _logger.LogInformation("BCP: brak wierszy do zapisania.");
            return;
        }

        await _repo.UpsertBcpAsync(rows, ct);
        _logger.LogInformation("BCP saved to DB. Rows={Count}", rows.Count);
    }

    public async Task SaveSystemsToDbAsync(string country, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("country is required", nameof(country));

        var rows = (await GetSystemsAsync(country, ct)).ToList();
        if (rows.Count == 0)
        {
            _logger.LogInformation("Systems: brak wierszy do zapisania for country={Country}", country);
            return;
        }

        await _repo.ReloadSystemsAsync(country.Trim().ToUpperInvariant(), rows, ct);
        _logger.LogInformation("Systems saved to DB for country={Country}. Rows={Count}", country, rows.Count);
    }

    public Task<IEnumerable<SystemRowDto>> GetSystemsLocalAsync(string country, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(country))
            return Task.FromResult(Enumerable.Empty<SystemRowDto>());

        var cc = country.Trim().ToUpperInvariant();
        return _repo.GetSystemsByCountryAsync(cc, ct);
    }

    public async Task<IEnumerable<string>> GetCountriesLocalAsync(CancellationToken ct = default)
    {
        var codes = await _repo.GetCountryCodesAsync(ct);
        return codes?.Select(s => (s ?? string.Empty).Trim().ToUpperInvariant()).ToList() ?? Enumerable.Empty<string>();
    }

    public async Task<IEnumerable<BcpRowDto>> GetBcpLocalAsync(CancellationToken ct = default)
    {
        var rows = await _repo.GetBcpAsync(ct);
        return rows?.ToList() ?? Enumerable.Empty<BcpRowDto>();
    }
}