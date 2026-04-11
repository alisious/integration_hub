// IntegrationHub.Sources.ANPRS/Services/ANPRSDictionaryService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using IntegrationHub.Sources.ANPRS.Client;
using IntegrationHub.Sources.ANPRS.Config;
using IntegrationHub.Sources.ANPRS.Contracts;
using IntegrationHub.Infrastructure.Audit;

namespace IntegrationHub.Sources.ANPRS.Services
{
    public sealed class ANPRSDictionaryService : IANPRSDictionaryService
    {
        private const string Source = "ANPRS";
        private readonly ANPRSHttpClient _client;
        private readonly ANPRSConfig _cfg;
        private readonly ISourceCallAuditor _auditor;

        public ANPRSDictionaryService(ANPRSHttpClient client, ANPRSConfig cfg, ISourceCallAuditor auditor)
        {
            _client = client;
            _cfg = cfg;
            _auditor = auditor;
        }

        public Task<BCPResponse?> GetBCPAsync(CancellationToken ct = default)
        {
            var url = $"{_cfg.DictionaryServiceUrl}?type=bcp";
            return _auditor.InvokeAsync<BCPResponse>(
                Source, url, "GET /Dictionary?type=bcp",
                call: () => _client.GetAsync<BCPResponse>(url, ct),
                ct: ct,
                requestBody: null,
                addOutgoingHeader: id => _client.SetCorrelationIdHeader(id));
        }

        public Task<CountriesResponse?> GetCountriesAsync(CancellationToken ct = default)
        {
            var url = $"{_cfg.DictionaryServiceUrl}?type=countries";
            return _auditor.InvokeAsync<CountriesResponse>(
                Source, url, "GET /Dictionary?type=countries",
                call: () => _client.GetAsync<CountriesResponse>(url, ct),
                ct: ct,
                requestBody: null,
                addOutgoingHeader: id => _client.SetCorrelationIdHeader(id));
        }

        public Task<SystemsResponse?> GetSystemsAsync(string country, CancellationToken ct = default)
        {
            var url = $"{_cfg.DictionaryServiceUrl}/Systems?country={Uri.EscapeDataString(country)}";
            return _auditor.InvokeAsync<SystemsResponse>(
                Source, url, "GET /Dictionary/Systems",
                call: () => _client.GetAsync<SystemsResponse>(url, ct),
                ct: ct,
                requestBody: null,
                addOutgoingHeader: id => _client.SetCorrelationIdHeader(id));
        }

        public Task<BCPResponse?> GetBCPByCountrySystemAsync(string country, string system, CancellationToken ct = default)
        {
            var url = $"{_cfg.DictionaryServiceUrl}/BCP" +
                      $"?country={Uri.EscapeDataString(country)}" +
                      $"&system={Uri.EscapeDataString(system)}";

            return _auditor.InvokeAsync<BCPResponse>(
                Source, url, "GET /Dictionary/BCP",
                call: () => _client.GetAsync<BCPResponse>(url, ct),
                ct: ct,
                requestBody: null,
                addOutgoingHeader: id => _client.SetCorrelationIdHeader(id));
        }
    }
}
