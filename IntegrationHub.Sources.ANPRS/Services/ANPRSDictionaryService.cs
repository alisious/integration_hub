using System;
using System.Threading;
using System.Threading.Tasks;
using IntegrationHub.Sources.ANPRS.Client;
using IntegrationHub.Sources.ANPRS.Config;
using IntegrationHub.Sources.ANPRS.Contracts;

namespace IntegrationHub.Sources.ANPRS.Services
{
    public interface IANPRSDictionaryService
    {
        /// <summary>type: "bcp" lub "countries".</summary>
        Task<DictionaryResponse?> GetDictionaryAsync(string type, CancellationToken ct = default);

        /// <summary>/Dictionary/Systems?country=PLN</summary>
        Task<SystemsResponse?> GetSystemsAsync(string country, CancellationToken ct = default);

        /// <summary>/Dictionary/BCP?country=PLN&system=OCR</summary>
        Task<BCPResponse?> GetBCPAsync(string country, string system, CancellationToken ct = default);
    }

    public sealed class ANPRSDictionaryService : IANPRSDictionaryService
    {
        private readonly ANPRSHttpClient _client;
        private readonly ANPRSConfig _cfg;

        public ANPRSDictionaryService(ANPRSHttpClient client, ANPRSConfig cfg)
        {
            _client = client;
            _cfg = cfg;
        }

        public Task<DictionaryResponse?> GetDictionaryAsync(string type, CancellationToken ct = default)
        {
            var url = $"{_cfg.DictionaryServiceUrl}?type={Uri.EscapeDataString(type)}";
            return _client.GetAsync<DictionaryResponse>(url, ct);
        }

        public Task<SystemsResponse?> GetSystemsAsync(string country, CancellationToken ct = default)
        {
            var url = $"{_cfg.DictionaryServiceUrl}/Systems?country={Uri.EscapeDataString(country)}";
            return _client.GetAsync<SystemsResponse>(url, ct);
        }

        public Task<BCPResponse?> GetBCPAsync(string country, string system, CancellationToken ct = default)
        {
            var url =
                $"{_cfg.DictionaryServiceUrl}/BCP" +
                $"?country={Uri.EscapeDataString(country)}" +
                $"&system={Uri.EscapeDataString(system)}";

            return _client.GetAsync<BCPResponse>(url, ct);
        }
    }
}
