// IntegrationHub.Sources.KSIP/Services/KSIPServiceTest.cs
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IntegrationHub.Common.Primitives;         // Result<T, Error>, ErrorFactory, ErrorCodeEnum
using IntegrationHub.Sources.KSIP.Contracts;    // SprawdzenieOsobyRequest, SprawdzenieOsobyResponse
using IntegrationHub.Sources.KSIP.Mappers;      // SprawdzenieOsobyResponseMapper
using IntegrationHub.Sources.KSIP.RequestValidation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IntegrationHub.Sources.KSIP.Services
{
    /// <summary>
    /// Testowa implementacja IKSIPService.
    /// Zamiast wywoływać prawdziwy endpoint KSIP, zwraca odpowiedzi z plików XML:
    /// TestData\KSIP\dziodko_RESPONSE.xml,
    /// TestData\KSIP\OsobaJest_State0_RESPONSE.xml,
    /// TestData\KSIP\NotFound_RESPONSE.xml.
    /// </summary>
    public sealed class KSIPServiceTest : IKSIPService
    {
        private readonly ILogger<KSIPServiceTest> _logger;
        private readonly string _testDataDir;

        public KSIPServiceTest(ILogger<KSIPServiceTest> logger, IHostEnvironment env)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (env is null) throw new ArgumentNullException(nameof(env));

            // <contentRoot>\TestData\KSIP  – analogicznie do UpKiServiceTest (TestData\CEK)
            _testDataDir = Path.Combine(env.ContentRootPath, "TestData", "KSIP");
        }

        public Task<Result<SprawdzenieOsobyResponse, Error>> SprawdzenieOsobyWRuchuDrogowymAsync(
            SprawdzenieOsobyRequest body,
            string requestId,
            CancellationToken ct = default)
        {
            if (body is null)
            {
                return Task.FromResult<Result<SprawdzenieOsobyResponse, Error>>(
                    ErrorFactory.BusinessError(
                        ErrorCodeEnum.ValidationError,
                        "Body (SprawdzenieOsobyRequest) nie może być null."));
            }

            // 1) Walidacja – tak jak w KSIPService
            var validator = new SprawdzenieOsobyRequestValidator();
            var vr = validator.ValidateAndNormalize(body);
            if (!vr.IsValid)
            {
                return Task.FromResult<Result<SprawdzenieOsobyResponse, Error>>(
                    ErrorFactory.BusinessError(
                        ErrorCodeEnum.ValidationError,
                        vr.MessageError ?? "Błąd walidacji SprawdzenieOsobyRequest."));
            }

            // 2) Decyzja jaki plik XML zwrócić na podstawie parametrów
            var nrPesel = (body.NrPesel ?? string.Empty).Trim();
            var firstName = (body.FirstName ?? string.Empty).Trim().ToUpperInvariant();
            var lastName = (body.LastName ?? string.Empty).Trim().ToUpperInvariant();
            var birthDate = (body.BirthDate ?? string.Empty).Trim(); // zakładam string "yyyy-MM-dd"
            var getByPesel = !String.IsNullOrEmpty(nrPesel); 


            string fileName;

            // Zestaw 2.x – MATEUSZ DZIODKO 2003-09-09 / PESEL 03290901192
            if (nrPesel == "03290901192" ||
                (firstName == "MATEUSZ" && lastName == "DZIODKO" && birthDate == "2003-09-09"))
            {
                fileName = "dziodko_RESPONSE.xml";
            }
            // Zestaw 3.x – EDYTA KOROLCZUK 1981-04-07 / PESEL 81040709629
            else if (nrPesel == "81040709629" ||
                     (firstName == "EDYTA" && lastName == "KOROLCZUK" && birthDate == "1981-04-07"))
            {
                fileName = "OsobaJest_State0_RESPONSE.xml";
            }
            // Inne parametry – NotFound
            else
            {
                fileName = "NotFound_RESPONSE.xml";
            }

            try
            {
                var xml = LoadResponseXml(fileName);

                // 3) Mapowanie SOAP → DTO
                var dto = SprawdzenieOsobyResponseMapper.MapFromSoapEnvelope(xml);

                _logger.LogInformation(
                    "KSIPServiceTest – zwrócono dane z pliku {FileName} dla requestId={RequestId}.",
                    fileName, requestId);

                //Jeżeli nie znaleziono osoby zwróć BusinessError 
                if (dto.State == 0)
                {
                    var msg = "";
                    if (getByPesel)
                    {
                        msg = $"Nie znaleziono wpisów dla osoby: PESEL = {nrPesel}.";
                    }
                    else
                    { 
                        msg = $"Nie znaleziono wpisów dla osoby: {firstName} {lastName}, ur. {birthDate}.";
                    }

                    _logger.LogWarning(msg);
                    return Task.FromResult<Result<SprawdzenieOsobyResponse, Error>>(
                        ErrorFactory.BusinessError(
                            ErrorCodeEnum.NotFoundError,
                            message: msg));
                }
                // implicit conversion SprawdzenieOsobyResponse -> Result<SprawdzenieOsobyResponse, Error>
                return Task.FromResult<Result<SprawdzenieOsobyResponse, Error>>(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "KSIPServiceTest – błąd podczas odczytu lub mapowania pliku XML {FileName}.",
                    fileName);

                return Task.FromResult<Result<SprawdzenieOsobyResponse, Error>>(
                    ErrorFactory.TechnicalError(
                        ErrorCodeEnum.ExternalServiceError,
                        message: $"Błąd testowego serwisu KSIP podczas przetwarzania pliku {fileName}. Szczegóły: {ex.Message}"));
            }
        }

        private string LoadResponseXml(string fileName)
        {
            var xmlPath = Path.Combine(_testDataDir, fileName);

            if (!File.Exists(xmlPath))
            {
                throw new FileNotFoundException($"Nie znaleziono pliku z danymi testowymi KSIP: {xmlPath}", xmlPath);
            }

            return File.ReadAllText(xmlPath);
        }
    }
}
