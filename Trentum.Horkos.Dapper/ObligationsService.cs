using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trentum.Common.Csv; // CsvPeopleParser

namespace Trentum.Horkos;

public sealed class ObligationsService : IObligationsService
{
    private const string TblAnnual = "dbo.ZobowiazaniaRoczne";
    private const string TblDischarged = "dbo.ZobowiazaniaPoZwolnieniu";

    private readonly IDbConnectionFactory _factory;

    public ObligationsService(IDbConnectionFactory factory) => _factory = factory;

    public async Task<int> ImportAnnualList(Stream csvStream, int horkosListId, int rok, CancellationToken ct = default)
    {
        var people = CsvPeopleParser.ParseAnnualCsv(csvStream); // Osoba
        if (people.Count == 0) return 0;

        using var conn = _factory.Create();
        if (conn is DbConnection db)
            await db.OpenAsync(ct);
        else
            conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            await DeleteAnnualAsync(conn, tx, horkosListId, ct);
            
            var rows = await BulkInsertAnnualAsync(conn, tx, CreateAnnualParams(people, horkosListId, rok));

            tx.Commit();
            return rows;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<int> ImportDischargedList(Stream csvStream, int horkosListId, int rok, string miesiac, CancellationToken ct = default)
    {
        var people = CsvPeopleParser.ParseDischargedCsv(csvStream); // OsobaZwolniona
        if (people.Count == 0) return 0;

        using var conn = _factory.Create();
        if (conn is DbConnection db)
            await db.OpenAsync(ct);
        else
            conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            await DeleteDischargedAsync(conn, tx, horkosListId, ct);

            var rows = await BulkInsertDischargedAsync(conn, tx, CreateDischargedParams(people, horkosListId, rok,miesiac));

            tx.Commit();
            return rows;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private async Task<int> BulkInsertAnnualAsync(
    IDbConnection conn,
    IDbTransaction tx,
    IEnumerable<object> rows) // to co zwraca CreateAnnualParams(...)
    {
        // Zamiana na DataTable (najprostsza droga bez dodatkowych paczek)
        var dt = new DataTable();
        dt.Columns.Add("HorkosListaZobowiazanychId", typeof(int));
        dt.Columns.Add("Rok", typeof(int));
        dt.Columns.Add("Stopien", typeof(string));
        dt.Columns.Add("ImiePierwsze", typeof(string));
        dt.Columns.Add("ImieDrugie", typeof(string));
        dt.Columns.Add("Nazwisko", typeof(string));
        dt.Columns.Add("Pesel", typeof(string));
        dt.Columns.Add("Stanowisko", typeof(string));
        dt.Columns.Add("NazwaJednostkiWojskowej", typeof(string));

        foreach (var r in rows)
        {
            dynamic x = r!;
            dt.Rows.Add(
                x.HorkosListaZobowiazanychId,
                x.Rok,
                x.Stopien,
                x.ImiePierwsze,
                x.ImieDrugie,
                x.Nazwisko,
                x.Pesel,
                x.Stanowisko,
                x.NazwaJednostkiWojskowej
            );
        }

        // Uwaga: musimy mieć SqlConnection pod spodem
        var sqlConn = (SqlConnection)conn;
        using var bulk = new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.TableLock, (SqlTransaction)tx)
        {
            DestinationTableName = TblAnnual,
            BatchSize = 5000,
            BulkCopyTimeout = 0 // bez limitu (lub ustaw wg potrzeb)
        };

        bulk.ColumnMappings.Add("HorkosListaZobowiazanychId", "HorkosListaZobowiazanychId");
        bulk.ColumnMappings.Add("Rok", "Rok");
        bulk.ColumnMappings.Add("Stopien", "Stopien");
        bulk.ColumnMappings.Add("ImiePierwsze", "ImiePierwsze");
        bulk.ColumnMappings.Add("ImieDrugie", "ImieDrugie");
        bulk.ColumnMappings.Add("Nazwisko", "Nazwisko");
        bulk.ColumnMappings.Add("Pesel", "Pesel");
        bulk.ColumnMappings.Add("Stanowisko", "Stanowisko");
        bulk.ColumnMappings.Add("NazwaJednostkiWojskowej", "NazwaJednostkiWojskowej");

        await bulk.WriteToServerAsync(dt);
        return dt.Rows.Count;
    }

    private async Task<int> BulkInsertDischargedAsync(
   IDbConnection conn,
   IDbTransaction tx,
   IEnumerable<object> rows) // to co zwraca CreateAnnualParams(...)
    {
        // Zamiana na DataTable (najprostsza droga bez dodatkowych paczek)
        var dt = new DataTable();
        dt.Columns.Add("HorkosListaZobowiazanychId", typeof(int));
        dt.Columns.Add("Rok", typeof(int));
        dt.Columns.Add("Miesiac", typeof(string));
        dt.Columns.Add("Stopien", typeof(string));
        dt.Columns.Add("ImiePierwsze", typeof(string));
        dt.Columns.Add("ImieDrugie", typeof(string));
        dt.Columns.Add("Nazwisko", typeof(string));
        dt.Columns.Add("Pesel", typeof(string));
        dt.Columns.Add("Stanowisko", typeof(string));
        dt.Columns.Add("NazwaJednostkiWojskowej", typeof(string));
        dt.Columns.Add("DataZwolnienia", typeof(string));

        foreach (var r in rows)
        {
            dynamic x = r!;
            dt.Rows.Add(
                x.HorkosListaZobowiazanychId,
                x.Rok,
                x.Miesiac,
                x.Stopien,
                x.ImiePierwsze,
                x.ImieDrugie,
                x.Nazwisko,
                x.Pesel,
                x.Stanowisko,
                x.NazwaJednostkiWojskowej,
                x.DataZwolnienia
            );
        }

        // Uwaga: musimy mieć SqlConnection pod spodem
        var sqlConn = (SqlConnection)conn;
        using var bulk = new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.TableLock, (SqlTransaction)tx)
        {
            DestinationTableName = TblDischarged,
            BatchSize = 5000,
            BulkCopyTimeout = 0 // bez limitu (lub ustaw wg potrzeb)
        };

        bulk.ColumnMappings.Add("HorkosListaZobowiazanychId", "HorkosListaZobowiazanychId");
        bulk.ColumnMappings.Add("Rok", "Rok");
        bulk.ColumnMappings.Add("Miesiac", "Miesiac");
        bulk.ColumnMappings.Add("Stopien", "Stopien");
        bulk.ColumnMappings.Add("ImiePierwsze", "ImiePierwsze");
        bulk.ColumnMappings.Add("ImieDrugie", "ImieDrugie");
        bulk.ColumnMappings.Add("Nazwisko", "Nazwisko");
        bulk.ColumnMappings.Add("Pesel", "Pesel");
        bulk.ColumnMappings.Add("Stanowisko", "Stanowisko");
        bulk.ColumnMappings.Add("NazwaJednostkiWojskowej", "NazwaJednostkiWojskowej");
        bulk.ColumnMappings.Add("DataZwolnienia", "DataZwolnienia");

        await bulk.WriteToServerAsync(dt);
        return dt.Rows.Count;
    }

    // ==== Kasowanie (wydzielone metody) ====

    private static async Task<int> DeleteAnnualAsync(IDbConnection conn, IDbTransaction tx, int horkosListId, CancellationToken ct)
    {
        const string sql = $@"DELETE FROM {TblAnnual} WHERE HorkosListaZobowiazanychId=@HorkosListId;";
        return await conn.ExecuteAsync(new CommandDefinition(sql, new { HorkosListId = horkosListId }, tx, cancellationToken: ct));
    }

    private static async Task<int> DeleteDischargedAsync(IDbConnection conn, IDbTransaction tx, int horkosListId, CancellationToken ct)
    {
        const string sql = $@"DELETE FROM {TblDischarged} WHERE HorkosListaZobowiazanychId=@HorkosListId;";
        return await conn.ExecuteAsync(new CommandDefinition(sql, new { HorkosListId = horkosListId}, tx, cancellationToken: ct));
    }

    // ==== Mapowanie parametrów do INSERT ====

    private static IEnumerable<object> CreateAnnualParams(IReadOnlyList<CsvPeopleParser.Osoba> people, int horkosListId, int rok)
        => people.Select(p => new
        {
            HorkosListaZobowiazanychId = horkosListId,
            Rok = rok,
            p.Stopien,
            p.ImiePierwsze,
            p.ImieDrugie,
            p.Nazwisko,
            Pesel = p.PESEL,
            p.Stanowisko,
            p.NazwaJednostkiWojskowej
        });

    private static IEnumerable<object> CreateDischargedParams(IReadOnlyList<CsvPeopleParser.OsobaZwolniona> people, int horkosListId, int rok, string miesiac)
        => people.Select(p => new
        {
            HorkosListaZobowiazanychId = horkosListId,
            Rok = rok,
            Miesiac = miesiac,
            p.Stopien,
            p.ImiePierwsze,
            p.ImieDrugie,
            p.Nazwisko,
            Pesel = p.PESEL,
            p.Stanowisko,
            p.NazwaJednostkiWojskowej,
            p.DataZwolnienia // varchar(10) wg Twojej decyzji
        });
}
