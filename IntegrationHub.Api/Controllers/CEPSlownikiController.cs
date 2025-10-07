using IntegrationHub.Common.Contracts;                 // + ProxyResponse, ProxyStatus
using IntegrationHub.Infrastructure.Cepik;
using IntegrationHub.Sources.CEP.Contracts;
using IntegrationHub.Sources.CEP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;              // (opcjonalnie) ładniejszy opis w Swagger
using System.Net;

namespace IntegrationHub.Api.Controllers;

[ApiController]
[Route("CEP/slowniki")]
[Authorize(Roles = "User")]
public sealed class CEPSlownikiController : ControllerBase
{
    private readonly ICEPSlownikiService _service;
    private readonly ILogger<CEPSlownikiController> _logger;
    private readonly ICepikDictionaryRepository _dictRepo;
    

    public CEPSlownikiController(ICEPSlownikiService service, ILogger<CEPSlownikiController> logger, ICepikDictionaryRepository dictRepo)
    {
        _service = service;
        _logger = logger;
        _dictRepo = dictRepo;
    }

    /// <summary>Lista nagłówków słowników (CEP – pobierzListeSlownikow).</summary>
    // GET CEP/slowniki/lista
    [SwaggerOperation(Summary = "Pobiera listę słowników z CEP.")]
    [SwaggerResponse(StatusCodes.Status200OK,
        "Wynik operacji lub błąd biznesowy/techniczny",
        typeof(ProxyResponse<List<SlownikNaglowekDto>>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized,
        "Unauthorized – brak autoryzacji")]
    [SwaggerResponse(StatusCodes.Status403Forbidden,
        "Forbidden – brak uprawnień")]
    [HttpGet("lista")]
    [ProducesResponseType(typeof(ProxyResponse<List<SlownikNaglowekDto>>), StatusCodes.Status200OK)]
    public async Task<ProxyResponse<List<SlownikNaglowekDto>>> PobierzListeSlownikow(CancellationToken ct)
    {
        var requestId = Guid.NewGuid().ToString();

        try
        {
            var data = await _service.PobierzListeSlownikowAsync(ct)
                       ?? new List<SlownikNaglowekDto>();

            return new ProxyResponse<List<SlownikNaglowekDto>>
            {
                RequestId = requestId,
                Source = "CEP",
                Status = ProxyStatus.Success,
                SourceStatusCode = ((int)HttpStatusCode.OK).ToString(),
                Data = data
            };
        }
        catch (OperationCanceledException)
        {
            // przekazujemy dalej — nie „opakowujemy” anulowania
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd w CEP/pobierzListeSlownikow. RequestId={RequestId}", requestId);

            return new ProxyResponse<List<SlownikNaglowekDto>>
            {
                RequestId = requestId,
                Source = "CEP",
                Status = ProxyStatus.TechnicalError,          // jak w SRP przy błędach technicznych
                SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>Słownik: Typ dokumentu pojazdu (DICT_232).</summary>
    // GET CEP/slowniki/typ-dokumentu-pojazdu
    [SwaggerOperation(Summary = "Pobiera słownik 'Typ dokumentu pojazdu' (DICT_232).")]
    [SwaggerResponse(StatusCodes.Status200OK,
        "Wynik operacji lub błąd biznesowy/techniczny",
        typeof(ProxyResponse<List<WartoscSlownikowaDto>>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized,
        "Unauthorized – brak autoryzacji")]
    [SwaggerResponse(StatusCodes.Status403Forbidden,
        "Forbidden – brak uprawnień")]
    [HttpGet("typ-dokumentu-pojazdu")]
    [ProducesResponseType(typeof(ProxyResponse<List<WartoscSlownikowaDto>>), StatusCodes.Status200OK)]
    public async Task<ProxyResponse<List<WartoscSlownikowaDto>>> PobierzSlownikTypDokumentuPojazdu(
        [FromQuery] DateTime? onDate,
        CancellationToken ct)
    {
        const string idSlownika = "DICT_232";
        var requestId = Guid.NewGuid().ToString();

        try
        {
            var items = await _dictRepo.GetAsync(idSlownika, onDate, ct);
            var data = items.Select(x => new WartoscSlownikowaDto
            {
                Kod = x.Kod,
                WartoscOpisowa = x.WartoscOpisowa,
                IdentyfikatorSlownika = idSlownika,
            }).ToList();

            return new ProxyResponse<List<WartoscSlownikowaDto>>
            {
                RequestId = requestId,
                Source = "IntegrationHubDB",
                Status = ProxyStatus.Success,
                SourceStatusCode = ((int)HttpStatusCode.OK).ToString(),
                Data = data
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd w CEP/slowniki/typ-dokumentu-pojazdu. RequestId={RequestId}", requestId);

            return new ProxyResponse<List<WartoscSlownikowaDto>>
            {
                RequestId = requestId,
                Source = "IntegrationHubDB",
                Status = ProxyStatus.TechnicalError,
                SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
                ErrorMessage = ex.Message
            };
        }
    }

}
