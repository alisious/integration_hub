using IntegrationHub.Common.Config;
using IntegrationHub.Common.Primitives;
using IntegrationHub.Sources.SRP.Config;
using IntegrationHub.Sources.SRP.Contracts;
using IntegrationHub.Sources.SRP.Extensions;
using IntegrationHub.Sources.SRP.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace IntegrationHub.Sources.SRP.Services
{
    public sealed class RdoServiceTest : IRdoService
    {
        private readonly SrpConfig _srp;
        private readonly ISrpSoapInvoker _invoker;
        private readonly ILogger<RdoService> _logger;
        private readonly string _testDataDir;

        public RdoServiceTest(IOptions<SrpConfig> srpConfig, ISrpSoapInvoker invoker, ILogger<RdoService> logger, IHostEnvironment env)
        {
            _srp = srpConfig.Value;
            _invoker = invoker;
            _logger = logger;
            _testDataDir = System.IO.Path.Combine(env.ContentRootPath, "TestData", "SRP");
        }

        public Task<Result<GetCurrentPhotoResponse, Error>> GetCurrentPhotoAsync(GetCurrentPhotoRequest body, string? requestId = null, CancellationToken ct = default)
        {
            return Task.FromResult<Result<GetCurrentPhotoResponse, Error>>(
                ErrorFactory.TechnicalError(ErrorCodeEnum.UnexpectedError, "GetCurrentPhotoAsync w trybie testowym nie jest zaimplementowane."));
        }

        /// <summary>
        /// Udostępnia aktualne dane z dowodu osobistego na podstawie numeru PESEL.
        /// Dane testowe dla PESEL = 11111111111.
        /// </summary>
        public async Task<Result<GetCurrentIdByPeselResponse, Error>> GetCurrentIdByPeselAsync(
            GetCurrentIdByPeselRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString();

            if (string.IsNullOrWhiteSpace(body.Pesel))
            {
                return ErrorFactory.BusinessError(ErrorCodeEnum.ValidationError, "PESEL jest wymagany.", (int)HttpStatusCode.BadRequest);
            }

            var xmlPath = Path.Combine(_testDataDir, "UdostepnijDaneAktualnegoDowoduPoPesel_RESPONSE.xml");

            try
            {
                if (body.Pesel != "11111111111")
                {
                    _logger.LogWarning("SRP:RDO Brak danych dowodu osobistego w odpowiedzi SRP (PESEL={Pesel}) RID={RID}", body.Pesel, requestId);
                    return ErrorFactory.BusinessError(ErrorCodeEnum.NotFoundError, "Brak danych dowodu osobistego dla podanego numeru PESEL.", (int)HttpStatusCode.NotFound);
                }

                var xml = await File.ReadAllTextAsync(xmlPath, ct).ConfigureAwait(false);
                var responseObj = RdoGetCurrentIdResponseXmlMapper.Parse(xml);

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
