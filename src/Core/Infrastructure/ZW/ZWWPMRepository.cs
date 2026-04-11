using System.Data;
using System.Text;
using Dapper;
using IntegrationHub.Domain.Contracts.ZW;
using IntegrationHub.Domain.Interfaces.ZW;
using IntegrationHub.Infrastructure.Abstractions;

namespace IntegrationHub.Infrastructure.ZW;

public sealed class ZWWPMRepository : IZWWPMRepository
{
    private readonly IDbConnectionFactory _factory;

    public ZWWPMRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<IEnumerable<WPMResponse>> SearchAsync(WPMRequest request, CancellationToken ct = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        using var conn = _factory.Create();

        var (whereSql, p) = BuildCriteria(request);

        var sql = new StringBuilder(@"
SELECT
    pw.Id,
    pw.NrRejestracyjny,
    pw.Opis,
    pw.RokProdukcji,
    pw.NumerPodwozia,
    pw.NrSerProducenta,
    pw.NrSerSilnika,
    pw.Miejscowosc,
    pw.JednostkaWojskowa,
    pw.JednostkaGospodarcza,
    pw.DataAktualizacji
FROM [piesp].[PojazdyWojskowe] pw
WHERE 1=1
");
        sql.AppendLine(whereSql);
        sql.AppendLine("ORDER BY pw.Id DESC");

        var cmd = new CommandDefinition(sql.ToString(), p, commandType: CommandType.Text, cancellationToken: ct);
        return await conn.QueryAsync<WPMResponse>(cmd);
    }

    public async Task<int> CountVehiclesAsync(WPMRequest request, CancellationToken ct = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        using var conn = _factory.Create();

        var (whereSql, p) = BuildCriteria(request);

        var sql = new StringBuilder(@"
SELECT COUNT(1)
FROM [piesp].[PojazdyWojskowe] pw
WHERE 1=1
");
        sql.AppendLine(whereSql);

        var cmd = new CommandDefinition(sql.ToString(), p, commandType: CommandType.Text, cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(cmd);
    }

    private static (string whereSql, DynamicParameters parameters) BuildCriteria(WPMRequest request)
    {
        // Normalizacja wejścia (TRIM + null, jeśli puste)
        var nrRej = Normalize(request.NrRejestracyjny);
        var vin = Normalize(request.NumerPodwozia);
        var prod = Normalize(request.NrSerProducenta);
        var silnik = Normalize(request.NrSerSilnika);

        if (nrRej is null && vin is null && prod is null && silnik is null)
            throw new ArgumentException("Podaj przynajmniej jedno kryterium: NrRejestracyjny / NumerPodwozia / NrSerProducenta / NrSerSilnika.");

        var p = new DynamicParameters();
        var anyClauses = new List<string>();

        if (nrRej is not null) { anyClauses.Add("pw.NrRejestracyjny LIKE @NrRejestracyjny"); p.Add("@NrRejestracyjny", nrRej + "%"); }
        if (vin is not null) { anyClauses.Add("pw.NumerPodwozia   = @NumerPodwozia"); p.Add("@NumerPodwozia", vin); }
        if (prod is not null) { anyClauses.Add("pw.NrSerProducenta = @NrSerProducenta"); p.Add("@NrSerProducenta", prod); }
        if (silnik is not null) { anyClauses.Add("pw.NrSerSilnika    = @NrSerSilnika"); p.Add("@NrSerSilnika", silnik); }

        var where = "  AND (" + string.Join(" OR ", anyClauses) + ")";
        return (where, p);
    }

    private static string? Normalize(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
