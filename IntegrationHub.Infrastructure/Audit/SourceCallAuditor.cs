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
        private readonly IRequestContext _requestContext;

        public SourceCallAuditor(
            IAuditSink audit,
            IConfiguration cfg,
            ILogger<SourceCallAuditor> logger,
            IRequestContext requestContext)
        {
            _audit = audit;
            _cfg = cfg;
            _logger = logger;
            _requestContext = requestContext;
        }


        /// <summary>
        /// Obejmuje audytem wywołanie do zewnętrznego źródła (HTTP, SQL, serwis domenowy itd.),
        /// mierząc czas wykonania, logując błędy i zapisując <see cref="SourceCallLogItem"/> do audytu.
        /// Metoda jest ogólna – może owinąć dowolną operację zwracającą typ <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// Typ wyniku wywoływanej operacji (np. DTO, kolekcja danych, wartość prosta).
        /// </typeparam>
        /// <param name="source">
        /// Kod źródła zewnętrznego (np. "SRP", "CEP", "ZW") – trafia do logu jako identyfikator systemu.
        /// </param>
        /// <param name="endpointUrl">
        /// Adres końcowy wywołania (np. URL usługi HTTP, nazwa procedury SQL) używany w logu audytu.
        /// </param>
        /// <param name="action">
        /// Nazwa operacji wykonywanej na źródle (np. SOAPAction, nazwa metody serwisu, operacja SQL).
        /// </param>
        /// <param name="call">
        /// Funkcja wykonująca właściwe wywołanie do źródła. Jest owijana logiką pomiaru czasu i obsługi błędów.
        /// </param>
        /// <param name="ct">
        /// Token anulowania, przekazywany do wywoływanej operacji oraz używany przy zapisie logu.
        /// </param>
        /// <param name="requestBody">
        /// Opcjonalna treść żądania w formie tekstowej, która zostanie zapisana w logu zgodnie z polityką audytu.
        /// </param>
        /// <param name="expectedHttpOk">
        /// Oczekiwany kod powodzenia (np. 200 dla HTTP). Dla wywołań nie-HTTP może pozostać wartością domyślną.
        /// </param>
        /// <param name="addOutgoingHeader">
        /// Opcjonalny delegat pozwalający wstrzyknąć nagłówek korelacyjny (np. X-Correlation-Id) do wywołania.
        /// </param>
        /// <returns>
        /// Wynik wywoływanej operacji typu <typeparamref name="T"/>. W przypadku błędu metoda loguje zdarzenie
        /// i ponownie rzuca wyjątek.
        /// </returns>
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
            // Jeśli jesteśmy w kontekście HTTP – użyj RequestId z ApiAuditMiddleware,
            // inaczej wygeneruj nowy (np. dla jobów/background service).
            var requestId = _requestContext.RequestId ?? Guid.NewGuid().ToString("N");

            using var _scope = _logger.BeginScope(new System.Collections.Generic.Dictionary<string, object?>
            {
                ["Source"] = source,
                ["RequestId"] = requestId,
                ["Action"] = action
            });

            // Correlation-ID do propagacji – jeśli mamy w kontekście, użyj go, inaczej użyj requestId
            var correlationId = _requestContext.CorrelationId ?? requestId;
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
            catch (SourceHttpException ex)
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

        /// <summary>
        /// Specjalizowana wersja audytu dla wywołań HTTP. Obejmuje audytem wywołanie
        /// zwracające <see cref="HttpResponseMessage"/>, mierzy czas, loguje status HTTP,
        /// treść odpowiedzi (zgodnie z polityką audytu) oraz zapisuje <see cref="SourceCallLogItem"/>.
        /// </summary>
        /// <param name="source">
        /// Kod źródła zewnętrznego (np. "SRP", "CEP") – identyfikator systemu w logach.
        /// </param>
        /// <param name="endpointUrl">
        /// Pełny adres URL wywołania HTTP, zapisywany w logu audytu.
        /// </param>
        /// <param name="action">
        /// Nazwa operacji HTTP, zwykle SOAPAction lub kombinacja metody i ścieżki (np. "POST /api/...").
        /// </param>
        /// <param name="call">
        /// Funkcja wykonująca faktyczne wywołanie HTTP (np. <c>() =&gt; base.SendAsync(request, ct)</c>).
        /// Metoda owija to wywołanie logiką audytową.
        /// </param>
        /// <param name="ct">
        /// Token anulowania przekazywany do wywołania HTTP oraz używany przy zapisie logu.
        /// </param>
        /// <param name="requestBody">
        /// Opcjonalna treść żądania HTTP w formie tekstowej, zapisywana w logu zgodnie z polityką audytu.
        /// </param>
        /// <param name="addOutgoingHeader">
        /// Opcjonalny delegat ustawiający nagłówek korelacyjny (np. X-Correlation-Id) na żądaniu HTTP.
        /// </param>
        /// <returns>
        /// Odpowiedź HTTP (<see cref="HttpResponseMessage"/>). Metoda zawsze loguje wynik (sukces lub błąd)
        /// i ponownie rzuca wyjątek w przypadku błędu wykonania lub anulowania.
        /// </returns>
        public async Task<HttpResponseMessage> InvokeHttpAsync(
            string source,
            string endpointUrl,
            string action,
            Func<Task<HttpResponseMessage>> call,
            CancellationToken ct,
            string? requestBody = null,
            Action<string>? addOutgoingHeader = null)
        {
            var requestId = _requestContext.RequestId ?? Guid.NewGuid().ToString("N");

            using var _scope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["Source"] = source,
                ["RequestId"] = requestId,
                ["Action"] = action
            });

            // Correlation-ID do propagacji (np. X-Correlation-Id)
            var correlationId = _requestContext.CorrelationId ?? requestId;
            addOutgoingHeader?.Invoke(correlationId);

            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("HTTP start {Source} {Action} -> {Endpoint}", source, action, endpointUrl);
                var resp = await call();

                sw.Stop();

                var status = (int)resp.StatusCode;
                string? errorMessage = null;

                if (resp.IsSuccessStatusCode)
                {
                    _logger.LogInformation("HTTP done {Source} {Action} ({Status}) in {Elapsed} ms",
                        source, action, status, sw.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogError("HTTP error {Source} {Action} ({Status}) in {Elapsed} ms",
                        source, action, status, sw.ElapsedMilliseconds);
                    errorMessage = $"HTTP {status} {resp.ReasonPhrase}";
                }

                string? responseBodyRaw = null;
                if (resp.Content is not null)
                {
                    // nie zmieniamy semantyki response – tylko czytamy do audytu
                    responseBodyRaw = await resp.Content.ReadAsStringAsync(ct);
                }

                await _audit.Enqueue(new SourceCallLogItem(
                    RequestId: requestId,
                    Source: source,
                    EndpointUrl: endpointUrl,
                    Action: action,
                    HttpStatus: status,
                    FaultCode: null,
                    FaultMessage: null,
                    DurationMs: (int)sw.ElapsedMilliseconds,
                    ErrorMessage: errorMessage,
                    RequestBody: AuditBodyHelper.PrepareBody(requestBody, _cfg),
                    ResponseBody: AuditBodyHelper.PrepareBody(responseBodyRaw, _cfg)
                ), ct);

                return resp;
            }
            catch (TaskCanceledException tce) when (!ct.IsCancellationRequested)
            {
                sw.Stop();
                _logger.LogError(tce, "HTTP canceled {Source} {Action}", source, action);

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
                _logger.LogError(ex, "Unexpected HTTP error {Source} {Action}", source, action);

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
