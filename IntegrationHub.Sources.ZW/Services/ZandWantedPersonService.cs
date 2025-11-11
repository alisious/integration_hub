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

        
    }
}
    

