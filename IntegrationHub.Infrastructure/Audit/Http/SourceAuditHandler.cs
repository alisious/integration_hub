// IntegrationHub.Infrastructure.Audit.Http/SourceAuditHandler.cs
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IntegrationHub.Infrastructure.Audit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IntegrationHub.Infrastructure.Audit.Http
{
    /// <summary>
    /// DelegatingHandler, który owija wszystkie wywołania HTTP do danego źródła
    /// w ISourceCallAuditor – bez ręcznego _audit.Enqueue w serwisach.
    /// </summary>
    public sealed class SourceAuditHandler : DelegatingHandler
    {
        private readonly string _sourceName;
        private readonly ISourceCallAuditor _auditor;
        private readonly IConfiguration _cfg;
        private readonly ILogger<SourceAuditHandler> _logger;

        public SourceAuditHandler(
            string sourceName,
            ISourceCallAuditor auditor,
            IConfiguration cfg,
            ILogger<SourceAuditHandler> logger)
        {
            _sourceName = sourceName;
            _auditor = auditor;
            _cfg = cfg;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken ct)
        {
            // 1. Odczytujemy body requestu (jeśli jest) – tylko do audytu
            string? requestBody = null;
            if (request.Content is not null)
            {
                try
                {
                    requestBody = await request.Content.ReadAsStringAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Nie udało się odczytać body requestu dla audytu ({Source})", _sourceName);
                }
            }

            var endpoint = request.RequestUri?.ToString() ?? "(null)";
            // Dla SOAP: action = SOAPAction; dla reszty – metoda HTTP
            var action = request.Headers.TryGetValues("SOAPAction", out var soapActions)
                ? soapActions.FirstOrDefault() ?? request.Method.Method
                : request.Method.Method;

            // 2. Używamy wariantu HTTP audytora – on liczy czas, loguje, zapisuje do SQL
            var response = await _auditor.InvokeHttpAsync(
                _sourceName,
                endpoint,
                action,
                async () =>
                {
                    // Dodawanie nagłówka korelacyjnego – obsługiwane w audytorze przez delegata
                    return await base.SendAsync(request, ct);
                },
                ct,
                requestBody,
                addOutgoingHeader: correlationId =>
                {
                    const string headerName = "X-Correlation-Id";
                    if (!request.Headers.Contains(headerName))
                    {
                        request.Headers.Add(headerName, correlationId);
                    }
                });

            return response;
        }
    }
}
