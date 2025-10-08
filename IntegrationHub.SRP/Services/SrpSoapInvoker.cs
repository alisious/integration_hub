using IntegrationHub.Common.Exceptions;
using IntegrationHub.Infrastructure.Audit;
using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Services
{
    public sealed class SrpSoapInvoker : ISrpSoapInvoker
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SrpSoapInvoker> _logger;
        private readonly IAuditSink _audit;
        private readonly IConfiguration _cfg;

        public SrpSoapInvoker(IHttpClientFactory httpClientFactory, ILogger<SrpSoapInvoker> logger, IAuditSink audit, IConfiguration cfg)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _audit = audit;
            _cfg = cfg;
        }

        public async Task<SoapInvokeResult> InvokeAsync(string endpointUrl, string soapAction, string soapEnvelope,
                                                         string requestId, CancellationToken ct = default)
        {
            var client = _httpClientFactory.CreateClient("SrpServiceClient");
            using var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", soapAction);

            _logger.LogInformation("SRP SOAP request start: {Action}. RequestId={RequestId}. Endpoint={Endpoint}. Envelope={soapEnvelope}",
                                   soapAction, requestId, endpointUrl,soapEnvelope);
            var started = ValueStopwatch.StartNew();
            try
            {
                using var response = await client.PostAsync(endpointUrl, content, ct);
                var xml = await response.Content.ReadAsStringAsync(ct);

                SoapFaultResponse? fault = null;
                try { RequestEnvelopeHelper.TryParseSoapFault(xml, out fault); } catch { /* best-effort */ }

                _logger.LogInformation("SRP SOAP request done: {Action}. RequestId={RequestId}. HTTP={Status}",
                                       soapAction, requestId, (int)response.StatusCode);

                // === AUDYT: sukces lub Fault (jak w CEP) ===
                await _audit.Enqueue(new SourceCallLogItem(
                    RequestId: requestId,
                    Source: "SRP",                                  // nazwa źródła
                    EndpointUrl: endpointUrl,
                    Action: soapAction,
                    HttpStatus: (int)response.StatusCode,
                    FaultCode: fault?.FaultCode,                         // null jeśli nie Fault
                    FaultMessage: fault?.FaultString,                   // null jeśli nie Fault
                    DurationMs: (int)started.GetElapsedTime().TotalMilliseconds,
                    ErrorMessage: null,
                    RequestBody: AuditBodyHelper.PrepareBody(soapEnvelope, _cfg),
                    ResponseBody: AuditBodyHelper.PrepareBody(xml, _cfg)
                ), ct);

                if (fault is null &&  !response.IsSuccessStatusCode)
                    throw new SoapIntegrationException($"HTTP {(int)response.StatusCode} from SRP",
                        endpointUrl, soapAction, requestId, response.StatusCode);

                return new SoapInvokeResult(response.StatusCode, xml, fault);
            }
            catch (TaskCanceledException tce) when (!ct.IsCancellationRequested)
            {
                // HttpClient mapuje ConnectTimeout/AttemptTimeout na TaskCanceledException.
                // Zwracamy jednoznaczny komunikat dla logów/klienta.
                _logger.LogError(tce, "SRP SOAP canceled by caller: {Action}, RequestId={RequestId}",soapAction, requestId);
                await _audit.Enqueue(new SourceCallLogItem(
                    requestId,
                    "SRP",
                    endpointUrl,
                    soapAction,
                    HttpStatus: null,
                    FaultCode: null,
                    FaultMessage: null,
                    DurationMs: (int)started.GetElapsedTime().TotalMilliseconds,
                    ErrorMessage: "Canceled by caller",
                    RequestBody: AuditBodyHelper.PrepareBody(soapEnvelope, _cfg),
                    ResponseBody: null
                ), ct);

                throw new SoapIntegrationException("Timeout połączenia (ConnectTimeout) do SRP. Nieosiągalny host.", endpointUrl, soapAction, requestId, null, tce);
            }
            catch (TimeoutException tex)
            {
                _logger.LogError(tex, "Timeout during SOAP call: {Action}. RequestId={RequestId}", soapAction, requestId);
                await _audit.Enqueue(new SourceCallLogItem(
                   requestId,
                   "SRP",
                   endpointUrl,
                   soapAction,
                   HttpStatus: null,
                   FaultCode: null,
                   FaultMessage: null,
                   DurationMs: (int)started.GetElapsedTime().TotalMilliseconds,
                   ErrorMessage: $"Timeout: {tex.Message}",
                   RequestBody: AuditBodyHelper.PrepareBody(soapEnvelope, _cfg),
                   ResponseBody: null
                ), ct);

                throw new SoapIntegrationException("Timeout calling SRP", endpointUrl, soapAction, requestId, null, tex);
            }
            catch (CommunicationException cex)
            {
                _logger.LogError(cex, "Communication error during SOAP call: {Action}. RequestId={RequestId}", soapAction, requestId);
                await _audit.Enqueue(new SourceCallLogItem(
                    requestId,
                    "SRP",
                    endpointUrl,
                    soapAction,
                    HttpStatus: null,
                    FaultCode: null,
                    FaultMessage: null,
                    DurationMs: (int)started.GetElapsedTime().TotalMilliseconds,
                    ErrorMessage: $"Communication error: {cex.Message}",
                    RequestBody: AuditBodyHelper.PrepareBody(soapEnvelope, _cfg),
                    ResponseBody: null
                ), ct);

                throw new SoapIntegrationException("Communication error calling SRP", endpointUrl, soapAction, requestId, null, cex);
            }
        }
    }
}
