// Infrastructure/Audit/AuditModels.cs
public sealed record ApiRequestLogItem(
    string? RequestId, string HttpMethod, string Path, string? UserName, string? ClientIp,
    int StatusCode, int? ProxyStatus, string? Source, int DurationMs, string? ErrorMessage,
    byte[]? RequestBody, byte[]? ResponseBody, byte[]? BodyHash,string? UserId = null,string? UnitName = null
    );

public sealed record SourceCallLogItem(
    string? RequestId, string Source, string EndpointUrl, string? Action,
    int? HttpStatus, string? FaultCode, string? FaultMessage, int DurationMs,
    string? ErrorMessage, byte[]? RequestBody, byte[]? ResponseBody);

// Infrastructure/Audit/IAuditSink.cs
public interface IAuditSink
{
    ValueTask Enqueue(ApiRequestLogItem item, CancellationToken ct = default);
    ValueTask Enqueue(SourceCallLogItem item, CancellationToken ct = default);
}
