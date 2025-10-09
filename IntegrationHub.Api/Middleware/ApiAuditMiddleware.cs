using IntegrationHub.Infrastructure.Audit;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

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
                GetClientIpv4(ctx),
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

    static string? GetClientIpv4(HttpContext ctx)
    {
        // 1) za proxy: bierz pierwszy IP z X-Forwarded-For
        var xff = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(xff))
        {
            var first = xff.Split(',')[0].Trim();
            if (IPAddress.TryParse(first, out var xffIp))
            {
                if (xffIp.AddressFamily == AddressFamily.InterNetworkV6 && xffIp.IsIPv4MappedToIPv6)
                    xffIp = xffIp.MapToIPv4();

                if (xffIp.AddressFamily == AddressFamily.InterNetwork) // IPv4
                    return xffIp.ToString();
            }
        }

        // 2) bez proxy: użyj RemoteIpAddress z mapowaniem gdy to IPv4-mapped
        var ip = ctx.Connection.RemoteIpAddress;
        if (ip is null) return null;

        if (ip.AddressFamily == AddressFamily.InterNetworkV6 && ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();

        // jeśli to prawdziwy IPv6 (nie IPv4-mapped), nie ma gwarantowanego IPv4
        return ip.AddressFamily == AddressFamily.InterNetwork ? ip.ToString() : ip.ToString();
    }
}


