// IntegrationHub.Sources.CEP.Services/CEPUdostepnianieService.cs
using IntegrationHub.Common.Contracts;                 // ProxyResponse<T>, ProxyStatus
using IntegrationHub.Common.Exceptions;                // SoapIntegrationException
using IntegrationHub.Sources.CEP.Config;               // CEPConfig
using IntegrationHub.Sources.CEP.Udostepnianie.Helpers;            // CepUdostepnianieEnvelopeHelper
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;  // PytanieOPojazdResponse
using IntegrationHub.Sources.CEP.Udostepnianie.Mappers;    // PytanieOPojazdResponseXmlMapper
using IntegrationHub.Sources.CEP.Udostepnianie.Services;   // ICepSoapInvoker
using IntegrationHub.Sources.CEP.Udostepnianie.RequestValidation; // PytanieOPojazdRequestValidator (+ ValidationResultExtensions)
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Net;

// Jeśli ValidationResultExtensions jest w innej przestrzeni nazw, dodaj odpowiedni using.

namespace IntegrationHub.Sources.CEP.Udostepnianie.Services
{
    public interface ICEPUdostepnianieService
    {
        Task<ProxyResponse<PytanieOPojazdResponse>> PytanieOPojazdAsync(
            PytanieOPojazdRequest body,
            string? requestId = null,
            CancellationToken ct = default);
        Task<ProxyResponse<PytanieOPojazdRozszerzoneResponse>> PytanieOPojazdRozszerzoneAsync(
            PytanieOPojazdRequest body, 
            string? requestId = null, 
            CancellationToken ct = default);
        Task<ProxyResponse<PytanieODokumentPojazduResponse>> PytanieODokumentPojazduAsync(
            PytanieODokumentPojazduRequest body,
            string? requestId = null,
            CancellationToken ct = default);

        Task<ProxyResponse<PytanieOListeCzynnosciPojazduResponse>> PytanieOListeCzynnosciPojazduAsync(
            PytanieOListeCzynnosciPojazduRequest body,
            string? requestId = null,
            CancellationToken ct = default);

    }

    public sealed class CEPUdostepnianieService : ICEPUdostepnianieService
    {
        private readonly CEPConfig _cfg;
        private readonly ICepSoapInvoker _invoker;
        private readonly ILogger<CEPUdostepnianieService> _logger;

        public CEPUdostepnianieService(
            IOptions<CEPConfig> cfg,
            ICepSoapInvoker invoker,
            ILogger<CEPUdostepnianieService> logger)
        {
            _cfg = cfg.Value ?? throw new ArgumentNullException(nameof(cfg));
            _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProxyResponse<PytanieOPojazdResponse>> PytanieOPojazdAsync(
            PytanieOPojazdRequest body,
            string? requestId = null,
            CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString("N");

            // 1) Walidacja przez PytanieOPojazdRequestValidator + zwrot ProxyResponse via ValidationResultExtensions
            var validator = new PytanieOPojazdRequestValidator();
            var vr = validator.ValidateAndNormalize(body);
            if (!vr.IsValid)
            {
                // Wymóg: wykorzystaj ValidationResultExtensions (vr.ToProxyResponse)
                var baseResp = vr.ToProxyResponse(requestId); // ProxyResponse<object>
                return new ProxyResponse<PytanieOPojazdResponse>
                {
                    RequestId = baseResp.RequestId,
                    Source = baseResp.Source,
                    Status = baseResp.Status,
                    SourceStatusCode = baseResp.SourceStatusCode,
                    ErrorMessage = baseResp.ErrorMessage
                };
            }

            // 2) Koperta SOAP z helpera (identyfikatorSystemuZewnetrznego="ŻW", wnioskodawca="ŻW",
            //    znakSprawy=requestId, parametryCzasowe z body – dataPrezentacji, wyszukiwaniePoDanychHistorycznych=false domyślnie)
            var envelope = PytanieOPojazdEnvelope.Create(body, requestId);

            // 3) Endpoint z konfiguracji
            var endpointUrl = _cfg.ShareServiceUrl;
            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                return new ProxyResponse<PytanieOPojazdResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = (int)HttpStatusCode.InternalServerError,
                    ErrorMessage = "Brak ShareServiceUrl w konfiguracji CEP."
                };
            }

            // 4) SOAPAction
            const string soapAction = CEPUdostepnianieSoapActions.PytanieOPojazd;

            try
            {
                // 5) Wywołanie SOAP (mTLS + opcjonalny TrustServerCertificate wewnątrz invokera)
                var (status, xml) = await _invoker.InvokeAsync(_cfg, endpointUrl, soapAction, envelope, requestId, ct);

                // 6) HTTP status spoza 2xx → błąd techniczny
                if ((int)status < 200 || (int)status >= 300)
                {
                    return new ProxyResponse<PytanieOPojazdResponse>
                    {
                        RequestId = requestId,
                        Source = "CEP.Udostepnianie",
                        Status = ProxyStatus.TechnicalError,
                        SourceStatusCode = (int)status,
                        ErrorMessage = $"HTTP {(int)status}"
                    };
                }

                // 7) Mapowanie odpowiedzi XML → DTO
                var dto = PytanieOPojazdResponseXmlMapper.Parse(xml);

                return new ProxyResponse<PytanieOPojazdResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.Success,
                    SourceStatusCode = (int)HttpStatusCode.OK,
                    Data = dto
                };
            }
            catch (SoapIntegrationException sie)
            {
                // Transport/komunikacja/time-out/anulowanie – opakowane w SoapIntegrationException przez invoker
                _logger.LogError(sie, "SOAP error ({Action}) RequestId={RequestId}", soapAction, requestId);

                return new ProxyResponse<PytanieOPojazdResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = (int?)(sie.HttpStatus ?? HttpStatusCode.BadGateway),
                    ErrorMessage = sie.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd PytanieOPojazdAsync, RequestId={RequestId}", requestId);
                return new ProxyResponse<PytanieOPojazdResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = (int)HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ProxyResponse<PytanieOPojazdRozszerzoneResponse>> PytanieOPojazdRozszerzoneAsync(
            PytanieOPojazdRequest body,
            string? requestId = null,
            CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString("N");

            // 1) Walidacja ta sama co dla zwykłego pytania
            var validator = new PytanieOPojazdRequestValidator();
            var vr = validator.ValidateAndNormalize(body);
            if (!vr.IsValid)
            {
                var baseResp = vr.ToProxyResponse(requestId);
                return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
                {
                    RequestId = baseResp.RequestId,
                    Source = baseResp.Source,
                    Status = baseResp.Status,
                    SourceStatusCode = baseResp.SourceStatusCode,
                    ErrorMessage = baseResp.ErrorMessage
                };
            }

            // 2) Koperta SOAP — wariant „rozszerzony”
            var envelope = PytanieOPojazdRozszerzoneEnvelope.Create(body, requestId);

            // 3) Endpoint
            var endpointUrl = _cfg.ShareServiceUrl;
            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = (int)HttpStatusCode.InternalServerError,
                    ErrorMessage = "Brak ShareServiceUrl w konfiguracji CEP."
                };
            }

            // 4) SOAPAction
            const string soapAction = CEPUdostepnianieSoapActions.PytanieOPojazdRozszerzone;

            try
            {
                // 5) Wywołanie SOAP
                var (status, xml) = await _invoker.InvokeAsync(_cfg, endpointUrl, soapAction, envelope, requestId, ct);

                if ((int)status < 200 || (int)status >= 300)
                {
                    return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
                    {
                        RequestId = requestId,
                        Source = "CEP.Udostepnianie",
                        Status = ProxyStatus.TechnicalError,
                        SourceStatusCode = (int)status,
                        ErrorMessage = $"HTTP {(int)status}"
                    };
                }

                // 6) Mapowanie XML → DTO (rozszerzony)
                var dto = PytanieOPojazdRozszerzoneResponseXmlMapper.Parse(xml);

                return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.Success,
                    SourceStatusCode = (int)HttpStatusCode.OK,
                    Data = dto
                };
            }
            catch (SoapIntegrationException sie)
            {
                _logger.LogError(sie, "SOAP error ({Action}) RequestId={RequestId}", soapAction, requestId);
                return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = (int?)(sie.HttpStatus ?? HttpStatusCode.BadGateway),
                    ErrorMessage = sie.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd PytanieOPojazdRozszerzoneAsync, RequestId={RequestId}", requestId);
                return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = (int)HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ProxyResponse<PytanieODokumentPojazduResponse>> PytanieODokumentPojazduAsync(
            PytanieODokumentPojazduRequest body,
            string? requestId = null,
            CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString("N");

            // 1) Walidacja
            var validator = new PytanieODokumentPojazduRequestValidator();
            var vr = validator.ValidateAndNormalize(body);
            if (!vr.IsValid)
            {
                var baseResp = vr.ToProxyResponse(requestId);
                return new ProxyResponse<PytanieODokumentPojazduResponse>
                {
                    RequestId = baseResp.RequestId,
                    Source = "CEP.Udostepnianie",
                    Status = baseResp.Status,
                    SourceStatusCode = baseResp.SourceStatusCode,
                    ErrorMessage = baseResp.ErrorMessage
                };
            }

            // 2) Koperta SOAP
            var envelope = PytanieODokumentPojazduEnvelope.Create(body, requestId);

            // 3) Endpoint
            var endpointUrl = _cfg.ShareServiceUrl;
            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                return new ProxyResponse<PytanieODokumentPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = (int)HttpStatusCode.InternalServerError,
                    ErrorMessage = "Brak ShareServiceUrl w konfiguracji CEP."
                };
            }

            // 4) SOAPAction
            const string soapAction = CEPUdostepnianieSoapActions.PytanieODokumentPojazdu;

            try
            {
                // 5) Wywołanie SOAP
                var (status, xml) = await _invoker.InvokeAsync(_cfg, endpointUrl, soapAction, envelope, requestId, ct);

                if ((int)status < 200 || (int)status >= 300)
                {
                    return new ProxyResponse<PytanieODokumentPojazduResponse>
                    {
                        RequestId = requestId,
                        Source = "CEP.Udostepnianie",
                        Status = ProxyStatus.TechnicalError,
                        SourceStatusCode = (int)status,
                        ErrorMessage = $"HTTP {(int)status}"
                    };
                }

                // 6) Mapowanie XML → DTO
                var dto = PytanieODokumentPojazduResponseXmlMapper.Parse(xml);

                return new ProxyResponse<PytanieODokumentPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.Success,
                    SourceStatusCode = (int)HttpStatusCode.OK,
                    Data = dto
                };
            }
            catch (SoapIntegrationException sie)
            {
                _logger.LogError(sie, "SOAP error ({Action}) RequestId={RequestId}", soapAction, requestId);
                return new ProxyResponse<PytanieODokumentPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = (int?)(sie.HttpStatus ?? HttpStatusCode.BadGateway),
                    ErrorMessage = sie.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd PytanieODokumentPojazduAsync, RequestId={RequestId}", requestId);
                return new ProxyResponse<PytanieODokumentPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = (int)HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        // IMPLEMENTACJA w CEPUdostepnianieService
        public async Task<ProxyResponse<PytanieOListeCzynnosciPojazduResponse>> PytanieOListeCzynnosciPojazduAsync(
            PytanieOListeCzynnosciPojazduRequest body,
            string? requestId = null,
            CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString("N");

            // 1) Walidacja
            var validator = new PytanieOListeCzynnosciPojazduRequestValidator();
            var vr = validator.ValidateAndNormalize(body);
            if (!vr.IsValid)
            {
                var baseResp = vr.ToProxyResponse(requestId);
                return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
                {
                    RequestId = baseResp.RequestId,
                    Source = "CEP.Udostepnianie",
                    Status = baseResp.Status,
                    SourceStatusCode = baseResp.SourceStatusCode,
                    ErrorMessage = baseResp.ErrorMessage
                };
            }

            // 2) Koperta SOAP
            var envelope = PytanieOListeCzynnosciPojazduEnvelope.Create(body, requestId);

            // 3) Endpoint
            var endpointUrl = _cfg.ShareServiceUrl;
            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = (int)HttpStatusCode.InternalServerError,
                    ErrorMessage = "Brak ShareServiceUrl w konfiguracji CEP."
                };
            }

            // 4) SOAPAction
            const string soapAction = CEPUdostepnianieSoapActions.PytanieOListeCzynnosciPojazdu;

            try
            {
                // 5) Wywołanie SOAP
                var (status, xml) = await _invoker.InvokeAsync(_cfg, endpointUrl, soapAction, envelope, requestId, ct);

                if ((int)status < 200 || (int)status >= 300)
                {
                    return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
                    {
                        RequestId = requestId,
                        Source = "CEP.Udostepnianie",
                        Status = ProxyStatus.TechnicalError,
                        SourceStatusCode = (int)status,
                        ErrorMessage = $"HTTP {(int)status}"
                    };
                }

                // 6) Mapowanie XML → DTO
                var dto = PytanieOListeCzynnosciPojazduResponseXmlMapper.Parse(xml);

                return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.Success,
                    SourceStatusCode = (int)HttpStatusCode.OK,
                    Data = dto
                };
            }
            catch (SoapIntegrationException sie)
            {
                _logger.LogError(sie, "SOAP error ({Action}) RequestId={RequestId}", soapAction, requestId);
                return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = (int?)(sie.HttpStatus ?? HttpStatusCode.BadGateway),
                    ErrorMessage = sie.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd PytanieOListeCzynnosciPojazduAsync, RequestId={RequestId}", requestId);
                return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = (int)HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

    }
}
