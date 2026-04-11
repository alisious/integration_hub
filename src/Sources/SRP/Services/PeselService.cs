using IntegrationHub.Common.Config;
using IntegrationHub.Common.Contracts;
using IntegrationHub.Common.Helpers;
using IntegrationHub.Common.Primitives;
using IntegrationHub.SRP.Config;
using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Extensions;
using IntegrationHub.SRP.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using static IntegrationHub.SRP.Services.SrpServiceCommon;

namespace IntegrationHub.SRP.Services
{
    public sealed class PeselService : IPeselService
    {
        private readonly SrpConfig _srpConfig;
        private readonly ISrpSoapInvoker _soapInvoker;
        private readonly ILogger<PeselService> _logger;
        private readonly IRdoService _rdoService;

        public PeselService(IOptions<SrpConfig> srpConfig, ISrpSoapInvoker soapInvoker, IRdoService rdoService, ILogger<PeselService> logger)
        {
            _srpConfig = srpConfig.Value;
            _soapInvoker = soapInvoker;
            _rdoService = rdoService;
            _logger = logger;
        }

        public async Task<Result<GetPersonByPeselResponse, Error>> GetPersonByPeselAsync(GetPersonByPeselRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString();

            if (string.IsNullOrWhiteSpace(body.Pesel))
            {
                return ErrorFactory.BusinessError(ErrorCodeEnum.ValidationError, "Brak wymaganego parametru: pesel", (int)HttpStatusCode.BadRequest);
            }

            try
            {
                var envelope = RequestEnvelopeHelper.PrepareGetPersonByPeselEnvelope(body, requestId);
                var result = await _soapInvoker.InvokeAsync(_srpConfig.PeselShareServiceUrl, SrpSoapActions.Pesel_UdostepnijAktualneDaneOsobyPoPesel,
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

                var responseObj = PeselGetPersonByPeselResponseXmlMapper.Parse(result.Body);
                if (responseObj.daneOsoby is null)
                {
                    return ErrorFactory.BusinessError(ErrorCodeEnum.NotFoundError, "Brak osoby o podanym PESEL.", (int)HttpStatusCode.NotFound);
                }

                return responseObj;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd GetPerson, RequestId: {RequestId}", requestId);
                return ErrorFactory.TechnicalError(ErrorCodeEnum.UnexpectedError, ex.Message, (int)HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Result<SearchPersonResponse, Error>> SearchPersonAsync(SearchPersonRequest body, string? requestId, CancellationToken ct)
        {
            requestId ??= Guid.NewGuid().ToString();
            if (!TryValidateAndNormalize(body, requestId, allowRange: true, out var err))
                return err!.Value;

            var envelope = RequestEnvelopeHelper.PrepareSearchPersonEnvelope(body, requestId);

            try
            {
                var result = await _soapInvoker.InvokeAsync(
                    _srpConfig.PeselSearchServiceUrl, SrpSoapActions.Pesel_WyszukajOsoby,
                    envelope, requestId, ct);

                if (result.Fault is not null)
                    return ErrorFactory.BusinessError(ErrorCodeEnum.ExternalServiceError, result.Fault.FaultString, (int)HttpStatusCode.BadRequest);

                if ((int)result.StatusCode < 200 || (int)result.StatusCode >= 300)
                    return ErrorFactory.TechnicalError(ErrorCodeEnum.ExternalServiceError, $"HTTP {(int)result.StatusCode}", (int)result.StatusCode);

                var responseObj = PeselSearchPersonResponseXmlMapper.Parse(result.Body);

                if (body.CzyZyje == true)
                    responseObj.Persons.RemoveAll(p => p.CzyZyje == false);

                var photoReqs = responseObj.Persons
                    .Where(p => p.CzyZyje == true
                             && !string.IsNullOrWhiteSpace(p.IdOsoby)
                             && !string.IsNullOrWhiteSpace(p.Pesel))
                    .Select(p => new GetCurrentPhotoRequest
                    {
                        IdOsoby = p.IdOsoby!,
                        Pesel = p.Pesel!
                    })
                    .DistinctBy(r => (r.IdOsoby, r.Pesel))
                    .ToList();

                if (photoReqs.Count > 0)
                {
                    var photoResults = await RdoBulkHelpers.BulkGetCurrentPhotosAsync(
                        _rdoService, photoReqs, maxParallel: 6, ct);

                    var byKey = photoResults.ToDictionary(
                        x => (x.Request.IdOsoby, x.Request.Pesel), x => x.Result);

                    foreach (var person in responseObj.Persons)
                    {
                        if (!string.IsNullOrWhiteSpace(person.IdOsoby) && !string.IsNullOrWhiteSpace(person.Pesel) &&
                            byKey.TryGetValue((person.IdOsoby, person.Pesel), out var pr) &&
                            pr.Status == ProxyStatus.Success && pr.Data != null)
                        {
                            person.Zdjecie = pr.Data.GetFirstPhotoOrDefault();
                        }
                    }
                }

                return responseObj;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blad SearchBasePerson, RequestId: {RequestId}", requestId);
                return ErrorFactory.TechnicalError(ErrorCodeEnum.UnexpectedError, ex.Message, (int)HttpStatusCode.InternalServerError);
            }
        }
    }
}


