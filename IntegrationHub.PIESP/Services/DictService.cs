using Dapper;
using IntegrationHub.Common.Primitives;
using IntegrationHub.Infrastructure.Abstractions;
using IntegrationHub.PIESP.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace IntegrationHub.PIESP.Services;

/// <summary>
/// Serwis do pobierania słowników z tabeli piesp.DictItems przy użyciu Dapper.
/// </summary>
public sealed class DictService : IDictService
{
    private readonly IDbConnectionFactory _factory;
    private readonly ILogger<DictService> _logger;

    public DictService(IDbConnectionFactory factory, ILogger<DictService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <summary>
    /// Pobiera wszystkie elementy słownika z tabeli piesp.DictItems.
    /// </summary>
    public async Task<Result<IReadOnlyList<DictItem>, Error>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT 
                DI_ID AS [Id],
                DI_DID AS [DictId],
                DI_CODE AS [Code],
                DI_VALUE AS [Value],
                DI_CREATEDAT AS [CreatedAt]
            FROM piesp.DictItems WITH (NOLOCK)
            ORDER BY DI_DID, DI_CODE, DI_VALUE;";

        try
        {
            using var conn = _factory.Create();
            var rows = await conn.QueryAsync<DictItem>(
                new CommandDefinition(sql, commandType: CommandType.Text, cancellationToken: ct));

            return rows.AsList();
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania wszystkich elementów słownika.");
            return ErrorFactory.TechnicalError(
                ErrorCodeEnum.SqlError,
                "Błąd połączenia z bazą danych podczas pobierania słowników.",
                httpStatus: 502,
                details: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nieoczekiwany błąd podczas pobierania wszystkich elementów słownika.");
            return ErrorFactory.TechnicalError(
                ErrorCodeEnum.UnexpectedError,
                "Nieoczekiwany błąd podczas pobierania słowników.",
                httpStatus: 500,
                details: ex.Message);
        }
    }

    /// <summary>
    /// Pobiera wszystkie elementy dla danego słownika (po DI_DID).
    /// </summary>
    public async Task<Result<IReadOnlyList<DictItem>, Error>> GetByDictIdAsync(string dictId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dictId))
        {
            return ErrorFactory.BusinessError(
                ErrorCodeEnum.ValidationError,
                "Identyfikator słownika (dictId) jest wymagany.",
                httpStatus: 400);
        }

        const string sql = @"
            SELECT 
                DI_ID AS [Id],
                DI_DID AS [DictId],
                DI_CODE AS [Code],
                DI_VALUE AS [Value],
                DI_CREATEDAT AS [CreatedAt]
            FROM piesp.DictItems WITH (NOLOCK)
            WHERE DI_DID = @DictId
            ORDER BY DI_CODE, DI_VALUE;";

        try
        {
            using var conn = _factory.Create();
            var rows = await conn.QueryAsync<DictItem>(
                new CommandDefinition(
                    sql,
                    new { DictId = dictId },
                    commandType: CommandType.Text,
                    cancellationToken: ct));

            return rows.AsList();
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania elementów słownika dla DictId={DictId}.", dictId);
            return ErrorFactory.TechnicalError(
                ErrorCodeEnum.SqlError,
                "Błąd połączenia z bazą danych podczas pobierania słowników.",
                httpStatus: 502,
                details: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nieoczekiwany błąd podczas pobierania elementów słownika dla DictId={DictId}.", dictId);
            return ErrorFactory.TechnicalError(
                ErrorCodeEnum.UnexpectedError,
                "Nieoczekiwany błąd podczas pobierania słowników.",
                httpStatus: 500,
                details: ex.Message);
        }
    }

    /// <summary>
    /// Pobiera element słownika po identyfikatorze (DI_ID).
    /// </summary>
    public async Task<Result<DictItem?, Error>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ErrorFactory.BusinessError(
                ErrorCodeEnum.ValidationError,
                "Identyfikator elementu (id) jest wymagany.",
                httpStatus: 400);
        }

        const string sql = @"
            SELECT 
                DI_ID AS [Id],
                DI_DID AS [DictId],
                DI_CODE AS [Code],
                DI_VALUE AS [Value],
                DI_CREATEDAT AS [CreatedAt]
            FROM piesp.DictItems WITH (NOLOCK)
            WHERE DI_ID = @Id;";

        try
        {
            using var conn = _factory.Create();
            var item = await conn.QueryFirstOrDefaultAsync<DictItem>(
                new CommandDefinition(
                    sql,
                    new { Id = id },
                    commandType: CommandType.Text,
                    cancellationToken: ct));

            return item;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania elementu słownika dla Id={Id}.", id);
            return ErrorFactory.TechnicalError(
                ErrorCodeEnum.SqlError,
                "Błąd połączenia z bazą danych podczas pobierania słownika.",
                httpStatus: 502,
                details: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nieoczekiwany błąd podczas pobierania elementu słownika dla Id={Id}.", id);
            return ErrorFactory.TechnicalError(
                ErrorCodeEnum.UnexpectedError,
                "Nieoczekiwany błąd podczas pobierania słownika.",
                httpStatus: 500,
                details: ex.Message);
        }
    }

    /// <summary>
    /// Pobiera element słownika po kodzie i ID słownika (DI_CODE i DI_DID).
    /// </summary>
    public async Task<Result<DictItem?, Error>> GetByCodeAsync(string dictId, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dictId))
        {
            return ErrorFactory.BusinessError(
                ErrorCodeEnum.ValidationError,
                "Identyfikator słownika (dictId) jest wymagany.",
                httpStatus: 400);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return ErrorFactory.BusinessError(
                ErrorCodeEnum.ValidationError,
                "Kod elementu (code) jest wymagany.",
                httpStatus: 400);
        }

        const string sql = @"
            SELECT 
                DI_ID AS [Id],
                DI_DID AS [DictId],
                DI_CODE AS [Code],
                DI_VALUE AS [Value],
                DI_CREATEDAT AS [CreatedAt]
            FROM piesp.DictItems WITH (NOLOCK)
            WHERE DI_DID = @DictId AND DI_CODE = @Code;";

        try
        {
            using var conn = _factory.Create();
            var item = await conn.QueryFirstOrDefaultAsync<DictItem>(
                new CommandDefinition(
                    sql,
                    new { DictId = dictId, Code = code },
                    commandType: CommandType.Text,
                    cancellationToken: ct));

            return item;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania elementu słownika dla DictId={DictId}, Code={Code}.", dictId, code);
            return ErrorFactory.TechnicalError(
                ErrorCodeEnum.SqlError,
                "Błąd połączenia z bazą danych podczas pobierania słownika.",
                httpStatus: 502,
                details: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nieoczekiwany błąd podczas pobierania elementu słownika dla DictId={DictId}, Code={Code}.", dictId, code);
            return ErrorFactory.TechnicalError(
                ErrorCodeEnum.UnexpectedError,
                "Nieoczekiwany błąd podczas pobierania słownika.",
                httpStatus: 500,
                details: ex.Message);
        }
    }
}
