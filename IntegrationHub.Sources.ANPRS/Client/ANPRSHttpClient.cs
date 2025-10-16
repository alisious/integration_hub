// IntegrationHub.Sources.ANPRS/Client/ANPRSHttpClient.cs
using IntegrationHub.Sources.ANPRS.Config;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IntegrationHub.Sources.ANPRS.Client
{
    public class ANPRSHttpClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<ANPRSHttpClient> _logger;
        public ANPRSConfig Config { get; }

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ANPRSHttpClient(HttpClient httpClient, ANPRSConfig cfg, ILogger<ANPRSHttpClient> logger)
        {
            _http = httpClient;
            Config = cfg;
            _logger = logger;

            // Authorization: Basic base64("user:pass") – identycznie jak w SRP
            var authPlain = $"{Config.ANPRSUserID}:{Config.ANPRSPassword}";
            var authenticationHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authPlain));
            _logger.LogInformation($"ANPRS authentication header Basic {authenticationHeaderValue}");
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", authenticationHeaderValue);
        }

        public async Task<T?> GetAsync<T>(string relativeUrl, CancellationToken ct = default, params (string Key, string Value)[] headers)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            foreach (var (k, v) in headers)
                req.Headers.TryAddWithoutValidation(k, v);

            using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                throw new ANPRSHttpException((int)res.StatusCode, body);
            }

            var json = await res.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(json)) return default;

            return JsonSerializer.Deserialize<T>(json, JsonOpts);
        }
    }

    public sealed class ANPRSHttpException : Exception
    {
        public int StatusCode { get; }
        public ANPRSHttpException(int statusCode, string? content)
            : base($"ANPRS HTTP {statusCode}. Body: {content}") => StatusCode = statusCode;
    }
}
