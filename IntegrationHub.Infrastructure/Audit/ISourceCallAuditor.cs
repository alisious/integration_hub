// IntegrationHub.Infrastructure.Audit/ISourceCallAuditor.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationHub.Infrastructure.Audit
{
    public interface ISourceCallAuditor
    {
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
    }
}
