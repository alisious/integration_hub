using IntegrationHub.Sources.ANPRS.Client;      // ANPRSHttpException
using IntegrationHub.Sources.ANPRS.Contracts;   // BCPResponse, CountriesResponse, SystemsResponse
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IntegrationHub.Sources.ANPRS.Services
{
    /// <summary>
    /// Testowy serwis słowników ANPRS – czyta JSON-y z katalogu: &lt;ContentRoot&gt;\TestData\ANPRS
    /// Pliki:
    /// - GetBCPAsync_RESPONSE.json
    /// - GetCountriesAsync_RESPONSE.json
    /// - GetSystemsAsync_PLN_RESPONSE.json / _EST_ / _LTV_ / _LVA_
    /// </summary>
   

    public sealed class ANPRSDictionaryServiceTest : IANPRSDictionaryService
    {
        private readonly ILogger<ANPRSDictionaryServiceTest> _logger;
        private readonly string _testDataDir;
        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        public ANPRSDictionaryServiceTest(ILogger<ANPRSDictionaryServiceTest> logger, IHostEnvironment env)
        {
            _logger = logger;
            // Wzorzec jak w CEPSlownikiServiceTest: <contentRoot>\TestData\<DOSTAWCA>
            _testDataDir = Path.Combine(env.ContentRootPath, "TestData", "ANPRS"); // …\TestData\ANPRS
        }

        public Task<BCPResponse?> GetBCPAsync(CancellationToken ct = default) =>
            Load<BCPResponse>("GetBCPAsync_RESPONSE.json", ct);

        public Task<CountriesResponse?> GetCountriesAsync(CancellationToken ct = default) =>
            Load<CountriesResponse>("GetCountriesAsync_RESPONSE.json", ct);

        public Task<SystemsResponse?> GetSystemsAsync(string country, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(country))
                throw new ANPRSHttpException(400, "Body: {\"message\":\"Niepoprawny parametr zapytania (musi być podany)!\"}");

            var key = country.Trim().ToUpperInvariant();
            var file = key switch
            {
                "PLN" => "GetSystemsAsync_PLN_RESPONSE.json",
                "EST" => "GetSystemsAsync_EST_RESPONSE.json",
                "LTV" => "GetSystemsAsync_LTV_RESPONSE.json",
                "LVA" => "GetSystemsAsync_LVA_RESPONSE.json",
                _ => null
            };

            if (file is null)
                throw new ANPRSHttpException(404, $"Body: {{\"message\":\"Brak danych dla country={country}.\"}}");

            return Load<SystemsResponse>(file, ct);
        }

        public async Task<BCPResponse?> GetBCPByCountrySystemAsync(string country, string system, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(system))
                throw new ANPRSHttpException(400, "Body: {\"message\":\"Niepoprawny parametr zapytania (musi być podany)!\"}");

            var src = await GetBCPAsync(ct).ConfigureAwait(false);
            if (src is null) return null;

            // Zgodnie ze specyfikacją BCP to siatka columnsNames + data. Nagłówki mogą mieć różny porządek,
            // dlatego wyznaczamy indeksy po nazwach (case-insensitive). 
            var cols = Index(src.ColumnsNames);
            int iCountry = FirstIndex(cols, "kraj", "country", "code", "countrycode", "iso3");
            int iSystem = FirstIndex(cols, "system", "systemcode");
            // Uwaga: w niektórych materiałach kolejność pól to [Kraj, Nazwa, BCPID, System, Typ, Lat, Lon]. Pliki testowe są zgodne. 

            var wantCountry = country.Trim().ToUpperInvariant();
            var wantSystem = system.Trim().ToUpperInvariant();

            var filtered = new List<List<string>>();
            foreach (var row in src.Data ?? Enumerable.Empty<List<string>>())
            {
                if (ct.IsCancellationRequested) break;
                var rCountry = Get(row, iCountry)?.Trim().ToUpperInvariant();
                var rSystem = Get(row, iSystem)?.Trim().ToUpperInvariant();
                if (rCountry == wantCountry && rSystem == wantSystem)
                    filtered.Add(row);
            }

            return new BCPResponse
            {
                ColumnsNames = src.ColumnsNames?.ToList() ?? new List<string>(),
                Data = filtered
            };
        }

     
        

        // ------------ helpers ------------

        private async Task<T?> Load<T>(string fileName, CancellationToken ct)
        {
            var path = Path.Combine(_testDataDir, fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Brak pliku z danymi testowymi: {path}");

            _logger.LogInformation("ANPRS działa w TRYBIE TESTOWYM – wczytuję {Path}", path);
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(fs, _json, ct).ConfigureAwait(false);
        }

        private static Dictionary<string, int> Index(IEnumerable<string> names) =>
            names.Select((n, i) => new { n, i })
                 .GroupBy(x => (x.n ?? string.Empty).Trim().ToLowerInvariant())
                 .ToDictionary(g => g.Key, g => g.First().i);

        private static int FirstIndex(Dictionary<string, int> dict, params string[] keys)
        {
            foreach (var k in keys)
                if (dict.TryGetValue(k, out var i)) return i;
            return -1;
        }

        private static string? Get(IReadOnlyList<string> row, int i) =>
            i >= 0 && i < row.Count ? row[i] : null;
    }
}
