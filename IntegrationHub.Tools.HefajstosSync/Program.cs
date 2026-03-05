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
    var collection = db.GetCollection<BsonDocument>(mongoConfig.RejestrBroniCollection);

    // interesują nas wpisy, gdzie w tablicy Rejestry_Broni jest element z data_wyrejestrowania_broni == null
    var elemFilter = Builders<BsonDocument>.Filter.ElemMatch(
        "Rejestry_Broni",
        Builders<BsonDocument>.Filter.Eq("data_wyrejestrowania_broni", BsonNull.Value));

    var docs = await collection.Find(elemFilter).ToListAsync();

    // Mapa PESEL -> zestaw rodzajów broni, które dana osoba aktualnie posiada
    var peselToKinds = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
    var pesels = new HashSet<string>();

    foreach (var doc in docs)
    {
        // rodzaj_broni jest na poziomie dokumentu – ten sam dla wszystkich wpisów Rejestry_Broni
        string? kind = null;
        if (doc.TryGetValue("rodzaj_broni", out var kindValue) && !kindValue.IsBsonNull)
            kind = kindValue.AsString?.Trim();

        if (!doc.TryGetValue("Rejestry_Broni", out var rejestryValue) || rejestryValue.IsBsonNull)
            continue;

        var rejestry = rejestryValue.AsBsonArray;
        foreach (var re in rejestry)
        {
            if (!re.IsBsonDocument) continue;
            var reDoc = re.AsBsonDocument;

            var dataWyrejestrowania = reDoc.GetValue("data_wyrejestrowania_broni", BsonNull.Value);
            if (!dataWyrejestrowania.IsBsonNull)
                continue; // tylko aktywne wpisy

            if (!reDoc.TryGetValue("Osoby", out var osobyValue) || !osobyValue.IsBsonDocument)
                continue;

            var osobyDoc = osobyValue.AsBsonDocument;
            var peselValue = osobyDoc.GetValue("pesel_osoby", BsonNull.Value);
            if (peselValue.IsBsonNull) continue;

            var pesel = peselValue.AsString?.Trim();
            if (string.IsNullOrWhiteSpace(pesel)) continue;

            pesels.Add(pesel);

            if (!string.IsNullOrWhiteSpace(kind))
            {
                if (!peselToKinds.TryGetValue(pesel, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    peselToKinds[pesel] = set;
                }

                set.Add(kind);
            }
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
INSERT INTO piesp.BronOsoby (BO_PESEL, BO_OPIS)
VALUES (@Pesel, @Opis);";

    var rows = pesels.Select(p =>
    {
        peselToKinds.TryGetValue(p, out var kinds);
        var opis = kinds is { Count: > 0 } ? string.Join(", ", kinds) : null;
        return new { Pesel = p, Opis = opis };
    });
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

    // Po załadowaniu adresów przepisz opis z piesp.BronOsoby.BO_OPIS do piesp.BronAdresy.BA_OPIS według PESEL
    const string updateOpisSql = @"
UPDATE ba
SET BA_OPIS = bo.BO_OPIS
FROM piesp.BronAdresy AS ba
INNER JOIN piesp.BronOsoby AS bo
    ON bo.BO_PESEL = ba.BA_BOPESEL;";

    await conn.ExecuteAsync(updateOpisSql, transaction: (IDbTransaction)tx);

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
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("data_wpisania")]
    public DateTime? DataWpisania { get; set; }

    [BsonElement("rodzaj_broni")]
    public string? RodzajBroni { get; set; }

    // Pola techniczne, których nie używamy, ale mapujemy, aby uniknąć błędów serializacji
    [BsonElement("seria_numer_broni")]
    public string? SeriaNumerBroni { get; set; }

    [BsonElement("Rejestry_Broni")]
    public List<RejestrBroniEntry> RejestryBroni { get; set; } = new();
}

 [BsonIgnoreExtraElements]
public sealed class RejestrBroniEntry
{
    [BsonElement("Osoby")]
    public RejestrBroniOsoba? Osoby { get; set; }

    [BsonElement("data_wyrejestrowania_broni")]
    public DateTime? DataWyrejestrowaniaBroni { get; set; }
}

 [BsonIgnoreExtraElements]
public sealed class RejestrBroniOsoba
{
    [BsonElement("pesel_osoby")]
    public string? PeselOsoby { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class OsobaDoc
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("Osoba")]
    public MongoOsoba Osoba { get; set; } = new();

    [BsonElement("Adresy")]
    public List<MongoAdres> Adresy { get; set; } = new();
}

[BsonIgnoreExtraElements]
public sealed class MongoOsoba
{
    [BsonElement("pesel_osoby")]
    public string PeselOsoby { get; set; } = string.Empty;
}

[BsonIgnoreExtraElements]
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
