using IntegrationHub.Common.Config;
using IntegrationHub.Common.Contracts;
using IntegrationHub.Common.Primitives;
using IntegrationHub.SRP.Config;
using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.ServiceModel;


namespace IntegrationHub.SRP.Services
{
    public sealed class RdoService : IRdoService
    {
        private readonly SrpConfig _srp;
        private readonly ISrpSoapInvoker _invoker;
        private readonly ILogger<RdoService> _logger;
       

        public RdoService(IOptions<SrpConfig> srpConfig, ISrpSoapInvoker invoker, ILogger<RdoService> logger)
        {
            _srp = srpConfig.Value;
            _invoker = invoker;
            _logger = logger;
        }

        public async Task<Result<GetCurrentPhotoResponse, Error>> GetCurrentPhotoAsync(GetCurrentPhotoRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString();

            var hasPesel = !string.IsNullOrWhiteSpace(body.Pesel);
            var hasPersonId = !string.IsNullOrWhiteSpace(body.IdOsoby);
            if (!(hasPesel && hasPersonId))
            {
                return ErrorFactory.BusinessError(ErrorCodeEnum.ValidationError, "Brak numeru PESEL i ID osoby do wyszukania zdjęcia.", (int)HttpStatusCode.BadRequest);
            }

            var envelope = RequestEnvelopeHelper.PrepareGetCurrentPhotoRequestEnvelope(body, requestId);

            try
            {
                var result = await _invoker.InvokeAsync(_srp.RdoShareServiceUrl, SrpSoapActions.Rdo_UdostepnijAktualneZdjecie,
                                                        envelope, requestId, ct);

                if (result.Fault is not null)
                {
                    var msg = result.Fault.DetailOpis ?? result.Fault.FaultString;
                    if (!string.IsNullOrWhiteSpace(result.Fault.DetailOpisTechniczny))
                        msg += $"; {result.Fault.DetailOpisTechniczny}";

                    return ErrorFactory.BusinessError(ErrorCodeEnum.ExternalServiceError, msg, (int)HttpStatusCode.BadRequest);
                }

                if ((int)result.StatusCode < 200 || (int)result.StatusCode >= 300)
                {
                    return ErrorFactory.TechnicalError(ErrorCodeEnum.ExternalServiceError, $"HTTP {(int)result.StatusCode}", (int)result.StatusCode);
                }

                var parsed = RdoGetCurrentPhotoResponseXmlMapper.Parse(
                    result.Body, _logger, requestId, validateBase64: true, snippetChars: 16);

                return parsed;
            }
            catch (TimeoutException)
            {
                return ErrorFactory.TechnicalError(ErrorCodeEnum.OperationCanceledError, "Przekroczono czas oczekiwania na odpowiedz uslugi SRP: udostepnijAktualneZdjecie.", (int)HttpStatusCode.RequestTimeout);
            }
            catch (CommunicationException cex)
            {
                return ErrorFactory.TechnicalError(ErrorCodeEnum.ExternalServiceError, $"Blad komunikacji z usluga SRP: udostepnijAktualneZdjecie. {cex.Message}", (int)HttpStatusCode.BadGateway);
            }
            catch (Exception ex)
            {
                return ErrorFactory.TechnicalError(ErrorCodeEnum.UnexpectedError, ex.Message, (int)HttpStatusCode.InternalServerError);
            }
        }


        /// <summary>
        /// Udostępnia aktualne dane z dowodu osobistego na podstawie numeru PESEL.
        /// </summary>
        public async Task<Result<GetCurrentIdByPeselResponse, Error>> GetCurrentIdByPeselAsync(
            GetCurrentIdByPeselRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString();

            if (string.IsNullOrWhiteSpace(body.Pesel))
            {
                return ErrorFactory.BusinessError(ErrorCodeEnum.ValidationError, "PESEL jest wymagany.", (int)HttpStatusCode.BadRequest);
            }

            var envelope = RequestEnvelopeHelper.PrepareGetCurrentIdByPeselRequestEnvelope(body, requestId);

            try
            {
                var result = await _invoker.InvokeAsync(
                    _srp.RdoShareServiceUrl,
                    SrpSoapActions.Rdo_UdostepnijDaneAktualnegoDowoduPoPesel,
                    envelope,
                    requestId,
                    ct);

                if (result.Fault is not null)
                    return ErrorFactory.BusinessError(ErrorCodeEnum.ExternalServiceError, result.Fault.FaultString, (int)HttpStatusCode.BadRequest);

                if ((int)result.StatusCode < 200 || (int)result.StatusCode >= 300)
                    return ErrorFactory.TechnicalError(ErrorCodeEnum.ExternalServiceError, $"HTTP {(int)result.StatusCode}", (int)result.StatusCode);

                var responseObj = RdoGetCurrentIdResponseXmlMapper.Parse(result.Body);

                if (responseObj.Dowod is null)
                {
                    _logger.LogWarning("SRP:RDO Brak danych dowodu osobistego w odpowiedzi SRP (PESEL={Pesel}) RID={RID}", body.Pesel, requestId);
                    return ErrorFactory.BusinessError(ErrorCodeEnum.NotFoundError, "Brak danych dowodu osobistego dla podanego numeru PESEL.", (int)HttpStatusCode.NotFound);
                }

                return responseObj;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SRP:RDO Błąd UdostepnijDaneAktualnegoDowoduPoPesel (PESEL={Pesel}) RID={RID}", body.Pesel, requestId);
                return ErrorFactory.TechnicalError(ErrorCodeEnum.UnexpectedError, ex.Message, (int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
