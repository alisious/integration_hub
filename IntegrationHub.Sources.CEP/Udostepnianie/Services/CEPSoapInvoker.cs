// IntegrationHub.Sources.CEP.Udostepnianie/Services/CepSoapInvoker.cs
using IntegrationHub.Common.Exceptions;
using IntegrationHub.Common.Interfaces;
using IntegrationHub.Infrastructure.Audit;
using IntegrationHub.Sources.CEP.Config;
using IntegrationHub.Sources.CEP.Udostepnianie.Mappers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Services
{
    public interface ICepSoapInvoker
    {
        Task<(HttpStatusCode Status, string Body)> InvokeAsync(
            CEPConfig cfg,
            string endpointUrl,
            string soapAction,
            string envelope,
            string requestId,
            CancellationToken ct = default);
    }

    public sealed class CepSoapInvoker : ICepSoapInvoker
    {
        private readonly ILogger<CepSoapInvoker> _logger;
        private readonly IClientCertificateProvider _certProvider;
        private readonly IAuditSink _audit;                 
        private readonly IConfiguration _cfg;

        public CepSoapInvoker(IClientCertificateProvider certProvider, ILogger<CepSoapInvoker> logger, IAuditSink audit, IConfiguration cfg)
        {
            _certProvider = certProvider;
            _logger = logger;
            _audit = audit;
            _cfg = cfg;
        }

        public async Task<(HttpStatusCode Status, string Body)> InvokeAsync(
            CEPConfig cfg, string endpointUrl, string soapAction, string envelope, string requestId, CancellationToken ct = default)
        {
            var handler = new HttpClientHandler { ClientCertificateOptions = ClientCertificateOption.Manual };
            var clientCert = _certProvider.GetClientCertificate(cfg);
            handler.ClientCertificates.Add(clientCert);

            if (cfg.TrustServerCerificate)
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            var started = ValueStopwatch.StartNew();
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(Math.Max(5, cfg.TimeoutSeconds)) };
            using var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", soapAction);

            _logger.LogInformation("CEP SOAP start: {Action}, RequestId={RequestId}, Endpoint={Endpoint}", soapAction, requestId, endpointUrl);

            try
            {
                using var resp = await client.PostAsync(endpointUrl, content, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                _logger.LogInformation("CEP SOAP done: {Action}, RequestId={RequestId}, HTTP={Status}", soapAction, requestId, (int)resp.StatusCode);
                // === NOWOŚĆ: wykrycie SOAP Fault i rzutowanie SoapFaultException ===
                // CEPIK potrafi odesłać <Fault/> także z HTTP 200, dlatego sprawdzamy treść zawsze.
                var fault = FaultResponseXmlMapper.ParseOrNull(body);
                if (fault is not null)
                {
                    var finalCode = fault.Kod ?? fault.FaultCode;
                    var finalMsg = fault.Komunikat ?? fault.FaultString ?? "SOAP Fault returned by service.";
                    _logger.LogWarning("CEP SOAP Fault: {Action}, RID={RequestId}, FaultCode={FaultCode}, Message={Message}",
                        soapAction, requestId, finalCode, finalMsg);

                    // Zapisz log wywołania do SQL (z Fault)
                    await _audit.Enqueue(new SourceCallLogItem(
                        requestId,
                        "CEP.Udostepnianie",
                        endpointUrl,
                        soapAction,
                        (int)resp.StatusCode,
                        finalCode,
                        finalMsg,
                        (int)started.GetElapsedTime().TotalMilliseconds,
                        null,
                        AuditBodyHelper.PrepareBody(envelope, _cfg),
                        AuditBodyHelper.PrepareBody(body, _cfg)
                    ), ct);

                    throw new SoapFaultException(finalMsg, endpointUrl, soapAction, requestId, finalCode);
                }

                // Zapisz log sukcesu
                await _audit.Enqueue(new SourceCallLogItem(
                    requestId,
                    "CEP.Udostepnianie",
                    endpointUrl,
                    soapAction,
                    (int)resp.StatusCode,
                    null,
                    null,
                    (int)started.GetElapsedTime().TotalMilliseconds,
                    null,
                    AuditBodyHelper.PrepareBody(envelope, _cfg),
                    AuditBodyHelper.PrepareBody(body, _cfg)
                ), ct);

                return (resp.StatusCode, body);
            }
            // === anulowanie wywołania przez wywołującego ===
            catch (OperationCanceledException oce) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning(oce, "CEP SOAP canceled by caller: {Action}, RequestId={RequestId}", soapAction, requestId);
                throw new SoapIntegrationException("Żądanie SOAP zostało anulowane przez wywołującego.",
                    endpointUrl, soapAction, requestId, null, oce);
            }
            // === timeout (brak ct.IsCancellationRequested) ===
            catch (TaskCanceledException tce) // typowy scenariusz timeoutu HttpClient
            {
                _logger.LogError(tce, "CEP SOAP timeout: {Action}, RequestId={RequestId}", soapAction, requestId);
                throw new SoapIntegrationException("Przekroczono limit czasu połączenia do usługi SOAP.",
                    endpointUrl, soapAction, requestId, null, tce);
            }
            // === błąd komunikacji HTTP/transportu ===
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "CEP SOAP communication error: {Action}, RequestId={RequestId}", soapAction, requestId);
                throw new SoapIntegrationException($"Błąd komunikacji HTTP: {hre.Message}",
                    endpointUrl, soapAction, requestId, hre.StatusCode, hre);
            }
        }

    }
}
