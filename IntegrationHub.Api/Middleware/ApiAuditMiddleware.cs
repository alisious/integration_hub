using IntegrationHub.Infrastructure.Audit;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

public sealed class ApiAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuditSink _sink;
    private readonly IConfiguration _cfg;
    private readonly IRequestContext _requestContext;

    public ApiAuditMiddleware(
        RequestDelegate next,
        IAuditSink sink,
        IConfiguration cfg,
        IRequestContext requestContext)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
    }

    public async Task Invoke(HttpContext ctx)
    {
        var sw = ValueStopwatch.StartNew();

        // 1) RequestId – jeśli jest w nagłówku, użyj go, inaczej wygeneruj
        var requestId = EnsureRequestId(ctx);
       
        // 2) CorrelationId – jeśli jest, użyj; jeśli nie ma, użyj requestId
        var correlationId = GetCorrelationId(ctx);
        
        // 3) Dane użytkownika z claimów
        var userId = GetUserId(ctx.User);
        var userName = ctx.User?.Identity?.Name;
        var unitName = GetUnitName(ctx.User);

        // 4) Wpisanie do RequestContext – od tej chwili cała reszta w scope może tego używać
        _requestContext.RequestId = requestId;
        _requestContext.CorrelationId = correlationId;
        _requestContext.UserId = userId;
        _requestContext.UserDisplayName = userName;
        _requestContext.UserUnitName = unitName;

        // 5) Buforowanie request body (jak dotychczas)
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

            await _sink.Enqueue(new ApiRequestLogItem(
                requestId,
                ctx.Request.Method,
                ctx.Request.Path.Value ?? "/",
                userName,
                GetClientIpv4(ctx),
                ctx.Response.StatusCode,
                proxyStatus,
                source,
                (int)sw.GetElapsedTime().TotalMilliseconds,
                error,
                AuditBodyHelper.PrepareBody(reqBody, _cfg),
                AuditBodyHelper.PrepareBody(respBody, _cfg),
                null,
                UserId: userId,
                UnitName: unitName
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

    static string? GetUserId(ClaimsPrincipal? user)
    {
        if (user is null || user.Identity?.IsAuthenticated != true) return null;
        return user.FindFirst("sub")?.Value
            ?? user.FindFirst("oid")?.Value
            ?? user.FindFirst("uid")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst(ClaimTypes.PrimarySid)?.Value
            ?? user.FindFirst(ClaimTypes.Upn)?.Value;
    }

    static string? GetUnitName(ClaimsPrincipal? user)
    {
        if (user is null || user.Identity?.IsAuthenticated != true) return null;
        return user.FindFirst("unit")?.Value
            ?? user.FindFirst("unit_name")?.Value
            ?? user.FindFirst("UnitName")?.Value
            ?? user.FindFirst("extension_UnitName")?.Value   // AAD custom attr
            ?? user.FindFirst("ou")?.Value
            ?? user.FindFirst("department")?.Value
            ?? user.FindFirst(ClaimTypes.GroupSid)?.Value;
    }

    private static string EnsureRequestId(HttpContext context)
    {
        const string headerName = "X-Request-Id";

        if (context.Request.Headers.TryGetValue(headerName, out StringValues existing) &&
            !StringValues.IsNullOrEmpty(existing))
        {
            // zapewnij, że response też ma ten nagłówek
            context.Response.Headers[headerName] = existing;
            return existing.ToString();
        }

        var generated = Guid.NewGuid().ToString("N");
        context.Request.Headers[headerName] = generated;
        context.Response.Headers[headerName] = generated;
        return generated;
    }

    private static string? GetCorrelationId(HttpContext context)
    {
        const string headerName = "X-Correlation-Id";

        if (context.Request.Headers.TryGetValue(headerName, out var values) &&
            !StringValues.IsNullOrEmpty(values))
        {
            var value = values.ToString();
            context.Response.Headers[headerName] = value;
            return value;
        }

        return null;
    }

}
