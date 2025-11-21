using IntegrationHub.Common.Primitives;
using IntegrationHub.Sources.ZW.Config;
using IntegrationHub.Sources.ZW.Contracts;
using IntegrationHub.Sources.ZW.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
using Dapper;

namespace IntegrationHub.Sources.ZW.Services
{
    public class ZandWantedPersonService : IZandWantedPersonService
    {
        private readonly string _cs;
        private readonly int _timeout;
        private ZandConfig _config;
        private readonly ILogger<ZandWantedPersonService> _logger;

        public ZandWantedPersonService(IOptions<ZandConfig> config, ILogger<ZandWantedPersonService> logger)
        {
            _config = config.Value;
            _cs = _config.ConnectionString
                  ?? throw new System.InvalidOperationException("Sources:ZW:ConnectionString is required.");
            _timeout = _config.TimeoutSeconds;
            _logger = logger;
        }

        public async Task<Result<IReadOnlyList<ZandWantedPersonDto>, Error>>
            GetByPeselAsync(string pesel, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(pesel))
                return new Error("Validation.PeselEmpty", "PESEL jest wymagany.", 400);

            var sql = @"
                SELECT 
                    OP_PESEL AS [Pesel],
                    OP_JEDNPOSZUK AS [JzwPoszukujaca]
                    FROM piesp.OsobyPoszukiwane WITH (NOLOCK)
                WHERE OP_PESEL = @Pesel
                ORDER BY OP_PESEL;";

            try
            {
                await using var conn = new SqlConnection(_cs);
                await conn.OpenAsync(ct);

                var rows = await conn.QueryAsync<ZandWantedPersonDto>(
                    new CommandDefinition(
                        sql,
                        new { Pesel = pesel },
                        commandType: CommandType.Text,
                        commandTimeout: _timeout,
                        cancellationToken: ct));

                var list = new List<ZandWantedPersonDto>(rows);
                return list;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "ZW.GetByPesel failed for PESEL {Pesel}", pesel);
                return new Error("ZW.SqlError", "Błąd połączenia z bazą ZW.", 502, ex.Message);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "ZW.GetByPesel unexpected error for PESEL {Pesel}", pesel);
                return new Error("ZW.Unexpected", "Nieoczekiwany błąd podczas odczytu z ZW.", 500, ex.Message);
            }
        }

        public async Task<Result<IReadOnlyList<BronOsobaResponse>, Error>> GetBronOsobaByPeselAsync(
            BronOsobaRequest request,
            CancellationToken ct = default)
        {
            if (request is null)
                return ErrorFactory.BusinessError(
                    ErrorCodeEnum.ValidationError, 
                    "Request nie może być null."
                 );

            var pesel = request.Pesel?.Trim();

            if (string.IsNullOrWhiteSpace(pesel))
                return ErrorFactory.BusinessError(
                    ErrorCodeEnum.ValidationError, 
                    "PESEL jest wymagany."
                );

            const string sqlPerson = @"
                SELECT BO_PESEL AS [Pesel]
                FROM piesp.BronOsoby WITH (NOLOCK)
                WHERE BO_PESEL = @Pesel;";

            const string sqlAddresses = @"
                SELECT
                    BA_MIEJSCOWOSC AS [Miejscowosc],
                    BA_ULICA        AS [Ulica],
                    BA_NUMER_DOMU   AS [NumerDomu],
                    BA_NUMER_LOKALU AS [NumerLokalu],
                    BA_KOD_POCZTOWY AS [KodPocztowy],
                    BA_POCZTA       AS [Poczta],
                    BA_OPIS         AS [Opis]
                FROM piesp.BronAdresy WITH (NOLOCK)
                WHERE BA_BOPESEL = @Pesel
                ORDER BY BA_MIEJSCOWOSC, BA_ULICA, BA_NUMER_DOMU, BA_NUMER_LOKALU;";

            try
            {
                await using var conn = new SqlConnection(_cs);
                await conn.OpenAsync(ct);

                var personPesel = await conn.QueryFirstOrDefaultAsync<string>(
                    new CommandDefinition(
                        sqlPerson,
                        new { Pesel = pesel },
                        commandType: CommandType.Text,
                        commandTimeout: _timeout,
                        cancellationToken: ct));

                // Brak wpisu w BronOsoby => zwracamy błąd biznesowy
                if (personPesel is null)
                    return ErrorFactory.BusinessError(
                        ErrorCodeEnum.NotFoundError, 
                        $"Nie znaleziono informacji o broni prywatnej dla PESEL {pesel}.");

                var adresy = await conn.QueryAsync<BronAdresDto>(
                    new CommandDefinition(
                        sqlAddresses,
                        new { Pesel = pesel },
                        commandType: CommandType.Text,
                        commandTimeout: _timeout,
                        cancellationToken: ct));

                var response = new BronOsobaResponse
                {
                    Pesel = personPesel,
                    Adresy = new List<BronAdresDto>(adresy)
                };

                return new List<BronOsobaResponse> { response };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "ZW.GetBronOsobaByPesel failed for PESEL {Pesel}", pesel);
                return ErrorFactory.TechnicalError(
                    ErrorCodeEnum.SqlError, 
                    "Błąd połączenia z bazą ZW.", 
                    details: ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZW.GetBronOsobaByPesel unexpected error for PESEL {Pesel}", pesel);
                return ErrorFactory.TechnicalError(
                    ErrorCodeEnum.UnexpectedError, 
                    "Nieoczekiwany błąd podczas odczytu z ZW.",
                    details: ex.Message);
            }
        }

        public async Task<Result<OsobaZolnierzResponse, Error>> GetOsobaZolnierzByPeselAsync(
            OsobaZolnierzRequest request,
            CancellationToken ct = default)
        {
            if (request is null)
                return ErrorFactory.BusinessError(
                    ErrorCodeEnum.ValidationError,
                    "Request nie może być null.");

            var pesel = request.Pesel?.Trim();

            if (string.IsNullOrWhiteSpace(pesel))
                return ErrorFactory.BusinessError(
                    ErrorCodeEnum.ValidationError,
                    "PESEL jest wymagany.");

            const string sql = @"
                SELECT
                    OZ_PESEL      AS [Pesel],
                    OZ_STOPIEN    AS [Stopien],
                    OZ_JEDNOSTKA  AS [Jednostka],
                    OZ_PESEL_HASH AS [PeselHash]
                FROM piesp.OsobyZolnierze WITH (NOLOCK)
                WHERE OZ_PESEL = @Pesel;";

            try
            {
                await using var conn = new SqlConnection(_cs);
                await conn.OpenAsync(ct);

                var soldier = await conn.QueryFirstOrDefaultAsync<OsobaZolnierzResponse>(
                    new CommandDefinition(
                        sql,
                        new { Pesel = pesel },
                        commandType: CommandType.Text,
                        commandTimeout: _timeout,
                        cancellationToken: ct));

                // Brak wpisu => błąd biznesowy NotFound
                if (soldier is null)
                    return ErrorFactory.BusinessError(
                        ErrorCodeEnum.NotFoundError,
                        $"Nie znaleziono informacji o żołnierzu dla PESEL {pesel}.");

                return soldier;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "ZW.GetOsobaZolnierzByPeselAsync failed for PESEL {Pesel}", pesel);

                return ErrorFactory.TechnicalError(
                    ErrorCodeEnum.SqlError,
                    "Błąd połączenia z bazą ZW.",
                    details: ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZW.GetOsobaZolnierzByPeselAsync unexpected error for PESEL {Pesel}", pesel);

                return ErrorFactory.TechnicalError(
                    ErrorCodeEnum.UnexpectedError,
                    "Nieoczekiwany błąd podczas odczytu z ZW.",
                    details: ex.Message);
            }
        }


    }
}
    

