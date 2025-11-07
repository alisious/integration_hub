using IntegrationHub.Common.Contracts;
using IntegrationHub.Common.Interfaces; // IClientCertificateProvider
using IntegrationHub.Common.RequestValidation;
using IntegrationHub.Sources.KSIP.Config;
using IntegrationHub.Sources.KSIP.Contracts;
using IntegrationHub.Sources.KSIP.Helpers;
using IntegrationHub.Sources.KSIP.Mappers;
using IntegrationHub.Sources.KSIP.RequestValidation;
using IntegrationHub.Sources.KSIP.SprawdzenieOsobyWRDService;
using IntegrationHub.Infrastructure.Audit;                // IAuditSink, SourceCallLogItem, AuditBodyHelper
using Microsoft.Extensions.Configuration;               // IConfiguration
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;                               // Stopwatch
using System.IO;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace IntegrationHub.Sources.KSIP.Services
{
    public interface IKSIPService
    {
        Task<ProxyResponse<SprawdzenieOsobyWRuchuDrogowymResponse>> SprawdzenieOsobyWRuchuDrogowymAsync(
            SprawdzenieOsobyWRuchuDrogowymRequest body,
            string requestId,
            CancellationToken ct = default);
    }

    public sealed class KSIPService : IKSIPService
    {
        private const string SourceName = "KSIP";
        private const string ActionName = "SprawdzenieOsobyWRD";   // nazwa akcji do audit logów

        private readonly KSIPConfig _cfg;
        private readonly ILogger<KSIPService> _logger;
        private readonly IRequestValidator<SprawdzenieOsobyWRuchuDrogowymRequest> _validator;
        private readonly IClientCertificateProvider _certProvider;

        private readonly IAuditSink _audit;                        // <— AUDYT
        private readonly IConfiguration _configuration;            // <— do AuditBodyHelper.PrepareBody

        public KSIPService(
            IOptions<KSIPConfig> cfg,
            ILogger<KSIPService> logger,
            IClientCertificateProvider certProvider,
            IAuditSink audit,                                      // <— AUDYT
            IConfiguration configuration)                          // <— do PrepareBody
        {
            _cfg = cfg.Value ?? throw new ArgumentNullException(nameof(cfg));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validator = new SprawdzenieOsobyWRuchuDrogowymRequestValidator();
            _certProvider = certProvider ?? throw new ArgumentNullException(nameof(certProvider));
            _audit = audit ?? throw new ArgumentNullException(nameof(audit));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        private static ProxyResponse<T> FromValidation<T>(ProxyResponse<object> vr, string requestId) =>
            new()
            {
                RequestId = requestId,
                Source = SourceName,
                Status = vr.Status,
                SourceStatusCode = vr.SourceStatusCode,
                Message = vr.Message
            };

        public async Task<ProxyResponse<SprawdzenieOsobyWRuchuDrogowymResponse>> SprawdzenieOsobyWRuchuDrogowymAsync(
            SprawdzenieOsobyWRuchuDrogowymRequest body,
            string requestId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(requestId)) requestId = Guid.NewGuid().ToString("N");

            // 1) Walidacja
            var vr = _validator.ValidateAndNormalize(body);
            if (!vr.IsValid)
            {
                var baseResp = vr.ToProxyResponse(requestId);
                return FromValidation<SprawdzenieOsobyWRuchuDrogowymResponse>(baseResp, requestId);
            }

            // 2) Endpoint
            var endpointUrl = _cfg.TestMode ? _cfg.TestSprawdzenieOsobyRDServiceUrl : _cfg.SprawdzenieOsobyRDServiceUrl;
            if (string.IsNullOrWhiteSpace(endpointUrl))
                return ProxyResponses.TechnicalError<SprawdzenieOsobyWRuchuDrogowymResponse>(
                    "Brak adresu usługi KSIP w konfiguracji.",
                    SourceName, ((int)HttpStatusCode.InternalServerError).ToString(), requestId);

            // 3) Binding HTTPS + cert klienta
            var binding = new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.Transport)
            {
                AllowCookies = false,
                MaxBufferSize = int.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferPoolSize = int.MaxValue,
                OpenTimeout = TimeSpan.FromSeconds(Math.Max(5, _cfg.TimeoutSeconds)),
                CloseTimeout = TimeSpan.FromSeconds(Math.Max(5, _cfg.TimeoutSeconds)),
                SendTimeout = TimeSpan.FromSeconds(Math.Max(5, _cfg.TimeoutSeconds)),
                ReceiveTimeout = TimeSpan.FromSeconds(Math.Max(5, _cfg.TimeoutSeconds)),
            };
            binding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Certificate;
            binding.ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max;

            var address = new System.ServiceModel.EndpointAddress(endpointUrl);
            var client = new PersonServiceClient(binding, address);

            // 4) Przygotowanie (cert, trust)
            try
            {
                var clientCert = _certProvider.GetClientCertificate(_cfg);
                client.ClientCredentials.ClientCertificate.Certificate = clientCert;

                if (_cfg.TrustServerCertificate)
                {
                    System.Net.ServicePointManager.ServerCertificateValidationCallback = (_, __, ___, ____) => true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd konfiguracji WCF klienta KSIP. RID={RequestId}", requestId);

                // AUDYT: błąd konfiguracji klienta, jeszcze przed wywołaniem
                await _audit.Enqueue(new SourceCallLogItem(
                    RequestId: requestId,
                    Source: SourceName,
                    EndpointUrl: endpointUrl,
                    Action: ActionName,
                    HttpStatus: null,
                    FaultCode: null,
                    FaultMessage: null,
                    DurationMs: 0,
                    ErrorMessage: $"Client config error: {ex.Message}",
                    RequestBody: null,
                    ResponseBody: null
                ), ct);

                return ProxyResponses.TechnicalError<SprawdzenieOsobyWRuchuDrogowymResponse>(
                    ex.Message, SourceName, ((int)HttpStatusCode.InternalServerError).ToString(), requestId);
            }

            // 5) Zbuduj request + serializacja do audytu
            var req = SprawdzenieOsobyRequestCreator.Create(body, requestId, body.UserId!, _cfg.UnitId);
            string? reqXml = null;
            try { reqXml = SerializeXml(req); } catch { /* best-effort */ }

            // 6) Wywołanie + audyt + obsługa błędów
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("KSIP start: {Action}, RID={RequestId}, Endpoint={Endpoint}",
                    ActionName, requestId, endpointUrl);

                var wcfResp = await client.SprawdzenieOsobyWRDAsync(req).ConfigureAwait(false);

                // serializacja odpowiedzi do audytu (best-effort)
                string? respXml = null;
                try { respXml = SerializeXml(wcfResp); } catch { /* best-effort */ }
                
                if (_cfg.SourceMode != Common.Config.SourceMode.Production)
                {
                    _logger.LogInformation("KSIP request: {RequestXml}", reqXml ?? "<null>");
                    // W trybie innym niż produkcyjny logujemy pełną odpowiedź (w tym dane osobowe)
                    _logger.LogInformation("KSIP response: {ResponseXml}", respXml ?? "<null>");
                }

                _logger.LogInformation("KSIP done: {Action}, RID={RequestId}", ActionName, requestId);
               
                var dto = SprawdzenieOsobyWRuchuDrogowymResponseMapper.Map(wcfResp?.SprawdzenieOsobyResponse);

                _logger.LogInformation("KSIP done: {Action}, RID={RequestId}", ActionName, requestId);

                // AUDYT: sukces (WCF nie daje HTTP, więc HttpStatus=null; Fault=null)
                await _audit.Enqueue(new SourceCallLogItem(
                    RequestId: requestId,
                    Source: SourceName,
                    EndpointUrl: endpointUrl,
                    Action: ActionName,
                    HttpStatus: (int)HttpStatusCode.OK,
                    FaultCode: null,
                    FaultMessage: null,
                    DurationMs: (int)sw.Elapsed.TotalMilliseconds,
                    ErrorMessage: null,
                    RequestBody: AuditBodyHelper.PrepareBody(reqXml, _configuration),
                    ResponseBody: AuditBodyHelper.PrepareBody(respXml, _configuration)
                ), ct);

                return ProxyResponses.Success(dto, SourceName, ((int)HttpStatusCode.OK).ToString(), requestId);
            }
            catch (FaultException fe)
            {
                _logger.LogWarning(fe, "KSIP SOAP Fault, RID={RequestId}", requestId);
                string faultCode = fe.Code?.Name ?? "SOAP_FAULT";

                // AUDYT: Fault (ResponseBody raczej brak; WCF zamyka komunikat)
                await _audit.Enqueue(new SourceCallLogItem(
                    RequestId: requestId,
                    Source: SourceName,
                    EndpointUrl: endpointUrl,
                    Action: ActionName,
                    HttpStatus: (int)HttpStatusCode.OK,
                    FaultCode: faultCode,
                    FaultMessage: fe.Message,
                    DurationMs: (int)sw.Elapsed.TotalMilliseconds,
                    ErrorMessage: null,
                    RequestBody: AuditBodyHelper.PrepareBody(reqXml, _configuration),
                    ResponseBody: null
                ), ct);

                return ProxyResponses.BusinessError<SprawdzenieOsobyWRuchuDrogowymResponse>(
                    fe.Message, SourceName, faultCode, requestId);
            }
            catch (TimeoutException te)
            {
                _logger.LogError(te, "KSIP timeout, RID={RequestId}", requestId);

                await _audit.Enqueue(new SourceCallLogItem(
                    RequestId: requestId,
                    Source: SourceName,
                    EndpointUrl: endpointUrl,
                    Action: ActionName,
                    HttpStatus: (int)HttpStatusCode.RequestTimeout,
                    FaultCode: null,
                    FaultMessage: null,
                    DurationMs: (int)sw.Elapsed.TotalMilliseconds,
                    ErrorMessage: $"Timeout: {te.Message}",
                    RequestBody: AuditBodyHelper.PrepareBody(reqXml, _configuration),
                    ResponseBody: null
                ), ct);

                return ProxyResponses.TechnicalError<SprawdzenieOsobyWRuchuDrogowymResponse>(
                    "Przekroczono limit czasu połączenia do usługi SOAP.",
                    SourceName, ((int)HttpStatusCode.RequestTimeout).ToString(), requestId);
            }
            catch (CommunicationException ce)
            {
                _logger.LogError(ce, "KSIP communication error, RID={RequestId}", requestId);

                await _audit.Enqueue(new SourceCallLogItem(
                    RequestId: requestId,
                    Source: SourceName,
                    EndpointUrl: endpointUrl,
                    Action: ActionName,
                    HttpStatus: (int)HttpStatusCode.BadGateway,
                    FaultCode: null,
                    FaultMessage: null,
                    DurationMs: (int)sw.Elapsed.TotalMilliseconds,
                    ErrorMessage: $"Communication error: {ce.Message}",
                    RequestBody: AuditBodyHelper.PrepareBody(reqXml, _configuration),
                    ResponseBody: null
                ), ct);

                return ProxyResponses.TechnicalError<SprawdzenieOsobyWRuchuDrogowymResponse>(
                    $"Błąd komunikacji SOAP: {ce.Message}",
                    SourceName, ((int)HttpStatusCode.BadGateway).ToString(), requestId);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                await _audit.Enqueue(new SourceCallLogItem(
                    RequestId: requestId,
                    Source: SourceName,
                    EndpointUrl: endpointUrl,
                    Action: ActionName,
                    HttpStatus: (int)HttpStatusCode.RequestTimeout,
                    FaultCode: null,
                    FaultMessage: null,
                    DurationMs: (int)sw.Elapsed.TotalMilliseconds,
                    ErrorMessage: "Canceled by caller",
                    RequestBody: AuditBodyHelper.PrepareBody(reqXml, _configuration),
                    ResponseBody: null
                ), ct);

                return ProxyResponses.TechnicalError<SprawdzenieOsobyWRuchuDrogowymResponse>(
                    "Operacja została anulowana.",
                    SourceName, ((int)HttpStatusCode.RequestTimeout).ToString(), requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KSIP unhandled error, RID={RequestId}", requestId);

                await _audit.Enqueue(new SourceCallLogItem(
                    RequestId: requestId,
                    Source: SourceName,
                    EndpointUrl: endpointUrl,
                    Action: ActionName,
                    HttpStatus: (int)HttpStatusCode.InternalServerError,
                    FaultCode: null,
                    FaultMessage: null,
                    DurationMs: (int)sw.Elapsed.TotalMilliseconds,
                    ErrorMessage: ex.Message,
                    RequestBody: AuditBodyHelper.PrepareBody(reqXml, _configuration),
                    ResponseBody: null
                ), ct);

                return ProxyResponses.TechnicalError<SprawdzenieOsobyWRuchuDrogowymResponse>(
                    ex.Message, SourceName, ((int)HttpStatusCode.InternalServerError).ToString(), requestId);
            }
            finally
            {
                try
                {
                    if (client.State == System.ServiceModel.CommunicationState.Faulted)
                        client.Abort();
                    else
                        await client.CloseAsync().ConfigureAwait(false);
                }
                catch
                {
                    client.Abort();
                }
            }
        }

        // === helpers ===

        private static string SerializeXml<T>(T obj)
        {
            if (obj is null) return string.Empty;

            // Używamy standardowego XmlSerializer (best-effort, dla audytu).
            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);

            var ser = new XmlSerializer(typeof(T));
            using var sw = new Utf8StringWriter();
            using var xw = XmlWriter.Create(sw, new XmlWriterSettings
            {
                OmitXmlDeclaration = false,
                Indent = false,
                Encoding = Encoding.UTF8
            });
            ser.Serialize(xw, obj, ns);
            return sw.ToString();
        }

        private sealed class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }
    }
}
