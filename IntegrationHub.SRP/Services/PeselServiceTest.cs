using IntegrationHub.Common.Config;
using IntegrationHub.Common.Helpers;
using IntegrationHub.Common.Primitives;
using IntegrationHub.SRP.Config;
using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Net;
using static IntegrationHub.SRP.Services.SrpServiceCommon;

namespace IntegrationHub.SRP.Services
{
    public sealed class PeselServiceTest : IPeselService
    {
        private readonly SrpConfig _srp;
        //private readonly ISrpSoapInvoker _invoker;
        private readonly ILogger<PeselServiceTest> _logger;
        private readonly string _testDataDir;

        //public PeselServiceTest(IOptions<SrpConfig> srpConfig, ISrpSoapInvoker invoker, ILogger<PeselServiceTest> logger,IHostEnvironment env)
        public PeselServiceTest(IOptions<SrpConfig> srpConfig, ILogger<PeselServiceTest> logger, IHostEnvironment env)
        {
            _srp = srpConfig.Value;
            //_invoker = invoker;
            _logger = logger;
            _testDataDir = System.IO.Path.Combine(env.ContentRootPath, "TestData","SRP");// <contentRoot>\TestData\SRP
        }
        /// <summary>
        /// Pobieranie danych osoby na podstawie numeru PESEL. Metoda zwraca dane testowe dla pesel = 11111111111.
        /// </summary>
        public async Task<Result<GetPersonByPeselResponse, Error>> GetPersonByPeselAsync(GetPersonByPeselRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString();

            if (string.IsNullOrWhiteSpace(body.Pesel))
            {
                return ErrorFactory.BusinessError(ErrorCodeEnum.ValidationError, "Brak wymaganego parametru: pesel", (int)HttpStatusCode.BadRequest);
            }

            try
            {
                if (body.Pesel != "11111111111")
                {
                    _logger.LogWarning("SRP:RDO Brak danych osoby w odpowiedzi SRP (PESEL={Pesel}) RID={RID}", body.Pesel, requestId);
                    return ErrorFactory.BusinessError(ErrorCodeEnum.NotFoundError, "Brak danych osoby dla podanego numeru PESEL.", (int)HttpStatusCode.NotFound);
                }

                var xmlPath = Path.Combine(_testDataDir, "UdostepnijAktualneDaneOsobyPoPesel_RESPONSE.xml");
                var xml = await File.ReadAllTextAsync(xmlPath, ct).ConfigureAwait(false);
                var responseObj = PeselGetPersonByPeselResponseXmlMapper.Parse(xml);

                return responseObj;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd GetPerson, RequestId: {RequestId}", requestId);
                return ErrorFactory.TechnicalError(ErrorCodeEnum.UnexpectedError, ex.Message, (int)HttpStatusCode.InternalServerError);
            }
        }

        
        /// <summary>
        /// Wysyłanie zapytania o wyszukanie osoby na podstawie podanych kryteriów. Metoda zwraca dane testowe.
        /// Dostępne dane testowe w plikach XML w folderze PeselTestData: 
        /// 1. Parametry: pesel = 11111111111 - zwraca 1 osobę.
        /// 2. Parametry: nazwisko = NOWAK, imiePierwsze = TOMASZ, imięOjca = KAZIMIERZ i kryterium daty urodzena: dataOd = 19701001 i dataDo = 19731231 - zwraca 13 osób. 
        /// 3. Parametry: nazwisko = NOWAK, imiePierwsze = TOMASZ - zwraca bład biznesowy - Znaleziono więcej niż 50 osób!.
        /// 4. Inne parametry - zwraca 0 osób.
        /// </summary>
        /// <param name="body">The request containing the search criteria for the person. Cannot be null.</param>
        /// <param name="requestId">An optional identifier for tracking the request. Can be null.</param>
        /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
        async Task<Result<SearchPersonResponse, Error>> IPeselService.SearchPersonAsync(SearchPersonRequest body, string? requestId, CancellationToken ct)
        {
            requestId ??= Guid.NewGuid().ToString();

            if (!TryValidateAndNormalize(body, requestId, allowRange: true, out var err))
                return err!.Value;

            var jsonPath = Path.Combine(_testDataDir, "SearchPersonResponse_13.json");

            SearchPersonResponse resp;
            try
            {
                await using var fs = File.OpenRead(jsonPath);
                var proxy = await JsonSerializer.DeserializeAsync<IntegrationHub.Common.Contracts.ProxyResponse<SearchPersonResponse>>(fs, JsonOpts, ct);
                resp = proxy?.Data ?? new SearchPersonResponse();
            }
            catch (FileNotFoundException)
            {
                return ErrorFactory.TechnicalError(ErrorCodeEnum.UnexpectedError, $"Brak pliku z danymi testowymi: {jsonPath}", (int)HttpStatusCode.InternalServerError);
            }
            catch (JsonException jx)
            {
                _logger.LogError(jx, "Błąd JSON przy wczytywaniu {Path}", jsonPath);
                return ErrorFactory.TechnicalError(ErrorCodeEnum.UnexpectedError, "Uszkodzony plik danych testowych (JSON).", (int)HttpStatusCode.InternalServerError);
            }

            static string N(string? s) => (s ?? "").Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(body.Pesel))
                resp.Persons = resp.Persons.Where(p => p.Pesel == body.Pesel).ToList();
            else
                resp.Persons = resp.Persons.Where(p =>
                       N(p.Nazwisko) == N(body.Nazwisko)
                    && N(p.ImiePierwsze) == N(body.ImiePierwsze)
                    && (string.IsNullOrWhiteSpace(body.ImieOjca) || N("KAZIMIERZ") == N(body.ImieOjca))
                ).ToList();

            if (!string.IsNullOrWhiteSpace(body.DataUrodzenia))
                resp.Persons = resp.Persons.Where(p => p.DataUrodzenia == body.DataUrodzenia).ToList();

            if (N(body.Nazwisko) == "NOWAK" && N(body.ImiePierwsze) == "TOMASZ" && string.IsNullOrWhiteSpace(body.ImieOjca))
            {
                return ErrorFactory.BusinessError(ErrorCodeEnum.ExternalServiceError, "Znaleziono więcej niż 50 osób!", (int)HttpStatusCode.NotAcceptable);
            }

            return resp;
        }
    }
}


