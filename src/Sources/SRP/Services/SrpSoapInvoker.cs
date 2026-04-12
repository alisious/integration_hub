using IntegrationHub.Common.Exceptions;
using IntegrationHub.Sources.SRP.Contracts;
using IntegrationHub.Sources.SRP.Extensions;
using Microsoft.Extensions.Logging;
using System.ServiceModel;
using System.Text;

namespace IntegrationHub.Sources.SRP.Services
{
    public sealed class SrpSoapInvoker : ISrpSoapInvoker
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SrpSoapInvoker> _logger;

        public SrpSoapInvoker(IHttpClientFactory httpClientFactory, ILogger<SrpSoapInvoker> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
          
        }

        public async Task<SoapInvokeResult> InvokeAsync(string endpointUrl, string soapAction, string soapEnvelope,
                                                         string requestId, CancellationToken ct = default)
        {
            var client = _httpClientFactory.CreateClient("SrpServiceClient");
            using var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", soapAction);

            _logger.LogInformation("SRP SOAP request start: {Action}. RequestId={RequestId}. Endpoint={Endpoint}. Envelope={soapEnvelope}",
                                   soapAction, requestId, endpointUrl,soapEnvelope);
            
            try
            {
                using var response = await client.PostAsync(endpointUrl, content, ct);
                var xml = await response.Content.ReadAsStringAsync(ct);

                SoapFaultResponse? fault = null;
                try { RequestEnvelopeHelper.TryParseSoapFault(xml, out fault); } catch { /* best-effort */ }

                _logger.LogInformation("SRP SOAP request done: {Action}. RequestId={RequestId}. HTTP={Status}",
                                       soapAction, requestId, (int)response.StatusCode);
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
                throw new SoapIntegrationException("Timeout połączenia (ConnectTimeout) do SRP. Nieosiągalny host.", endpointUrl, soapAction, requestId, null, tce);
            }
            catch (TimeoutException tex)
            {
                _logger.LogError(tex, "Timeout during SOAP call: {Action}. RequestId={RequestId}", soapAction, requestId);
                throw new SoapIntegrationException("Timeout calling SRP", endpointUrl, soapAction, requestId, null, tex);
            }
            catch (CommunicationException cex)
            {
                _logger.LogError(cex, "Communication error during SOAP call: {Action}. RequestId={RequestId}", soapAction, requestId);
                throw new SoapIntegrationException("Communication error calling SRP", endpointUrl, soapAction, requestId, null, cex);
            }
        }
    }
}
