// IntegrationHub.Sources.CEP.UpKi/Services/UpKiServiceTest.cs
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IntegrationHub.Common.Primitives;                    // Result<T,Error>
using IntegrationHub.Sources.CEP.UpKi.Contracts;           // DaneDokumentuRequestDto, DaneDokumentuResponseDto
using IntegrationHub.Sources.CEP.UpKi.Services;            // IUpKiService

namespace IntegrationHub.Sources.CEP.UpKi.Services
{
    /// <summary>
    /// Testowa implementacja UpKi – zwraca dane z pliku JSON:
    ///   <contentRoot>\TestData\CEP\upki_dane_dokumentu_RESPONSE.json
    /// Obsługiwany jest wyłącznie PESEL 73020916558.
    /// </summary>
    public sealed class UpKiServiceTest : IUpKiService
    {
        private const string ExpectedPesel = "73020916558";
        private readonly ILogger<UpKiServiceTest> _logger;
        private readonly string _jsonPath;

        public UpKiServiceTest(ILogger<UpKiServiceTest> logger, IHostEnvironment env)
        {
            _logger = logger;
            var testDir = Path.Combine(env.ContentRootPath, "TestData", "CEP");
            _jsonPath = Path.Combine(testDir, "upki_dane_dokumentu_RESPONSE.json");
        }

        public async Task<Result<DaneDokumentuResponseDto, Error>> GetDriverPermissionsAsync(
            DaneDokumentuRequestDto body, CancellationToken ct = default)
        {
            var rid = Guid.NewGuid().ToString("N");

            try
            {
                // Walidacja wejścia – w trybie testowym wymagamy PESEL-a
                var pesel = body?.DanePesel?.NumerPesel;
                if (string.IsNullOrWhiteSpace(pesel))
                {
                    return new Error(
                        Code: "BusinessError.MissingPesel",
                        Message: "W trybie testowym wymagany jest numer PESEL w polu danePesel.numerPesel.",
                        HttpStatus: (int)HttpStatusCode.BadRequest,
                        Details: rid);
                }

                if (!string.Equals(pesel, ExpectedPesel, StringComparison.Ordinal))
                {
                    return new Error(
                        Code: "BusinessError.UnsupportedPesel",
                        Message: $"Brak danych testowych dla PESEL {pesel}. Obsługiwany tylko {ExpectedPesel}.",
                        HttpStatus: (int)HttpStatusCode.BadRequest,
                        Details: rid);
                }

                // Plik z danymi testowymi
                if (!File.Exists(_jsonPath))
                {
                    return new Error(
                        Code: "TechnicalError.TestDataNotFound",
                        Message: $"Brak pliku z danymi testowymi: {_jsonPath}",
                        HttpStatus: (int)HttpStatusCode.InternalServerError,
                        Details: rid);
                }

                // Odczyt i deserializacja DTO z JSON
                await using var fs = File.OpenRead(_jsonPath);
                var dto = await JsonSerializer.DeserializeAsync<DaneDokumentuResponseDto>(
                    fs,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    },
                    ct);

                if (dto is null)
                {
                    return new Error(
                        Code: "TechnicalError.Deserialization",
                        Message: "Nie udało się zdeserializować danych testowych UpKi.",
                        HttpStatus: (int)HttpStatusCode.InternalServerError,
                        Details: rid);
                }

                // Sukces
                return dto;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning("UpKiServiceTest operation canceled. RID={RequestId}", rid);
                return new Error(
                    Code: "TechnicalError.Canceled",
                    Message: "Operacja została anulowana.",
                    HttpStatus: (int)HttpStatusCode.RequestTimeout,
                    Details: rid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpKiServiceTest failure. RID={RequestId}", rid);
                return new Error(
                    Code: "TechnicalError.Exception",
                    Message: ex.Message,
                    HttpStatus: (int)HttpStatusCode.InternalServerError,
                    Details: rid);
            }
        }
    }
}
