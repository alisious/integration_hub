using System.Text;
using System.Text.Json;
using IntegrationHub.Infrastructure.Audit;

public sealed class ApiAuditMiddleware
{
    private readonly RequestDelegate _next;
    public ApiAuditMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx, IAuditSink sink, IConfiguration cfg)
    {
        var sw = ValueStopwatch.StartNew();
        var requestId = ctx.Request.Headers["X-Request-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");

        ctx.Request.EnableBuffering();
        string? reqBody = null;
        if (ctx.Request.ContentLength is > 0 && (ctx.Request.ContentType?.Contains("json") ?? false))
        {
            using var sr = new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true);
            reqBody = await sr.ReadToEndAsync();
            ctx.Request.Body.Position = 0;
        }

        var originalBody = ctx.Response.Body;
        await using var mem = new MemoryStream();
        ctx.Response.Body = mem;

        try
        {
            await _next(ctx);
            mem.Position = 0;
            string? respBody = null;
            if (ctx.Response.ContentType?.Contains("json") ?? false)
                respBody = await new StreamReader(mem, Encoding.UTF8).ReadToEndAsync();

            var proxyStatus = TryReadProxyStatus(respBody);
            var source = TryReadSource(respBody);
            var error = TryReadErrorMessage(respBody);

            await sink.Enqueue(new ApiRequestLogItem(
                requestId,
                ctx.Request.Method,
                ctx.Request.Path.Value ?? "/",
                ctx.User?.Identity?.Name,
                ctx.Connection.RemoteIpAddress?.ToString(),
                ctx.Response.StatusCode,
                proxyStatus,
                source,
                (int)sw.GetElapsedTime().TotalMilliseconds,
                error,
                AuditBodyHelper.PrepareBody(reqBody, cfg),
                AuditBodyHelper.PrepareBody(respBody, cfg),
                null
            ));

            mem.Position = 0;
            await mem.CopyToAsync(originalBody);
        }
        finally
        {
            ctx.Response.Body = originalBody;
        }
    }

    static int? TryReadProxyStatus(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("status", out var st) && st.ValueKind is JsonValueKind.Number)
                return st.GetInt32();
        }
        catch { }
        return null;
    }
    static string? TryReadSource(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("source", out var s) ? s.GetString() : null;
        }
        catch { return null; }
    }
    static string? TryReadErrorMessage(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("errorMessage", out var e) ? e.GetString() : null;
        }
        catch { return null; }
    }
}


