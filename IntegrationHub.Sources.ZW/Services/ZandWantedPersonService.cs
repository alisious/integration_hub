using Dapper;
using IntegrationHub.Common.Primitives;
using IntegrationHub.Sources.ZW.Config;
using IntegrationHub.Sources.ZW.Contracts;
using IntegrationHub.Sources.ZW.Interfaces;
using IntegrationHub.Sources.ZW.RequestValidation;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<Result<BronOsobaResponse, Error>> GetBronAdresAsync(
    BronAdresRequest request,
    CancellationToken ct = default)
        {
            if (request is null)
            {
                return ErrorFactory.BusinessError(
                    ErrorCodeEnum.ValidationError,
                    "Request nie może być null.");
            }

            var validator = new BronAdresRequestValidator();
            var vr = validator.ValidateAndNormalize(request);
            if (!vr.IsValid)
            {
                return ErrorFactory.BusinessError(
                    ErrorCodeEnum.ValidationError,
                    vr.MessageError ?? "Błąd walidacji BronAdresRequest.");
            }

            const string baseSql = @"
        SELECT
            BA_BOPESEL      AS [Pesel],
            BA_MIEJSCOWOSC  AS [Miejscowosc],
            BA_ULICA        AS [Ulica],
            BA_NUMER_DOMU   AS [NumerDomu],
            BA_NUMER_LOKALU AS [NumerLokalu],
            BA_KOD_POCZTOWY AS [KodPocztowy],
            BA_POCZTA       AS [Poczta],
            BA_OPIS         AS [Opis]
        FROM piesp.BronAdresy WITH (NOLOCK)
        WHERE 1 = 1";

            var sqlBuilder = new StringBuilder(baseSql);
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(request.Miejscowosc))
            {
                sqlBuilder.AppendLine(" AND BA_MIEJSCOWOSC = @Miejscowosc");
                parameters.Add("Miejscowosc", request.Miejscowosc);
            }

            if (!string.IsNullOrWhiteSpace(request.Ulica))
            {
                sqlBuilder.AppendLine(" AND BA_ULICA = @Ulica");
                parameters.Add("Ulica", request.Ulica);
            }

            if (!string.IsNullOrWhiteSpace(request.NumerDomu))
            {
                sqlBuilder.AppendLine(" AND BA_NUMER_DOMU = @NumerDomu");
                parameters.Add("NumerDomu", request.NumerDomu);
            }

            if (!string.IsNullOrWhiteSpace(request.NumerLokalu))
            {
                sqlBuilder.AppendLine(" AND BA_NUMER_LOKALU = @NumerLokalu");
                parameters.Add("NumerLokalu", request.NumerLokalu);
            }

            if (!string.IsNullOrWhiteSpace(request.KodPocztowy))
            {
                sqlBuilder.AppendLine(" AND BA_KOD_POCZTOWY = @KodPocztowy");
                parameters.Add("KodPocztowy", request.KodPocztowy);
            }

            if (!string.IsNullOrWhiteSpace(request.Poczta))
            {
                sqlBuilder.AppendLine(" AND BA_POCZTA = @Poczta");
                parameters.Add("Poczta", request.Poczta);
            }

            sqlBuilder.AppendLine(@"
        ORDER BY BA_MIEJSCOWOSC, BA_ULICA, BA_NUMER_DOMU, BA_NUMER_LOKALU;");

            var sql = sqlBuilder.ToString();

            try
            {
                await using var conn = new SqlConnection(_cs);
                await conn.OpenAsync(ct);

                var rows = (await conn.QueryAsync<BronAdresFlatRow>(
                    new CommandDefinition(
                        sql,
                        parameters,
                        commandType: CommandType.Text,
                        commandTimeout: _timeout,
                        cancellationToken: ct)))
                    .ToList();

                if (rows.Count == 0)
                {
                    return ErrorFactory.BusinessError(
                        ErrorCodeEnum.NotFoundError,
                        "Nie znaleziono lokalizacji spełniającej zadane kryteria.");
                }

                if (rows.Count > 1)
                {
                    return ErrorFactory.BusinessError(
                        ErrorCodeEnum.ValidationError,
                        "Znaleziono więcej niż jedną lokalizację. Doprecyzuj parametry wyszukiwania.");
                }

                var row = rows[0];

                var adres = new BronAdresDto
                {
                    Miejscowosc = row.Miejscowosc,
                    Ulica = row.Ulica,
                    NumerDomu = row.NumerDomu,
                    NumerLokalu = row.NumerLokalu,
                    KodPocztowy = row.KodPocztowy,
                    Poczta = row.Poczta,
                    Opis = row.Opis
                };

                var response = new BronOsobaResponse
                {
                    Pesel = row.Pesel,              // z piesp.BronAdresy
                    Adresy = new List<BronAdresDto> { adres }
                };

                return response;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "ZW.GetBronAdresAsync failed for {@Request}", request);
                return ErrorFactory.TechnicalError(
                    ErrorCodeEnum.SqlError,
                    "Błąd połączenia z bazą ZW.",
                    details: ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZW.GetBronAdresAsync unexpected error for {@Request}", request);
                return ErrorFactory.TechnicalError(
                    ErrorCodeEnum.UnexpectedError,
                    "Nieoczekiwany błąd podczas odczytu z ZW.",
                    details: ex.Message);
            }
        }

        // Internal DTO do mapowania wyników z piesp.BronAdresy
        private sealed class BronAdresFlatRow
        {
            public string Pesel { get; set; } = default!;
            public string Miejscowosc { get; set; } = default!;
            public string? Ulica { get; set; }
            public string NumerDomu { get; set; } = default!;
            public string? NumerLokalu { get; set; }
            public string? KodPocztowy { get; set; }
            public string? Poczta { get; set; }
            public string? Opis { get; set; }
        }

    }
}

    

