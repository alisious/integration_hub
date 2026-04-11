// IntegrationHub.Sources.ANPRS/Client/ANPRSHttpClient.cs
using IntegrationHub.Infrastructure.Exceptions;
using IntegrationHub.Sources.ANPRS.Config;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IntegrationHub.Sources.ANPRS.Client
{
    public sealed record HttpJsonResult<T>(
        T? Body,
        IReadOnlyDictionary<string, IEnumerable<string>> Headers,
        IReadOnlyDictionary<string, IEnumerable<string>> ContentHeaders
    );

    public sealed class ANPRSHttpClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<ANPRSHttpClient> _logger;
        public ANPRSConfig Config { get; }

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly AsyncLocal<string?> _nextCorrelationId = new(); // jednorazowy nagłówek dla kolejnego requestu

        public ANPRSHttpClient(HttpClient httpClient, ANPRSConfig cfg, ILogger<ANPRSHttpClient> logger)
        {
            _http = httpClient;
            Config = cfg;
            _logger = logger;

            // Authorization: Basic base64("user:pass")
            var authPlain = $"{Config.ANPRSUserID}:{Config.ANPRSPassword}";
            var authenticationHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authPlain));

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", authenticationHeaderValue);
        }

        /// <summary>Ustawia X-Correlation-ID dla najbliższego wywołania HTTP.</summary>
        public void SetCorrelationIdHeader(string id) => _nextCorrelationId.Value = id;

        public async Task<T?> GetAsync<T>(
            string url,
            CancellationToken ct = default,
            params (string Name, string Value)[] extraHeaders)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);

            // CorrID – jednorazowo
            if (!string.IsNullOrWhiteSpace(_nextCorrelationId.Value))
            {
                req.Headers.TryAddWithoutValidation("X-Correlation-ID", _nextCorrelationId.Value);
                _nextCorrelationId.Value = null;
            }

            // Dodatkowe nagłówki
            foreach (var (n, v) in extraHeaders)
                req.Headers.TryAddWithoutValidation(n, v);

            using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
            {
                throw new ANPRSHttpException((int)res.StatusCode, body);
            }

            if (string.IsNullOrWhiteSpace(body))
                return default;

            return JsonSerializer.Deserialize<T>(body, JsonOpts);
        }

        public async Task<HttpJsonResult<T>> GetWithHeadersAsync<T>(
            string url,
            CancellationToken ct = default,
            params (string Name, string Value)[] extraHeaders)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);

            if (!string.IsNullOrWhiteSpace(_nextCorrelationId.Value))
            {
                req.Headers.TryAddWithoutValidation("X-Correlation-ID", _nextCorrelationId.Value);
                _nextCorrelationId.Value = null;
            }

            foreach (var (n, v) in extraHeaders)
                req.Headers.TryAddWithoutValidation(n, v);

            using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

            // Zapisz nagłówki zanim przeczytasz body
            var headers = res.Headers.ToDictionary(h => h.Key, h => h.Value);
            var contentHeaders = res.Content.Headers.ToDictionary(h => h.Key, h => h.Value);

            var bodyStr = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new ANPRSHttpException((int)res.StatusCode, bodyStr);

            var body = string.IsNullOrWhiteSpace(bodyStr)
                ? default
                : JsonSerializer.Deserialize<T>(bodyStr, JsonOpts);

            return new HttpJsonResult<T>(body, headers, contentHeaders);
        }

    }

    public sealed class ANPRSHttpException : SourceHttpException
    {
        public ANPRSHttpException(int statusCode, string? content)
           : base(statusCode, content, message: $"ANPRS HTTP {statusCode}.")
        {
        }
    }
}
