// IntegrationHub.Infrastructure.Audit/ISourceCallAuditor.cs
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationHub.Infrastructure.Audit
{
    public interface ISourceCallAuditor
    {
        // 1) Ogólny wariant (do SQL/domeny itp.) – zostawiamy jak jest
        Task<T?> InvokeAsync<T>(
            string source,
            string endpointUrl,
            string action,
            Func<Task<T?>> call,
            CancellationToken ct,
            string? requestBody = null,
            int? expectedHttpOk = 200,
            Action<string>? addOutgoingHeader = null // np. doda X-Correlation-ID
        );

        // 2) Wariant HTTP – używany z DelegatingHandler (SourceAuditHandler)
        Task<HttpResponseMessage> InvokeHttpAsync(
            string source,
            string endpointUrl,
            string action,
            Func<Task<HttpResponseMessage>> call,
            CancellationToken ct,
            string? requestBody = null,
            Action<string>? addOutgoingHeader = null
        );
    }
}
