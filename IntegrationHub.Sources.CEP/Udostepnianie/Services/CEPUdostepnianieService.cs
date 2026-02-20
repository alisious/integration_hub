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
using IntegrationHub.Common.RequestValidation; // IRequestValidator<T>

using System.Net;

// Jeśli ValidationResultExtensions jest w innej przestrzeni nazw, dodaj odpowiedni using.

namespace IntegrationHub.Sources.CEP.Udostepnianie.Services
{
 


    public interface ICEPUdostepnianieService
    {
        Task<ProxyResponse<PytanieOPojazdResponse>> PytanieOPojazdAsync(
            PytanieOPojazdRequest body,
            string requestId,
            CancellationToken ct = default);
        Task<ProxyResponse<PytanieOPojazdRozszerzoneResponse>> PytanieOPojazdRozszerzoneAsync(
            PytanieOPojazdRequest body, 
            string requestId, 
            CancellationToken ct = default);
        Task<ProxyResponse<PytanieODokumentPojazduResponse>> PytanieODokumentPojazduAsync(
            PytanieODokumentPojazduRequest body,
            string requestId,
            CancellationToken ct = default);

        Task<ProxyResponse<PytanieOListeCzynnosciPojazduResponse>> PytanieOListeCzynnosciPojazduAsync(
            PytanieOListeCzynnosciPojazduRequest body,
            string requestId,
            CancellationToken ct = default);
        Task<ProxyResponse<PytanieOHistorieLicznikaResponse>> PytanieOHistorieLicznikaAsync(
            PytanieOHistorieLicznikaRequest body,
            string requestId,
            CancellationToken ct = default);
        Task<ProxyResponse<PytanieOPodmiotResponse>> PytanieOPodmiotAsync(
            PytanieOPodmiotRequest body,
            string requestId,
            CancellationToken ct = default);

    }

    public sealed class CEPUdostepnianieService : ICEPUdostepnianieService
    {
        private const string SourceName = "CEP";
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

        private static ProxyResponse<T> FromValidation<T>(ProxyResponse<object> vr, string requestId)
        {
            // Przepisz pola bez Data
            return new ProxyResponse<T>
            {
                RequestId = requestId,
                Source = SourceName,
                Status = vr.Status,
                SourceStatusCode = vr.SourceStatusCode,
                Message = vr.Message
            };
        }

        private async Task<ProxyResponse<TRes>> CallAsync<TReq, TRes>(
            TReq body,
            string requestId,
            string soapAction,
            Func<TReq, string, string> buildEnvelope,
            Func<string, TRes> mapXml,
            Func<TReq, ProxyResponse<object>?> validateOrNull,
            CancellationToken ct)
        {
            if (String.IsNullOrEmpty(requestId)) requestId = Guid.NewGuid().ToString("N");

            // 1) Walidacja (jeżeli zwróci błąd – kończymy)
            var v = validateOrNull(body);
            if (v is not null && v.Status != ProxyStatus.Success)
                return FromValidation<TRes>(v, requestId);

            // 2) Endpoint z cfg
            var endpointUrl = _cfg.ShareServiceUrl;
            if (string.IsNullOrWhiteSpace(endpointUrl))
                return ProxyResponseFactory.TechnicalError<TRes>(
                    "Brak ShareServiceUrl w konfiguracji CEP.", SourceName, ((int)HttpStatusCode.InternalServerError).ToString(), requestId);

            // 3) Envelope
            var envelope = buildEnvelope(body, requestId);

            try
            {
                // 4) Wywołanie SOAP (może rzucić: SoapFaultException, SoapIntegrationException)
                var (status, xml) = await _invoker.InvokeAsync(_cfg, endpointUrl!, soapAction, envelope, requestId, ct);

                // HTTP != 2xx ⇒ błąd techniczny (status HTTP jako tekst)
                var httpCode = ((int)status).ToString();
                if ((int)status < 200 || (int)status >= 300)
                    return ProxyResponseFactory.TechnicalError<TRes>($"HTTP {(int)status}", SourceName, httpCode, requestId);

                // 5) Mapowanie XML → DTO
                var dto = mapXml(xml);
                return ProxyResponseFactory.Success(dto, SourceName, ((int)HttpStatusCode.OK).ToString(), requestId);
            }
            catch (SoapFaultException sfx)
            {
                // SOAP Fault = błąd biznesowy
                _logger.LogWarning(sfx, "SOAP FAULT: {Action} RID={RequestId} Code={Code}", soapAction, requestId, sfx.FaultCode ?? "SOAP_FAULT");
                var code = string.IsNullOrEmpty(sfx.FaultCode) ? "SOAP_FAULT" : sfx.FaultCode!;
                return ProxyResponseFactory.BusinessError<TRes>(sfx.Message, SourceName, code, requestId);
            }
            catch (SoapIntegrationException sie)
            {
                // Transport/timeout/anulowanie – błąd techniczny
                _logger.LogError(sie, "SOAP transport error: {Action} RID={RequestId}", soapAction, requestId);
                var http = ((int?)(sie.HttpStatus ?? HttpStatusCode.BadGateway)).ToString();
                return ProxyResponseFactory.TechnicalError<TRes>(sie.Message, SourceName, http, requestId);
            }
            catch (OperationCanceledException oce) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning(oce, "Operation canceled by caller: {Action} RID={RequestId}", soapAction, requestId);
                return ProxyResponseFactory.TechnicalError<TRes>("Operacja została anulowana.", SourceName, ((int)HttpStatusCode.RequestTimeout).ToString(), requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error: {Action} RID={RequestId}", soapAction, requestId);
                return ProxyResponseFactory.TechnicalError<TRes>(ex.Message, SourceName, ((int)HttpStatusCode.InternalServerError).ToString(), requestId);
            }
        }


        public Task<ProxyResponse<PytanieOPojazdResponse>> PytanieOPojazdAsync(
            PytanieOPojazdRequest body, string requestId, CancellationToken ct = default) =>
            CallAsync(
                body, requestId,
                CEPUdostepnianieSoapActions.PytanieOPojazd,
                (b, rid) => PytanieOPojazdEnvelope.Create(b, rid),
                PytanieOPojazdResponseXmlMapper.Parse,
                b => new PytanieOPojazdRequestValidator().ValidateAndNormalize(b).ToProxyResponseOrNull(SourceName,requestId),
                ct);



        //public async Task<ProxyResponse<PytanieOPojazdResponse>> PytanieOPojazdAsync(
        //    PytanieOPojazdRequest body,
        //    string? requestId = null,
        //    CancellationToken ct = default)
        //{
        //    requestId ??= Guid.NewGuid().ToString("N");

        //    // 1) Walidacja przez PytanieOPojazdRequestValidator + zwrot ProxyResponse via ValidationResultExtensions
        //    var validator = new PytanieOPojazdRequestValidator();
        //    var vr = validator.ValidateAndNormalize(body);
        //    if (!vr.IsValid)
        //    {
        //        // Wymóg: wykorzystaj ValidationResultExtensions (vr.ToProxyResponse)
        //        var baseResp = vr.ToProxyResponse(requestId); // ProxyResponse<object>
        //        return new ProxyResponse<PytanieOPojazdResponse>
        //        {
        //            RequestId = baseResp.RequestId,
        //            Source = baseResp.Source,
        //            Status = baseResp.Status,
        //            SourceStatusCode = baseResp.SourceStatusCode,
        //            ErrorMessage = baseResp.ErrorMessage
        //        };
        //    }

        //    // 2) Koperta SOAP z helpera (identyfikatorSystemuZewnetrznego="ŻW", wnioskodawca="ŻW",
        //    //    znakSprawy=requestId, parametryCzasowe z body – dataPrezentacji, wyszukiwaniePoDanychHistorycznych=false domyślnie)
        //    var envelope = PytanieOPojazdEnvelope.Create(body, requestId);

        //    // 3) Endpoint z konfiguracji
        //    var endpointUrl = _cfg.ShareServiceUrl;
        //    if (string.IsNullOrWhiteSpace(endpointUrl))
        //    {
        //        return new ProxyResponse<PytanieOPojazdResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
        //            ErrorMessage = "Brak ShareServiceUrl w konfiguracji CEP."
        //        };
        //    }

        //    // 4) SOAPAction
        //    const string soapAction = CEPUdostepnianieSoapActions.PytanieOPojazd;

        //    try
        //    {
        //        // 5) Wywołanie SOAP (mTLS + opcjonalny TrustServerCertificate wewnątrz invokera)
        //        var (status, xml) = await _invoker.InvokeAsync(_cfg, endpointUrl, soapAction, envelope, requestId, ct);

        //        // 6) HTTP status spoza 2xx → błąd techniczny
        //        if ((int)status < 200 || (int)status >= 300)
        //        {
        //            return new ProxyResponse<PytanieOPojazdResponse>
        //            {
        //                RequestId = requestId,
        //                Source = "CEP.Udostepnianie",
        //                Status = ProxyStatus.TechnicalError,
        //                SourceStatusCode = ((int)status).ToString(),
        //                ErrorMessage = $"HTTP {(int)status}"
        //            };
        //        }

        //        // 7) Mapowanie odpowiedzi XML → DTO
        //        var dto = PytanieOPojazdResponseXmlMapper.Parse(xml);

        //        return new ProxyResponse<PytanieOPojazdResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.Success,
        //            SourceStatusCode = ((int)HttpStatusCode.OK).ToString(),
        //            Data = dto
        //        };
        //    }
        //    catch (SoapIntegrationException sie)
        //    {
        //        // Transport/komunikacja/time-out/anulowanie – opakowane w SoapIntegrationException przez invoker
        //        _logger.LogError(sie, "SOAP error ({Action}) RequestId={RequestId}", soapAction, requestId);

        //        return new ProxyResponse<PytanieOPojazdResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int?)(sie.HttpStatus ?? HttpStatusCode.BadGateway)).ToString(),
        //            ErrorMessage = sie.Message
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Błąd PytanieOPojazdAsync, RequestId={RequestId}", requestId);
        //        return new ProxyResponse<PytanieOPojazdResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
        //            ErrorMessage = ex.Message
        //        };
        //    }
        //}


        public Task<ProxyResponse<PytanieOPojazdRozszerzoneResponse>> PytanieOPojazdRozszerzoneAsync(
            PytanieOPojazdRequest body, string requestId, CancellationToken ct = default) =>
            CallAsync(
                body, requestId,
                CEPUdostepnianieSoapActions.PytanieOPojazdRozszerzone,
                (b, rid) => PytanieOPojazdRozszerzoneEnvelope.Create(b, rid),
                PytanieOPojazdRozszerzoneResponseXmlMapper.Parse,
                b => new PytanieOPojazdRequestValidator().ValidateAndNormalize(b).ToProxyResponseOrNull(SourceName,requestId),
                ct);

        //public async Task<ProxyResponse<PytanieOPojazdRozszerzoneResponse>> PytanieOPojazdRozszerzoneAsync(
        //    PytanieOPojazdRequest body,
        //    string? requestId = null,
        //    CancellationToken ct = default)
        //{
        //    requestId ??= Guid.NewGuid().ToString("N");

        //    // 1) Walidacja ta sama co dla zwykłego pytania
        //    var validator = new PytanieOPojazdRequestValidator();
        //    var vr = validator.ValidateAndNormalize(body);
        //    if (!vr.IsValid)
        //    {
        //        var baseResp = vr.ToProxyResponse(requestId);
        //        return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
        //        {
        //            RequestId = baseResp.RequestId,
        //            Source = baseResp.Source,
        //            Status = baseResp.Status,
        //            SourceStatusCode = baseResp.SourceStatusCode,
        //            ErrorMessage = baseResp.ErrorMessage
        //        };
        //    }

        //    // 2) Koperta SOAP — wariant „rozszerzony”
        //    var envelope = PytanieOPojazdRozszerzoneEnvelope.Create(body, requestId);

        //    // 3) Endpoint
        //    var endpointUrl = _cfg.ShareServiceUrl;
        //    if (string.IsNullOrWhiteSpace(endpointUrl))
        //    {
        //        return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
        //            ErrorMessage = "Brak ShareServiceUrl w konfiguracji CEP."
        //        };
        //    }

        //    // 4) SOAPAction
        //    const string soapAction = CEPUdostepnianieSoapActions.PytanieOPojazdRozszerzone;

        //    try
        //    {
        //        // 5) Wywołanie SOAP
        //        var (status, xml) = await _invoker.InvokeAsync(_cfg, endpointUrl, soapAction, envelope, requestId, ct);

        //        if ((int)status < 200 || (int)status >= 300)
        //        {
        //            return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
        //            {
        //                RequestId = requestId,
        //                Source = "CEP.Udostepnianie",
        //                Status = ProxyStatus.TechnicalError,
        //                SourceStatusCode = ((int)status).ToString(),
        //                ErrorMessage = $"HTTP {(int)status}"
        //            };
        //        }

        //        // 6) Mapowanie XML → DTO (rozszerzony)
        //        var dto = PytanieOPojazdRozszerzoneResponseXmlMapper.Parse(xml);

        //        return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.Success,
        //            SourceStatusCode = ((int)HttpStatusCode.OK).ToString(),
        //            Data = dto
        //        };
        //    }
        //    catch (SoapIntegrationException sie)
        //    {
        //        _logger.LogError(sie, "SOAP error ({Action}) RequestId={RequestId}", soapAction, requestId);
        //        return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int?)(sie.HttpStatus ?? HttpStatusCode.BadGateway)).ToString(),
        //            ErrorMessage = sie.Message
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Błąd PytanieOPojazdRozszerzoneAsync, RequestId={RequestId}", requestId);
        //        return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
        //            ErrorMessage = ex.Message
        //        };
        //    }
        //}


        public Task<ProxyResponse<PytanieODokumentPojazduResponse>> PytanieODokumentPojazduAsync(
            PytanieODokumentPojazduRequest body, string requestId, CancellationToken ct = default) =>
            CallAsync(
                body, requestId,
                CEPUdostepnianieSoapActions.PytanieODokumentPojazdu,
                (b, rid) => PytanieODokumentPojazduEnvelope.Create(b, rid),
                PytanieODokumentPojazduResponseXmlMapper.Parse,
                b => new PytanieODokumentPojazduRequestValidator().ValidateAndNormalize(b).ToProxyResponseOrNull(SourceName, requestId),
                ct);
        //public async Task<ProxyResponse<PytanieODokumentPojazduResponse>> PytanieODokumentPojazduAsync(
        //    PytanieODokumentPojazduRequest body,
        //    string? requestId = null,
        //    CancellationToken ct = default)
        //{
        //    requestId ??= Guid.NewGuid().ToString("N");

        //    // 1) Walidacja
        //    var validator = new PytanieODokumentPojazduRequestValidator();
        //    var vr = validator.ValidateAndNormalize(body);
        //    if (!vr.IsValid)
        //    {
        //        var baseResp = vr.ToProxyResponse(requestId);
        //        return new ProxyResponse<PytanieODokumentPojazduResponse>
        //        {
        //            RequestId = baseResp.RequestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = baseResp.Status,
        //            SourceStatusCode = baseResp.SourceStatusCode,
        //            ErrorMessage = baseResp.ErrorMessage
        //        };
        //    }

        //    // 2) Koperta SOAP
        //    var envelope = PytanieODokumentPojazduEnvelope.Create(body, requestId);

        //    // 3) Endpoint
        //    var endpointUrl = _cfg.ShareServiceUrl;
        //    if (string.IsNullOrWhiteSpace(endpointUrl))
        //    {
        //        return new ProxyResponse<PytanieODokumentPojazduResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
        //            ErrorMessage = "Brak ShareServiceUrl w konfiguracji CEP."
        //        };
        //    }

        //    // 4) SOAPAction
        //    const string soapAction = CEPUdostepnianieSoapActions.PytanieODokumentPojazdu;

        //    try
        //    {
        //        // 5) Wywołanie SOAP
        //        var (status, xml) = await _invoker.InvokeAsync(_cfg, endpointUrl, soapAction, envelope, requestId, ct);

        //        if ((int)status < 200 || (int)status >= 300)
        //        {
        //            return new ProxyResponse<PytanieODokumentPojazduResponse>
        //            {
        //                RequestId = requestId,
        //                Source = "CEP.Udostepnianie",
        //                Status = ProxyStatus.TechnicalError,
        //                SourceStatusCode = ((int)status).ToString(),
        //                ErrorMessage = $"HTTP {(int)status}"
        //            };
        //        }

        //        // 6) Mapowanie XML → DTO
        //        var dto = PytanieODokumentPojazduResponseXmlMapper.Parse(xml);

        //        return new ProxyResponse<PytanieODokumentPojazduResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.Success,
        //            SourceStatusCode = ((int)HttpStatusCode.OK).ToString(),
        //            Data = dto
        //        };
        //    }
        //    catch (SoapIntegrationException sie)
        //    {
        //        _logger.LogError(sie, "SOAP error ({Action}) RequestId={RequestId}", soapAction, requestId);
        //        return new ProxyResponse<PytanieODokumentPojazduResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int?)(sie.HttpStatus ?? HttpStatusCode.BadGateway)).ToString(),
        //            ErrorMessage = sie.Message
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Błąd PytanieODokumentPojazduAsync, RequestId={RequestId}", requestId);
        //        return new ProxyResponse<PytanieODokumentPojazduResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
        //            ErrorMessage = ex.Message
        //        };
        //    }
        //}

        public Task<ProxyResponse<PytanieOListeCzynnosciPojazduResponse>> PytanieOListeCzynnosciPojazduAsync(
            PytanieOListeCzynnosciPojazduRequest body, string requestId, CancellationToken ct = default) =>
            CallAsync(
                body, requestId,
                CEPUdostepnianieSoapActions.PytanieOListeCzynnosciPojazdu,
                (b, rid) => PytanieOListeCzynnosciPojazduEnvelope.Create(b, rid),
                PytanieOListeCzynnosciPojazduResponseXmlMapper.Parse,
                b => new PytanieOListeCzynnosciPojazduRequestValidator().ValidateAndNormalize(b).ToProxyResponseOrNull(SourceName, requestId),
                ct);
        //public async Task<ProxyResponse<PytanieOListeCzynnosciPojazduResponse>> PytanieOListeCzynnosciPojazduAsync(
        //    PytanieOListeCzynnosciPojazduRequest body,
        //    string? requestId = null,
        //    CancellationToken ct = default)
        //{
        //    requestId ??= Guid.NewGuid().ToString("N");

        //    // 1) Walidacja
        //    var validator = new PytanieOListeCzynnosciPojazduRequestValidator();
        //    var vr = validator.ValidateAndNormalize(body);
        //    if (!vr.IsValid)
        //    {
        //        var baseResp = vr.ToProxyResponse(requestId);
        //        return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
        //        {
        //            RequestId = baseResp.RequestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = baseResp.Status,
        //            SourceStatusCode = baseResp.SourceStatusCode,
        //            ErrorMessage = baseResp.ErrorMessage
        //        };
        //    }

        //    // 2) Koperta SOAP
        //    var envelope = PytanieOListeCzynnosciPojazduEnvelope.Create(body, requestId);

        //    // 3) Endpoint
        //    var endpointUrl = _cfg.ShareServiceUrl;
        //    if (string.IsNullOrWhiteSpace(endpointUrl))
        //    {
        //        return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
        //            ErrorMessage = "Brak ShareServiceUrl w konfiguracji CEP."
        //        };
        //    }

        //    // 4) SOAPAction
        //    const string soapAction = CEPUdostepnianieSoapActions.PytanieOListeCzynnosciPojazdu;

        //    try
        //    {
        //        // 5) Wywołanie SOAP
        //        var (status, xml) = await _invoker.InvokeAsync(_cfg, endpointUrl, soapAction, envelope, requestId, ct);

        //        if ((int)status < 200 || (int)status >= 300)
        //        {
        //            return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
        //            {
        //                RequestId = requestId,
        //                Source = "CEP.Udostepnianie",
        //                Status = ProxyStatus.TechnicalError,
        //                SourceStatusCode = ((int)status).ToString(),
        //                ErrorMessage = $"HTTP {(int)status}"
        //            };
        //        }

        //        // 6) Mapowanie XML → DTO
        //        var dto = PytanieOListeCzynnosciPojazduResponseXmlMapper.Parse(xml);

        //        return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.Success,
        //            SourceStatusCode = ((int)HttpStatusCode.OK).ToString(),
        //            Data = dto
        //        };
        //    }
        //    catch (SoapIntegrationException sie)
        //    {
        //        _logger.LogError(sie, "SOAP error ({Action}) RequestId={RequestId}", soapAction, requestId);
        //        return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int?)(sie.HttpStatus ?? HttpStatusCode.BadGateway)).ToString(),
        //            ErrorMessage = sie.Message
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Błąd PytanieOListeCzynnosciPojazduAsync, RequestId={RequestId}", requestId);
        //        return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
        //            ErrorMessage = ex.Message
        //        };
        //    }
        //}

        public Task<ProxyResponse<PytanieOHistorieLicznikaResponse>> PytanieOHistorieLicznikaAsync(
            PytanieOHistorieLicznikaRequest body, string requestId, CancellationToken ct = default) =>
            CallAsync(
                body, requestId,
                CEPUdostepnianieSoapActions.PytanieOHistorieLicznika,
                (b, rid) => PytanieOHistorieLicznikaEnvelope.Create(b, rid),
                PytanieOHistorieLicznikaResponseXmlMapper.Parse,
                b => new PytanieOHistorieLicznikaRequestValidator().ValidateAndNormalize(b).ToProxyResponseOrNull(SourceName,requestId),
                ct);
        //public async Task<ProxyResponse<PytanieOHistorieLicznikaResponse>> PytanieOHistorieLicznikaAsync(
        //    PytanieOHistorieLicznikaRequest body,
        //    string? requestId = null,
        //    CancellationToken ct = default)
        //{
        //    requestId ??= Guid.NewGuid().ToString("N");

        //    // 1) Walidacja
        //    var validator = new PytanieOHistorieLicznikaRequestValidator();
        //    var vr = validator.ValidateAndNormalize(body);
        //    if (!vr.IsValid)
        //    {
        //        var baseResp = vr.ToProxyResponse(requestId);
        //        return new ProxyResponse<PytanieOHistorieLicznikaResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = baseResp.Status,
        //            SourceStatusCode = baseResp.SourceStatusCode,
        //            ErrorMessage = baseResp.ErrorMessage
        //        };
        //    }

        //    // 2) Koperta SOAP
        //    var envelope = PytanieOHistorieLicznikaEnvelope.Create(body, requestId);

        //    // 3) Endpoint
        //    var endpointUrl = _cfg.ShareServiceUrl;
        //    if (string.IsNullOrWhiteSpace(endpointUrl))
        //    {
        //        return new ProxyResponse<PytanieOHistorieLicznikaResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
        //            ErrorMessage = "Brak ShareServiceUrl w konfiguracji CEP."
        //        };
        //    }

        //    // 4) SOAPAction
        //    const string soapAction = CEPUdostepnianieSoapActions.PytanieOHistorieLicznika;

        //    try
        //    {
        //        // 5) Wywołanie SOAP
        //        var (status, xml) = await _invoker.InvokeAsync(_cfg, endpointUrl, soapAction, envelope, requestId, ct);

        //        if ((int)status < 200 || (int)status >= 300)
        //        {
        //            return new ProxyResponse<PytanieOHistorieLicznikaResponse>
        //            {
        //                RequestId = requestId,
        //                Source = "CEP.Udostepnianie",
        //                Status = ProxyStatus.TechnicalError,
        //                SourceStatusCode = ((int)status).ToString(),
        //                ErrorMessage = $"HTTP {(int)status}"
        //            };
        //        }

        //        // 6) Mapowanie XML → DTO
        //        var dto = PytanieOHistorieLicznikaResponseXmlMapper.Parse(xml);

        //        return new ProxyResponse<PytanieOHistorieLicznikaResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.Success,
        //            SourceStatusCode = ((int)HttpStatusCode.OK).ToString(),
        //            Data = dto
        //        };
        //    }
        //    catch (SoapIntegrationException sie)
        //    {
        //        _logger.LogError(sie, "SOAP error ({Action}) RequestId={RequestId}", soapAction, requestId);
        //        return new ProxyResponse<PytanieOHistorieLicznikaResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int?)(sie.HttpStatus ?? HttpStatusCode.BadGateway)).ToString(),
        //            ErrorMessage = sie.Message
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Błąd PytanieOHistorieLicznikaAsync, RequestId={RequestId}", requestId);
        //        return new ProxyResponse<PytanieOHistorieLicznikaResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
        //            ErrorMessage = ex.Message
        //        };
        //    }
        //}

        public Task<ProxyResponse<PytanieOPodmiotResponse>> PytanieOPodmiotAsync(
            PytanieOPodmiotRequest body, string requestId, CancellationToken ct = default) =>
            CallAsync(
                body, requestId,
                CEPUdostepnianieSoapActions.PytanieOPodmiot,
                (b, rid) => PytanieOPodmiotEnvelope.Create(b, rid),
                PytanieOPodmiotResponseXmlMapper.Parse,
                b => new PytanieOPodmiotRequestValidator().ValidateAndNormalize(b).ToProxyResponseOrNull(SourceName,requestId),
                ct);
        //public async Task<ProxyResponse<PytanieOPodmiotResponse>> PytanieOPodmiotAsync(
        //    PytanieOPodmiotRequest body,
        //    string? requestId = null,
        //    CancellationToken ct = default)
        //{
        //    requestId ??= Guid.NewGuid().ToString("N");

        //    // 1) Walidacja
        //    var validator = new PytanieOPodmiotRequestValidator();
        //    var vr = validator.ValidateAndNormalize(body);
        //    if (!vr.IsValid)
        //    {
        //        var baseResp = vr.ToProxyResponse(requestId);
        //        return new ProxyResponse<PytanieOPodmiotResponse>
        //        {
        //            RequestId = baseResp.RequestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = baseResp.Status,
        //            SourceStatusCode = baseResp.SourceStatusCode,
        //            ErrorMessage = baseResp.ErrorMessage
        //        };
        //    }

        //    // 2) Koperta SOAP
        //    var envelope = PytanieOPodmiotEnvelope.Create(body, requestId);

        //    // 3) Endpoint
        //    var endpointUrl = _cfg.ShareServiceUrl;
        //    if (string.IsNullOrWhiteSpace(endpointUrl))
        //    {
        //        return new ProxyResponse<PytanieOPodmiotResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
        //            ErrorMessage = "Brak ShareServiceUrl w konfiguracji CEP."
        //        };
        //    }

        //    // 4) SOAPAction
        //    const string soapAction = CEPUdostepnianieSoapActions.PytanieOPodmiot;

        //    try
        //    {
        //        // 5) Wywołanie
        //        var (status, xml) = await _invoker.InvokeAsync(_cfg, endpointUrl, soapAction, envelope, requestId, ct);

        //        if ((int)status < 200 || (int)status >= 300)
        //        {
        //            return new ProxyResponse<PytanieOPodmiotResponse>
        //            {
        //                RequestId = requestId,
        //                Source = "CEP.Udostepnianie",
        //                Status = ProxyStatus.TechnicalError,
        //                SourceStatusCode = ((int)status).ToString(),
        //                ErrorMessage = $"HTTP {(int)status}"
        //            };
        //        }

        //        // 6) Mapowanie XML → DTO
        //        var dto = PytanieOPodmiotResponseXmlMapper.Parse(xml);

        //        return new ProxyResponse<PytanieOPodmiotResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.Success,
        //            SourceStatusCode = ((int)HttpStatusCode.OK).ToString(),
        //            Data = dto
        //        };
        //    }
        //    catch (SoapIntegrationException sie)
        //    {
        //        _logger.LogError(sie, "SOAP error ({Action}) RequestId={RequestId}", soapAction, requestId);
        //        return new ProxyResponse<PytanieOPodmiotResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int?)(sie.HttpStatus ?? HttpStatusCode.BadGateway)).ToString(),
        //            ErrorMessage = sie.Message
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Błąd PytanieOPodmiotAsync, RequestId={RequestId}", requestId);
        //        return new ProxyResponse<PytanieOPodmiotResponse>
        //        {
        //            RequestId = requestId,
        //            Source = "CEP.Udostepnianie",
        //            Status = ProxyStatus.TechnicalError,
        //            SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
        //            ErrorMessage = ex.Message
        //        };
        //    }
        //}
    }
}
