using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

public static class AuditBodyHelper
{
    public static byte[]? PrepareBody(string? payload, IConfiguration cfg)
    {
        if (string.IsNullOrEmpty(payload)) return null;
        if (!cfg.GetValue("AuditLogging:StoreBodies", true)) return null;

        var max = cfg.GetValue("AuditLogging:MaxBodyBytes", 524288);
        var redact = cfg.GetValue("AuditLogging:Redaction:Enabled", true);
        if (redact)
        {
            var patterns = cfg.GetSection("AuditLogging:Redaction:Patterns").Get<string[]>() ?? Array.Empty<string>();
            foreach (var p in patterns)
                payload = Regex.Replace(payload, p, "***", RegexOptions.Compiled);
        }

        var bytes = Encoding.UTF8.GetBytes(payload);
        if (bytes.Length > max) bytes = bytes.Take(max).ToArray();

        if (cfg.GetValue("AuditLogging:CompressBodies", true))
        {
            using var ms = new MemoryStream();
            using (var gzip = new GZipStream(ms, CompressionLevel.Fastest, leaveOpen: true))
                gzip.Write(bytes, 0, bytes.Length);
            return ms.ToArray();
        }
        return bytes;
    }
}
