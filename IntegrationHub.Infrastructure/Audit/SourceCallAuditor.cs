// IntegrationHub.Infrastructure.Audit/SourceCallAuditor.cs
using IntegrationHub.Infrastructure.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace IntegrationHub.Infrastructure.Audit
{
    public sealed class SourceCallAuditor : ISourceCallAuditor
    {
        private readonly IAuditSink _audit;
        private readonly IConfiguration _cfg;
        private readonly ILogger<SourceCallAuditor> _logger;

        public SourceCallAuditor(IAuditSink audit, IConfiguration cfg, ILogger<SourceCallAuditor> logger)
        {
            _audit = audit;
            _cfg = cfg;
            _logger = logger;
        }

        public async Task<T?> InvokeAsync<T>(
            string source,
            string endpointUrl,
            string action,
            Func<Task<T?>> call,
            CancellationToken ct,
            string? requestBody = null,
            int? expectedHttpOk = 200,
            Action<string>? addOutgoingHeader = null)
        {
            var requestId = Guid.NewGuid().ToString("N");
            using var _scope = _logger.BeginScope(new System.Collections.Generic.Dictionary<string, object?>
            {
                ["Source"] = source,
                ["RequestId"] = requestId,
                ["Action"] = action
            });

            // Correlation-ID do propagacji (np. w ANPRSHttpClient ustaw w delegacie)
            var correlationId = requestId;
            addOutgoingHeader?.Invoke(correlationId);

            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Start {Source} {Action} -> {Endpoint}", source, action, endpointUrl);
                var resp = await call();

                sw.Stop();
                _logger.LogInformation("Done {Source} {Action} ({Elapsed} ms)", source, action, sw.ElapsedMilliseconds);

                string? responseBodyRaw = resp switch
                {
                    null => null,
                    string s => s,
                    _ => JsonSerializer.Serialize(resp)
                };

                await _audit.Enqueue(new SourceCallLogItem(
                    RequestId: requestId,
                    Source: source,
                    EndpointUrl: endpointUrl,
                    Action: action,
                    HttpStatus: expectedHttpOk,
                    FaultCode: null,
                    FaultMessage: null,
                    DurationMs: (int)sw.ElapsedMilliseconds,
                    ErrorMessage: null,
                    RequestBody: AuditBodyHelper.PrepareBody(requestBody, _cfg),
                    ResponseBody: AuditBodyHelper.PrepareBody(responseBodyRaw, _cfg)
                ), ct);

                return resp;
            }
            catch (SourceHttpException ex) // dostosuj jeśli masz inną klasę wyjątków HTTP
            {
                sw.Stop();
                _logger.LogError(ex, "HTTP error {Source} {Action} ({Status})", source, action, (int?)ex.StatusCode);

                await _audit.Enqueue(new SourceCallLogItem(
                    RequestId: requestId,
                    Source: source,
                    EndpointUrl: endpointUrl,
                    Action: action,
                    HttpStatus: (int?)ex.StatusCode,
                    FaultCode: null,
                    FaultMessage: null,
                    DurationMs: (int)sw.ElapsedMilliseconds,
                    ErrorMessage: ex.Message,
                    RequestBody: AuditBodyHelper.PrepareBody(requestBody, _cfg),
                    ResponseBody: AuditBodyHelper.PrepareBody(ex.ResponseBody, _cfg)
                ), ct);

                throw;
            }
            catch (TaskCanceledException tce) when (!ct.IsCancellationRequested)
            {
                sw.Stop();
                _logger.LogError(tce, "Canceled {Source} {Action}", source, action);

                await _audit.Enqueue(new SourceCallLogItem(
                    RequestId: requestId,
                    Source: source,
                    EndpointUrl: endpointUrl,
                    Action: action,
                    HttpStatus: null,
                    FaultCode: null,
                    FaultMessage: null,
                    DurationMs: (int)sw.ElapsedMilliseconds,
                    ErrorMessage: "Canceled by caller",
                    RequestBody: AuditBodyHelper.PrepareBody(requestBody, _cfg),
                    ResponseBody: null
                ), ct);

                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Unexpected {Source} {Action}", source, action);

                await _audit.Enqueue(new SourceCallLogItem(
                    RequestId: requestId,
                    Source: source,
                    EndpointUrl: endpointUrl,
                    Action: action,
                    HttpStatus: null,
                    FaultCode: null,
                    FaultMessage: null,
                    DurationMs: (int)sw.ElapsedMilliseconds,
                    ErrorMessage: ex.Message,
                    RequestBody: AuditBodyHelper.PrepareBody(requestBody, _cfg),
                    ResponseBody: null
                ), ct);

                throw;
            }
        }
    }
}
