// IntegrationHub.Sources.CEP.Services/CEPUdostepnianieServiceTest.cs
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using IntegrationHub.Common.Contracts;
using IntegrationHub.Common.RequestValidation; // ValidationResult
using IntegrationHub.Sources.CEP.Services;                 // ICEPUdostepnianieService
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;  // PytanieOPojazdRequest, PytanieOPojazdResponse
using IntegrationHub.Sources.CEP.Udostepnianie.Mappers;    // PytanieOPojazdResponseXmlMapper
using IntegrationHub.Sources.CEP.Udostepnianie.RequestValidation; // PytanieOPojazdRequestValidator (+ ValidationResultExtensions)

namespace IntegrationHub.Sources.CEP.Udostepnianie.Services
{
    /// <summary>
    /// Implementacja testowa serwisu CEPUdostepnianie – czyta gotowy XML z katalogu TestData/CEP
    /// i mapuje go na DTO. Przydatne do uruchamiania API bez dostępu do środowisk zewnętrznych.
    /// </summary>
    public sealed class CEPUdostepnianieServiceTest : ICEPUdostepnianieService
    {
        private readonly ILogger<CEPUdostepnianieServiceTest> _logger;
        private readonly string _testDataDir;
        private const string SourceName = "CEP";

        public CEPUdostepnianieServiceTest(ILogger<CEPUdostepnianieServiceTest> logger, IHostEnvironment env)
        {
            _logger = logger;
            // <contentRoot>\TestData\CEP
            _testDataDir = Path.Combine(env.ContentRootPath, "TestData", "CEP");
        }

        /// <summary>
        /// Wersja testowa: ignoruje realne wywołanie SOAP i zwraca zmapowany plik XML:
        /// ..\TestData\CEP\pytanieOPojazd_RESPONSE.xml
        /// </summary>
        public async Task<ProxyResponse<PytanieOPojazdResponse>> PytanieOPojazdAsync(
            PytanieOPojazdRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString("N");

            // 1) Walidacja zgodnie z produkcyjną ścieżką
            var validator = new PytanieOPojazdRequestValidator();
            var vr = validator.ValidateAndNormalize(body);
            if (!vr.IsValid)
            {
                var baseResp = vr.ToProxyResponse(SourceName, requestId); // BusinessError / 400
                return new ProxyResponse<PytanieOPojazdResponse>
                {
                    RequestId = baseResp.RequestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = baseResp.Status,
                    SourceStatusCode = baseResp.SourceStatusCode,
                    Message = baseResp.Message
                };
            }

            // 2) Wczytaj plik testowy i zmapuj przez mapper XML→DTO
            var xmlPath = Path.Combine(_testDataDir, "pytanieOPojazd_RESPONSE.xml");
            try
            {
                if (!File.Exists(xmlPath))
                    return ProxyResponseFactory.TechnicalError<PytanieOPojazdResponse>(
                        $"Brak pliku z danymi testowymi: {xmlPath}", "CEP.Udostepnianie.Test",
                        ((int)HttpStatusCode.InternalServerError).ToString(), requestId);

                var xml = await File.ReadAllTextAsync(xmlPath, ct).ConfigureAwait(false);
                var dto = PytanieOPojazdResponseXmlMapper.Parse(xml);

                return new ProxyResponse<PytanieOPojazdResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = ProxyStatus.Success,
                    SourceStatusCode = ((int)HttpStatusCode.OK).ToString(),
                    Data = dto
                };
            }
            catch (OperationCanceledException oce) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning(oce, "Test CEP canceled by caller. RID={RequestId}", requestId);
                return Error<PytanieOPojazdResponse>(requestId, HttpStatusCode.RequestTimeout,
                    ProxyStatus.TechnicalError, "Operacja została anulowana.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd testowego odczytu CEPUdostepnianie. RID={RequestId}", requestId);
                return Error<PytanieOPojazdResponse>(requestId, HttpStatusCode.InternalServerError,
                    ProxyStatus.TechnicalError, ex.Message);
            }
        }

        public async Task<ProxyResponse<PytanieOPojazdRozszerzoneResponse>> PytanieOPojazdRozszerzoneAsync(
            PytanieOPojazdRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString("N");

            var validator = new PytanieOPojazdRequestValidator();
            var vr = validator.ValidateAndNormalize(body);
            if (!vr.IsValid)
            {
                var baseResp = vr.ToProxyResponse(SourceName, requestId);
                return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
                {
                    RequestId = baseResp.RequestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = baseResp.Status,
                    SourceStatusCode = baseResp.SourceStatusCode,
                    Message = baseResp.Message
                };
            }

            var xmlPath = Path.Combine(_testDataDir, "pytanieOPojazdRozszerzone_LU638JU_RESPONSE.xml");
            try
            {
                if (!File.Exists(xmlPath))
                {
                    return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
                    {
                        RequestId = requestId,
                        Source = "CEP.Udostepnianie.Test",
                        Status = ProxyStatus.TechnicalError,
                        SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
                        Message = $"Brak pliku z danymi testowymi: {xmlPath}"
                    };
                }

                var xml = await File.ReadAllTextAsync(xmlPath, ct).ConfigureAwait(false);
                var dto = PytanieOPojazdRozszerzoneResponseXmlMapper.Parse(xml);

                return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = ProxyStatus.Success,
                    SourceStatusCode = ((int)HttpStatusCode.OK).ToString(),
                    Data = dto
                };
            }
            catch (OperationCanceledException oce) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning(oce, "Test CEP (rozszerzone) canceled by caller. RID={RequestId}", requestId);
                return Error<PytanieOPojazdRozszerzoneResponse>(
                    requestId, HttpStatusCode.RequestTimeout, ProxyStatus.TechnicalError, "Operacja została anulowana.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd testowego odczytu CEPUdostepnianie (rozszerzone). RID={RequestId}", requestId);
                return Error<PytanieOPojazdRozszerzoneResponse>(
                    requestId, HttpStatusCode.InternalServerError, ProxyStatus.TechnicalError, ex.Message);
            }
        }

        public async Task<ProxyResponse<PytanieODokumentPojazduResponse>> PytanieODokumentPojazduAsync(
            PytanieODokumentPojazduRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString("N");

            // 1) Walidacja
            var validator = new PytanieODokumentPojazduRequestValidator();
            var vr = validator.ValidateAndNormalize(body);
            if (!vr.IsValid)
            {
                var baseResp = vr.ToProxyResponse(SourceName, requestId);
                return new ProxyResponse<PytanieODokumentPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = baseResp.Status,
                    SourceStatusCode = baseResp.SourceStatusCode,
                    Message = baseResp.Message
                };
            }

            // 2) Plik testowy
            var xmlPath = Path.Combine(_testDataDir, "pytanieODokumentPojazdu_3391181117602887_RESPONSE.xml");
            try
            {
                if (!File.Exists(xmlPath))
                {
                    return new ProxyResponse<PytanieODokumentPojazduResponse>
                    {
                        RequestId = requestId,
                        Source = "CEP.Udostepnianie.Test",
                        Status = ProxyStatus.TechnicalError,
                        SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
                        Message = $"Brak pliku z danymi testowymi: {xmlPath}"
                    };
                }

                var xml = await File.ReadAllTextAsync(xmlPath, ct).ConfigureAwait(false);
                var dto = PytanieODokumentPojazduResponseXmlMapper.Parse(xml);

                return new ProxyResponse<PytanieODokumentPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = ProxyStatus.Success,
                    SourceStatusCode = ((int)HttpStatusCode.OK).ToString(),
                    Data = dto
                };
            }
            catch (OperationCanceledException oce) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning(oce, "Test CEP canceled by caller. RID={RequestId}", requestId);
                return new ProxyResponse<PytanieODokumentPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = ((int)HttpStatusCode.RequestTimeout).ToString(),
                    Message = "Operacja została anulowana."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd testowego odczytu CEPUdostepnianie (Dokument). RID={RequestId}", requestId);
                return new ProxyResponse<PytanieODokumentPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
                    Message = ex.Message
                };
            }
        }

        public async Task<ProxyResponse<PytanieOListeCzynnosciPojazduResponse>> PytanieOListeCzynnosciPojazduAsync(
            PytanieOListeCzynnosciPojazduRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString("N");

            var validator = new PytanieOListeCzynnosciPojazduRequestValidator();
            var vr = validator.ValidateAndNormalize(body);
            if (!vr.IsValid)
            {
                var baseResp = vr.ToProxyResponse(SourceName, requestId);
                return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = baseResp.Status,
                    SourceStatusCode = baseResp.SourceStatusCode,
                    Message = baseResp.Message
                };
            }

            var xmlPath = Path.Combine(_testDataDir, "pytanieOListeCzynnosciPojazdu_6206473948761901_RESPONSE.xml");
            try
            {
                if (!File.Exists(xmlPath))
                {
                    return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
                    {
                        RequestId = requestId,
                        Source = "CEP.Udostepnianie.Test",
                        Status = ProxyStatus.TechnicalError,
                        SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
                        Message = $"Brak pliku z danymi testowymi: {xmlPath}"
                    };
                }

                var xml = await File.ReadAllTextAsync(xmlPath, ct).ConfigureAwait(false);
                var dto = PytanieOListeCzynnosciPojazduResponseXmlMapper.Parse(xml);

                return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = ProxyStatus.Success,
                    SourceStatusCode = ((int)HttpStatusCode.OK).ToString(),
                    Data = dto
                };
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = ((int)HttpStatusCode.RequestTimeout).ToString(),
                    Message = "Operacja została anulowana."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd testowego odczytu CEPUdostepnianie (ListaCzynnosci). RID={RequestId}", requestId);
                return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
                    Message = ex.Message
                };
            }
        }

        
        public async Task<ProxyResponse<PytanieOHistorieLicznikaResponse>> PytanieOHistorieLicznikaAsync(
            PytanieOHistorieLicznikaRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString("N");

            // 1) Walidacja
            var validator = new PytanieOHistorieLicznikaRequestValidator();
            var vr = validator.ValidateAndNormalize(body);
            if (!vr.IsValid)
            {
                var baseResp = vr.ToProxyResponse(SourceName, requestId);
                return new ProxyResponse<PytanieOHistorieLicznikaResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = baseResp.Status,
                    SourceStatusCode = baseResp.SourceStatusCode,
                    Message = baseResp.Message
                };
            }

            // 2) Plik testowy
            var xmlPath = Path.Combine(_testDataDir, "pytanieOHistorieLicznika_6206473948761901_RESPONSE.xml");
            try
            {
                if (!File.Exists(xmlPath))
                {
                    return new ProxyResponse<PytanieOHistorieLicznikaResponse>
                    {
                        RequestId = requestId,
                        Source = "CEP.Udostepnianie.Test",
                        Status = ProxyStatus.TechnicalError,
                        SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
                        Message = $"Brak pliku z danymi testowymi: {xmlPath}"
                    };
                }

                var xml = await File.ReadAllTextAsync(xmlPath, ct).ConfigureAwait(false);
                var dto = PytanieOHistorieLicznikaResponseXmlMapper.Parse(xml);

                return new ProxyResponse<PytanieOHistorieLicznikaResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = ProxyStatus.Success,
                    SourceStatusCode = ((int)HttpStatusCode.OK).ToString(),
                    Data = dto
                };
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return new ProxyResponse<PytanieOHistorieLicznikaResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = ((int)HttpStatusCode.RequestTimeout).ToString(),
                    Message = "Operacja została anulowana."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd testowego odczytu CEPUdostepnianie (Historia licznika). RID={RequestId}", requestId);
                return new ProxyResponse<PytanieOHistorieLicznikaResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
                    Message = ex.Message
                };
            }
        }

        // IntegrationHub.Sources.CEP.Services/CEPUdostepnianieServiceTest.cs  (fragment metody PytanieOPodmiotAsync)
        public async Task<ProxyResponse<PytanieOPodmiotResponse>> PytanieOPodmiotAsync(
            PytanieOPodmiotRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString("N");

            var validator = new PytanieOPodmiotRequestValidator();
            var vr = validator.ValidateAndNormalize(body);
            if (!vr.IsValid)
            {
                var baseResp = vr.ToProxyResponse(SourceName, requestId);
                return new ProxyResponse<PytanieOPodmiotResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = baseResp.Status,
                    SourceStatusCode = baseResp.SourceStatusCode,
                    Message = baseResp.Message
                };
            }

            // Wymóg: nazwa pliku wg identyfikatora podmiotu
            if (string.IsNullOrEmpty(body.IdentyfikatorSystemowyPodmiotu))
            {
                return Error<PytanieOPodmiotResponse>(requestId, HttpStatusCode.BadRequest,
                    ProxyStatus.BusinessError, "W trybie testowym wymagany jest identyfikatorSystemowyPodmiotu do doboru pliku testowego.");
            }

            var fileName = $"pytanieOPodmiot_{body.IdentyfikatorSystemowyPodmiotu}_RESPONSE.xml";
            var xmlPath = Path.Combine(_testDataDir, fileName);

            try
            {
                if (!File.Exists(xmlPath))
                    return Error<PytanieOPodmiotResponse>(requestId, HttpStatusCode.InternalServerError,
                        ProxyStatus.TechnicalError, $"Brak pliku z danymi testowymi: {xmlPath}");

                var xml = await File.ReadAllTextAsync(xmlPath, ct).ConfigureAwait(false);
                var dto = PytanieOPodmiotResponseXmlMapper.Parse(xml);

                return new ProxyResponse<PytanieOPodmiotResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Test",
                    Status = ProxyStatus.Success,
                    SourceStatusCode = ((int)HttpStatusCode.OK).ToString(),
                    Data = dto
                };
            }
            catch (OperationCanceledException oce) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning(oce, "Test CEP canceled by caller. RID={RequestId}", requestId);
                return Error<PytanieOPodmiotResponse>(requestId, HttpStatusCode.RequestTimeout,
                    ProxyStatus.TechnicalError, "Operacja została anulowana.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd testowego odczytu CEPUdostepnianie (Podmiot). RID={RequestId}", requestId);
                return Error<PytanieOPodmiotResponse>(requestId, HttpStatusCode.InternalServerError,
                    ProxyStatus.TechnicalError, ex.Message);
            }
        }



        // === pomocnicze ===
        private static ProxyResponse<T> Error<T>(string requestId, HttpStatusCode code, ProxyStatus status, string message)
        {
            return new ProxyResponse<T>
            {
                RequestId = requestId,
                Source = "CEP.Udostepnianie.Test",
                Status = status,
                SourceStatusCode = ((int)code).ToString(),
                Message = message
            };
        }
    }
}
