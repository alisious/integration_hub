// Infrastructure/Audit/SqlAuditSink.cs
using Microsoft.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;


public sealed class SqlAuditSink : IAuditSink
{
    private readonly Channel<object> _channel = Channel.CreateBounded<object>(new BoundedChannelOptions(5000) { FullMode = BoundedChannelFullMode.DropOldest });
    private readonly ILogger<SqlAuditSink> _logger;
    private readonly string _cs;
    private readonly int _batchSize;

    public SqlAuditSink(IConfiguration cfg, ILogger<SqlAuditSink> logger)
    {
        _logger = logger;
        _cs = cfg.GetConnectionString("IntegrationHubDB")
              ?? cfg["AuditLogging:Sql:ConnectionString"]!;
        _batchSize = cfg.GetValue("AuditLogging:Sql:BatchSize", 50);
    }

    public ValueTask Enqueue(ApiRequestLogItem item, CancellationToken ct = default) => _channel.Writer.WriteAsync(item, ct);
    public ValueTask Enqueue(SourceCallLogItem item, CancellationToken ct = default) => _channel.Writer.WriteAsync(item, ct);

    public IAsyncEnumerable<IReadOnlyList<object>> ReadBatchesAsync(CancellationToken ct)
        => ReadBatches(_channel.Reader, _batchSize, ct);

    static async IAsyncEnumerable<IReadOnlyList<object>> ReadBatches(ChannelReader<object> reader, int batchSize, [EnumeratorCancellation] CancellationToken ct)
    {
        var buf = new List<object>(batchSize);
        while (await reader.WaitToReadAsync(ct))
        {
            while (reader.TryRead(out var item))
            {
                buf.Add(item);
                if (buf.Count >= batchSize) { yield return buf.ToArray(); buf.Clear(); }
            }
            if (buf.Count > 0) { yield return buf.ToArray(); buf.Clear(); }
        }
    }

    public sealed class Worker : BackgroundService
    {
        private readonly SqlAuditSink _sink;
        private readonly IConfiguration _cfg;
        private readonly ILogger<Worker> _logger;
        private readonly int _flushMs;

        public Worker(SqlAuditSink sink, IConfiguration cfg, ILogger<Worker> logger)
        {
            _sink = sink; _cfg = cfg; _logger = logger;
            _flushMs = _cfg.GetValue("AuditLogging:Sql:FlushIntervalMs", 1000);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var batch in _sink.ReadBatchesAsync(stoppingToken))
            {
                try
                {
                    using var con = new SqlConnection(_sink._cs);
                    await con.OpenAsync(stoppingToken);

                    using var tr = con.BeginTransaction();
                    foreach (var item in batch)
                    {
                        if (item is ApiRequestLogItem a)
                        {
                            await con.ExecuteAsync(@"
INSERT INTO dbo.ApiRequestLog(CreatedUtc, RequestId, HttpMethod, Path, UserName, UserId, UnitName, ClientIp, StatusCode, ProxyStatus, Source, DurationMs, ErrorMessage, RequestBody, ResponseBody, BodyHash)
VALUES (SYSUTCDATETIME(), @RequestId, @HttpMethod, @Path, @UserName, @UserId, @UnitName, @ClientIp, @StatusCode, @ProxyStatus, @Source, @DurationMs, @ErrorMessage, @RequestBody, @ResponseBody, @BodyHash);", a, tr);
                        }
                        else if (item is SourceCallLogItem s)
                        {
                            await con.ExecuteAsync(@"
INSERT INTO dbo.SourceCallLog(CreatedUtc, RequestId, Source, EndpointUrl, Action, HttpStatus, FaultCode, FaultMessage, DurationMs, ErrorMessage, RequestBody, ResponseBody)
VALUES (SYSUTCDATETIME(), @RequestId, @Source, @EndpointUrl, @Action, @HttpStatus, @FaultCode, @FaultMessage, @DurationMs, @ErrorMessage, @RequestBody, @ResponseBody);", s, tr);
                        }
                    }
                    tr.Commit();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Błąd zapisu batchu audit logów do SQL");
                    await Task.Delay(_flushMs, stoppingToken);
                }
            }
        }
    }
}
