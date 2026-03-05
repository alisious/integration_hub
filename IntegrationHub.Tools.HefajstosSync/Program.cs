using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

// Narzędzie do zsynchronizowania danych posiadaczy broni
// z MongoDB (moja_baza) do tabel SQL piesp.BronOsoby i piesp.BronAdresy.

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var mongoConfig = configuration.GetSection("Mongo").Get<MongoConfig>()
                 ?? throw new InvalidOperationException("Brak sekcji Mongo w appsettings.json");
var sqlConfig = configuration.GetSection("Sql").Get<SqlConfig>()
               ?? throw new InvalidOperationException("Brak sekcji Sql w appsettings.json");

var mongoClient = new MongoClient(mongoConfig.ConnectionString);
var mongoDatabase = mongoClient.GetDatabase(mongoConfig.DatabaseName);

Console.WriteLine("HefajstosSync start");

try
{
    // 1) Załaduj BronOsoby z Mongo (RejestrBroni)
    var activePesels = await LoadBronOsobyAsync(mongoDatabase, mongoConfig, sqlConfig);

    // 2) Załaduj BronAdresy z Mongo (Osoby) na podstawie PESEL z BronOsoby
    await LoadBronAdresyAsync(mongoDatabase, mongoConfig, sqlConfig, activePesels);

    Console.WriteLine("HefajstosSync zakończony pomyślnie.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Błąd krytyczny HefajstosSync:");
    Console.Error.WriteLine(ex);
    return 1;
}

// ===== Funkcje pomocnicze =====

static async Task<HashSet<string>> LoadBronOsobyAsync(
    IMongoDatabase db,
    MongoConfig mongoConfig,
    SqlConfig sqlConfig)
{
    var collection = db.GetCollection<RejestrBroniDoc>(mongoConfig.RejestrBroniCollection);

    // interesują nas wpisy, gdzie w tablicy Rejestry_Broni jest element z data_wyrejestrowania_broni == null
    var filter = Builders<RejestrBroniDoc>.Filter.ElemMatch(
        d => d.RejestryBroni,
        rb => rb.DataWyrejestrowaniaBroni == null);

    var docs = await collection.Find(filter).ToListAsync();

    var pesels = new HashSet<string>();

    foreach (var doc in docs)
    {
        if (doc.RejestryBroni == null) continue;

        foreach (var entry in doc.RejestryBroni)
        {
            if (entry.DataWyrejestrowaniaBroni != null) continue;
            var pesel = entry.Osoby?.PeselOsoby?.Trim();
            if (!string.IsNullOrWhiteSpace(pesel))
                pesels.Add(pesel);
        }
    }

    Console.WriteLine($"Znaleziono {pesels.Count} aktywnych posiadaczy broni (PESEL) w Mongo.");

    using var conn = new SqlConnection(sqlConfig.ConnectionString);
    await conn.OpenAsync();

    using var tx = await conn.BeginTransactionAsync();

    // Czyścimy docelowe tabele – pełny reload
    await conn.ExecuteAsync("DELETE FROM piesp.BronAdresy", transaction: (IDbTransaction)tx);
    await conn.ExecuteAsync("DELETE FROM piesp.BronOsoby", transaction: (IDbTransaction)tx);

    const string insertSql = @"
INSERT INTO piesp.BronOsoby (BO_PESEL)
VALUES (@Pesel);";

    var rows = pesels.Select(p => new { Pesel = p });
    await conn.ExecuteAsync(insertSql, rows, transaction: (IDbTransaction)tx);

    await tx.CommitAsync();

    Console.WriteLine("Załadowano piesp.BronOsoby.");

    return pesels;
}

static async Task LoadBronAdresyAsync(
    IMongoDatabase db,
    MongoConfig mongoConfig,
    SqlConfig sqlConfig,
    HashSet<string> pesels)
{
    if (pesels.Count == 0)
    {
        Console.WriteLine("Brak posiadaczy broni – pomijam ładowanie piesp.BronAdresy.");
        return;
    }

    var osobyCollection = db.GetCollection<OsobaDoc>(mongoConfig.OsobyCollection);

    var filter = Builders<OsobaDoc>.Filter.In(o => o.Osoba.PeselOsoby, pesels);
    var docs = await osobyCollection.Find(filter).ToListAsync();

    var adresRows = new List<BronAdresRow>();

    foreach (var doc in docs)
    {
        var pesel = doc.Osoba?.PeselOsoby?.Trim();
        if (string.IsNullOrWhiteSpace(pesel)) continue;

        if (doc.Adresy == null) continue;

        foreach (var adr in doc.Adresy)
        {
            if (!adr.MiejsceBroni) continue;

            adresRows.Add(new BronAdresRow
            {
                Pesel = pesel,
                Miejscowosc = adr.Miejscowosc,
                Ulica = adr.Ulica,
                NumerDomu = adr.NumerDomu,
                NumerLokalu = adr.NumerLokalu,
                KodPocztowy = adr.KodPocztowy,
                Poczta = adr.Poczta
            });
        }
    }

    Console.WriteLine($"Przygotowano {adresRows.Count} rekordów adresowych do piesp.BronAdresy.");

    using var conn = new SqlConnection(sqlConfig.ConnectionString);
    await conn.OpenAsync();

    using var tx = await conn.BeginTransactionAsync();

    const string insertSql = @"
INSERT INTO piesp.BronAdresy (
    BA_BOPESEL,
    BA_MIEJSCOWOSC,
    BA_ULICA,
    BA_NUMER_DOMU,
    BA_NUMER_LOKALU,
    BA_KOD_POCZTOWY,
    BA_POCZTA)
VALUES (
    @Pesel,
    @Miejscowosc,
    @Ulica,
    @NumerDomu,
    @NumerLokalu,
    @KodPocztowy,
    @Poczta);";

    await conn.ExecuteAsync(insertSql, adresRows, transaction: (IDbTransaction)tx);

    await tx.CommitAsync();

    Console.WriteLine("Załadowano piesp.BronAdresy.");
}

// ===== Konfiguracja =====

public sealed class MongoConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string RejestrBroniCollection { get; set; } = "RejestrBroni";
    public string OsobyCollection { get; set; } = "Osoby";
}

public sealed class SqlConfig
{
    public string ConnectionString { get; set; } = string.Empty;
}

// ===== Modele MongoDB (tylko do odczytu) =====

public sealed class RejestrBroniDoc
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("Rejestry_Broni")]
    public List<RejestrBroniEntry> RejestryBroni { get; set; } = new();
}

public sealed class RejestrBroniEntry
{
    [BsonElement("Osoby")]
    public RejestrBroniOsoba? Osoby { get; set; }

    [BsonElement("data_wyrejestrowania_broni")]
    public DateTime? DataWyrejestrowaniaBroni { get; set; }
}

public sealed class RejestrBroniOsoba
{
    [BsonElement("pesel_osoby")]
    public string? PeselOsoby { get; set; }
}

public sealed class OsobaDoc
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("Osoba")]
    public MongoOsoba Osoba { get; set; } = new();

    [BsonElement("Adresy")]
    public List<MongoAdres> Adresy { get; set; } = new();
}

public sealed class MongoOsoba
{
    [BsonElement("pesel_osoby")]
    public string PeselOsoby { get; set; } = string.Empty;
}

public sealed class MongoAdres
{
    [BsonElement("miejsce_broni")]
    public bool MiejsceBroni { get; set; }

    [BsonElement("miejscowosc")]
    public string? Miejscowosc { get; set; }

    [BsonElement("ulica")]
    public string? Ulica { get; set; }

    [BsonElement("numer_domu")]
    public string? NumerDomu { get; set; }

    [BsonElement("numer_lokalu")]
    public string? NumerLokalu { get; set; }

    [BsonElement("kod_pocztowy")]
    public string? KodPocztowy { get; set; }

    [BsonElement("poczta")]
    public string? Poczta { get; set; }
}

// ===== DTO do inserta BronAdresy =====

public sealed class BronAdresRow
{
    public string Pesel { get; set; } = string.Empty;
    public string? Miejscowosc { get; set; }
    public string? Ulica { get; set; }
    public string? NumerDomu { get; set; }
    public string? NumerLokalu { get; set; }
    public string? KodPocztowy { get; set; }
    public string? Poczta { get; set; }
}
