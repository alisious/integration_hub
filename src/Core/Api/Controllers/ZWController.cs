using Azure.Core;
using IntegrationHub.Application.ZW;              // IZWSourceFacade
using IntegrationHub.Common.Contracts;           // ProxyResponse, ProxyResponseFactory
using IntegrationHub.Common.Primitives;
using IntegrationHub.Common.RequestValidation;   // IRequestValidator<T>, ValidationResult
using IntegrationHub.Domain.Contracts.ZW;        // WPMRequest, WPMResponse
using IntegrationHub.Sources.ZW.Contracts;
using IntegrationHub.Sources.ZW.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Net;

namespace IntegrationHub.Api.Controllers
{
    [ApiController]
    [Route("ZW")]
    [Produces("application/json")]
    [Authorize(Roles = "User")]
    public sealed class ZWController : ControllerBase
    {
        private readonly IZWSourceFacade _facade;
        private readonly IRequestValidator<WPMRequest> _validator;
        private readonly IZWSourceService _zwSourceService;

        public ZWController(IZWSourceFacade facade, IRequestValidator<WPMRequest> validator, IZWSourceService zwSourceService)
        {
            _facade = facade;
            _validator = validator;
            _zwSourceService = zwSourceService;
        }

        [HttpGet("wpm/szukaj")]
        [Produces(typeof(ProxyResponse<IEnumerable<WPMResponse>>))]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<WPMResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<WPMResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<WPMResponse>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<WPMResponse>>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "WPM – wyszukiwanie pojazdów",
            Description = "Najpierw liczy potencjalne rekordy, a dopiero gdy nie przekraczają progu – zwraca wynik."
        )]
        public async Task<ProxyResponse<IEnumerable<WPMResponse>>> SearchWPMAsync(
            [FromQuery] string? nrRejestracyjny,
            [FromQuery] string? numerPodwozia,
            [FromQuery] string? nrSerProducenta,
            [FromQuery] string? nrSerSilnika,
            CancellationToken ct = default)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ZW";
            const int vehiclesLimit = 20; // TODO: w kolejnym kroku przenieść do appsettings

            try
            {
                static string? Norm(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

                var req = new WPMRequest
                {
                    NrRejestracyjny = Norm(nrRejestracyjny),
                    NumerPodwozia = Norm(numerPodwozia),
                    NrSerProducenta = Norm(nrSerProducenta),
                    NrSerSilnika = Norm(nrSerSilnika)
                };

                // Walidacja wejścia
                var vr = _validator.ValidateAndNormalize(req);
                if (!vr.IsValid)
                {
                    return ProxyResponseFactory.BusinessError<IEnumerable<WPMResponse>>(
                        message: vr.MessageError!,
                        source: source,
                        sourceStatusCode: StatusCodes.Status400BadRequest.ToString(),
                        requestId: requestId);
                }

                // 1) pre-count
                var count = await _facade.CountVehiclesAsync(req, ct);

                if (count == 0)
                {
                    return ProxyResponseFactory.BusinessError<IEnumerable<WPMResponse>>(
                        message: "Nie znaleziono pojazdów spełniających zadane kryteria.",
                        source: source,
                        sourceStatusCode: StatusCodes.Status404NotFound.ToString(),
                        requestId: requestId);
                }

                if (count > vehiclesLimit)
                {
                    return ProxyResponseFactory.BusinessError<IEnumerable<WPMResponse>>(
                        message: $"Znaleziono więcej niż {vehiclesLimit} pojazdów. Popraw kryteria.",
                        source: source,
                        sourceStatusCode: StatusCodes.Status400BadRequest.ToString(),
                        requestId: requestId);
                }

                // 2) pobierz dane (count w granicach limitu)
                var rows = (await _facade.SearchAsync(req, ct))?.ToList() ?? new List<WPMResponse>();
                if (rows.Count == 0)
                {
                    // Nie powinno się zdarzyć po pre-count, ale zachowujemy bezpieczną ścieżkę
                    return ProxyResponseFactory.BusinessError<IEnumerable<WPMResponse>>(
                        message: "Nie znaleziono pojazdów spełniających zadane kryteria.",
                        source: source,
                        sourceStatusCode: StatusCodes.Status404NotFound.ToString(),
                        requestId: requestId);
                }

                // Sukces z komunikatem "Znaleziono {count} pojazdów."
                var successMessage = $"Znalezione pojazdy: {count}";
                return new ProxyResponse<IEnumerable<WPMResponse>>
                {
                    Data = rows,
                    Status = 0,
                    Message = successMessage,
                    Source = source,
                    SourceStatusCode = StatusCodes.Status200OK.ToString(),
                    RequestId = requestId
                };
            }
            catch (OperationCanceledException)
            {
                return ProxyResponseFactory.TechnicalError<IEnumerable<WPMResponse>>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponseFactory.TechnicalError<IEnumerable<WPMResponse>>(
                    $"Nieoczekiwany błąd: {ex.Message}", source, StatusCodes.Status500InternalServerError.ToString(), requestId);
            }
        }

        // === NOWE ===
        [HttpGet("poszukiwani/by-pesel")]
        [Produces(typeof(ProxyResponse<IEnumerable<WantedPersonResponse>>))]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<WantedPersonResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<WantedPersonResponse>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<WantedPersonResponse>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<WantedPersonResponse>>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "ZW – lista osób poszukiwanych wg PESEL",
            Description = "Zwraca listę wpisów z tabeli piesp.OsobyPoszukiwane dla podanego numeru PESEL (adapter ZW)."
        )]
        public async Task<ProxyResponse<IEnumerable<WantedPersonResponse>>> GetWantedPersonAsync(
            [FromQuery] string pesel,
            CancellationToken ct = default)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ZW";

            try
            {
                if (string.IsNullOrWhiteSpace(pesel))
                {
                    return ProxyResponseFactory.BusinessError<IEnumerable<WantedPersonResponse>>(
                        message: "PESEL jest wymagany.",
                        source: source,
                        sourceStatusCode: StatusCodes.Status400BadRequest.ToString(),
                        requestId: requestId);
                }

                var result = await _zwSourceService.GetWantedPersonsByPeselAsync(pesel.Trim(), ct);

                return result.Match(
                    onSuccess: list =>
                    {
                        var isEmpty = list is null || list.Count == 0;
                        return new ProxyResponse<IEnumerable<WantedPersonResponse>>
                        {
                            Data = list ?? Array.Empty<WantedPersonResponse>(),
                            Status = ProxyStatus.Success,
                            Message = isEmpty
                                ? $"Nie znaleziono osoby/osób poszukiwanych dla PESEL {pesel}."
                                : $"Znaleziono {list!.Count} rekord(ów).",
                            Source = source,
                            SourceStatusCode = StatusCodes.Status200OK.ToString(),
                            RequestId = requestId
                        };
                    },
                    onError: err =>
                    {
                        var code = (err.HttpStatus ?? StatusCodes.Status500InternalServerError).ToString();
                        // Traktujemy 4xx jako błąd biznesowy, resztę jako techniczny
                        if (err.HttpStatus is >= 400 and < 500)
                        {
                            return ProxyResponseFactory.BusinessError<IEnumerable<WantedPersonResponse>>(
                                message: err.Message,
                                source: source,
                                sourceStatusCode: code,
                                requestId: requestId);
                        }

                        return ProxyResponseFactory.TechnicalError<IEnumerable<WantedPersonResponse>>(
                            message: err.Message,
                            source: source,
                            sourceStatusCode: code,
                            requestId: requestId);
                    });
            }
            catch (OperationCanceledException)
            {
                return ProxyResponseFactory.TechnicalError<IEnumerable<WantedPersonResponse>>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponseFactory.TechnicalError<IEnumerable<WantedPersonResponse>>(
                    $"Nieoczekiwany błąd: {ex.Message}", source, StatusCodes.Status500InternalServerError.ToString(), requestId);
            }
        }

        [HttpGet("poszukiwani/check")]
        [Produces(typeof(ProxyResponse<bool>))]
        [ProducesResponseType(typeof(ProxyResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<bool>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
    Summary = "ZW – sprawdzenie, czy PESEL widnieje jako poszukiwany",
    Description = "Zwraca true, jeśli istnieje co najmniej jeden wpis w piesp.OsobyPoszukiwane dla podanego PESEL; w przeciwnym razie false."
)]
        public async Task<ProxyResponse<bool>> CheckIfWantedPerson(
    [FromQuery] string pesel,
    CancellationToken ct = default)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ZW";

            try
            {
                if (string.IsNullOrWhiteSpace(pesel))
                {
                    return ProxyResponseFactory.BusinessError<bool>(
                        message: "PESEL jest wymagany.",
                        source: source,
                        sourceStatusCode: StatusCodes.Status400BadRequest.ToString(),
                        requestId: requestId);
                }

                var result = await _zwSourceService.GetWantedPersonsByPeselAsync(pesel.Trim(), ct);

                return result.Match(
                    onSuccess: list =>
                    {
                        var isWanted = list is not null && list.Count > 0;

                        return new ProxyResponse<bool>
                        {
                            Data = isWanted,
                            Status = 0,
                            Message = isWanted
                                ? $"Osoba o PESEL {pesel} figuruje jako poszukiwana."
                                : $"Nie znaleziono osoby o PESEL {pesel} w rejestrze poszukiwanych.",
                            Source = source,
                            SourceStatusCode = StatusCodes.Status200OK.ToString(),
                            RequestId = requestId
                        };
                    },
                    onError: err =>
                    {
                        var code = (err.HttpStatus ?? StatusCodes.Status500InternalServerError).ToString();

                        if (err.HttpStatus is >= 400 and < 500)
                        {
                            return ProxyResponseFactory.BusinessError<bool>(
                                message: err.Message,
                                source: source,
                                sourceStatusCode: code,
                                requestId: requestId);
                        }

                        return ProxyResponseFactory.TechnicalError<bool>(
                            message: err.Message,
                            source: source,
                            sourceStatusCode: code,
                            requestId: requestId);
                    });
            }
            catch (OperationCanceledException)
            {
                return ProxyResponseFactory.TechnicalError<bool>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponseFactory.TechnicalError<bool>(
                    $"Nieoczekiwany błąd: {ex.Message}", source, StatusCodes.Status500InternalServerError.ToString(), requestId);
            }
        }


        [HttpGet("bron-osoba/by-pesel")]
        [Produces(typeof(ProxyResponse<PrivateWeaponHolderResponse>))]
        [ProducesResponseType(typeof(ProxyResponse<PrivateWeaponHolderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<PrivateWeaponHolderResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<PrivateWeaponHolderResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProxyResponse<PrivateWeaponHolderResponse>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
     Summary = "ZW – dane osoby posiadającej broń prywatną",
     Description = @"
<p>Usługa zwraca dane osoby posiadającej broń prywatną na podstawie rejestrów ZW:</p>
<ul>
  <li>odczytuje wpis osoby z tabeli <code>piesp.BronOsoby</code> na podstawie numeru PESEL,</li>
  <li>zwraca listę miejsc przechowywania broni z tabeli <code>piesp.BronAdresy</code>,</li>
  <li>w przypadku braku danych zwraca komunikat biznesowy o braku informacji.</li>
</ul>
<p>W środowisku testowym dostępne są dane dla numerów PESEL:</p>
<ul>
  <li>82121017239</li>
  <li>85031806944</li>
</ul>"
 )]
        public async Task<ProxyResponse<PrivateWeaponHolderResponse>> GetPrivateWeaponHolderAsync(
                [FromQuery] string pesel,
                CancellationToken ct = default)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ZW";

            try
            {

                if (string.IsNullOrWhiteSpace(pesel))
                {
                    return ProxyResponseFactory.BusinessError<PrivateWeaponHolderResponse>(
                        message: "PESEL jest wymagany.",
                        source: source,
                        sourceStatusCode: StatusCodes.Status400BadRequest.ToString(),
                        requestId: requestId);
                }

                var request = new PrivateWeaponHolderRequest { Pesel = pesel.Trim() };

                // Wywołanie serwisu ZW, który zwróci wynik w postaci Result<IReadOnlyList<PrivateWeaponHolderResponse>, Error>.
                // - Sukces  => lista osób z bronią,
                // - Błąd    => obiekt Error z informacją o problemie biznesowym lub technicznym (np. nie znaleziono osoby, błąd SQL, walidacja itp).
                var result = await _zwSourceService.GetWeaponHolderByPeselAsync(request, ct);

                // Match rozgałęzia logikę w zależności od tego, czy Result jest sukcesem czy błędem.
                // Zawsze na końcu musi powstać ProxyResponse<PrivateWeaponHolderResponse>.
                return result.Match(
                    onSuccess: list =>
                    {
                        var isEmpty = list is null || list.Count == 0;
                        return new ProxyResponse<PrivateWeaponHolderResponse>
                        {
                            Data = isEmpty ? null : list![0],
                            Status = ProxyStatus.Success,
                            Message = isEmpty
                                ? $"Nie znaleziono informacji o broni prywatnej dla PESEL {pesel}."
                                : $"Znaleziono dane o broni prywatnej dla PESEL {pesel}.",
                            Source = source,
                            SourceStatusCode = StatusCodes.Status200OK.ToString(),
                            RequestId = requestId
                        };
                    },
                    onError: err =>
                    {
                        //Jeśli nie znaleziono osoby lub wystąpił inny błąd biznesowy:
                        if (err.ErrorKind == ErrorKindEnum.Business)
                        {
                            return ProxyResponseFactory.BusinessError<PrivateWeaponHolderResponse>(
                                message: err.Message,   // komunikat biznesowy z obiektu Error
                                source: source,
                                sourceStatusCode: err.Code,
                                requestId: requestId);
                        }
                       
                        return ProxyResponseFactory.TechnicalError<PrivateWeaponHolderResponse>(
                                message: err.Message,
                                source: source,
                                sourceStatusCode: err.Code,
                                requestId: requestId);
                    }
                );//result.Match
            }
            catch (OperationCanceledException)
            {
                return ProxyResponseFactory.TechnicalError<PrivateWeaponHolderResponse>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            // Ogólny catch na wszystkie inne nieoczkiwane wyjątki, które nie zostały obsłużone wyżej.
            // Tutaj jako SourceStatusCode ustawiamy "500" (Internal Server Error),
            // a w message przekazujemy komunikat wyjątku (bez stack trace).
            catch (Exception ex)
            {
                return ProxyResponseFactory.TechnicalError<PrivateWeaponHolderResponse>(
                    $"Nieoczekiwany błąd: {ex.Message}",
                    source,
                    StatusCodes.Status500InternalServerError.ToString(),
                    requestId);
            }
        }

        [HttpGet("bron-osoba/check")]
        [Produces(typeof(ProxyResponse<bool>))]
        [ProducesResponseType(typeof(ProxyResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<bool>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
    Summary = "ZW – sprawdzenie, czy osoba posiada broń prywatną",
    Description = @"
<p>Usługa sprawdza, czy w rejestrze ZW istnieje wpis o posiadaniu broni prywatnej przez osobę o podanym numerze PESEL.</p>
<ul>
  <li>zwraca <code>true</code>, jeśli istnieje wpis w tabeli <code>piesp.BronOsoby</code>,</li>
  <li>zwraca <code>false</code>, jeśli brak wpisu dla podanego PESEL,</li>
  <li>błędy biznesowe i techniczne zwracane są w strukturze <code>ProxyResponse</code> z kodem HTTP 200.</li>
</ul>
<p>W środowisku testowym dostępne są dane dla numerów PESEL:</p>
<ul>
  <li>82121017239</li>
  <li>85031806944</li>
</ul>"
)]
        public async Task<ProxyResponse<bool>> CheckHasPrivateWeaponAsync(
    [FromQuery] string pesel,
    CancellationToken ct = default)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ZW";

            try
            {
                if (string.IsNullOrWhiteSpace(pesel))
                {
                    return ProxyResponseFactory.BusinessError<bool>(
                        message: "PESEL jest wymagany.",
                        source: source,
                        sourceStatusCode: StatusCodes.Status400BadRequest.ToString(),
                        requestId: requestId);
                }

                var request = new PrivateWeaponHolderRequest { Pesel = pesel.Trim() };

                var result = await _zwSourceService.GetWeaponHolderByPeselAsync(request, ct);

                return result.Match(
                    onSuccess: list =>
                    {
                        var hasWeapon = list is not null && list.Count > 0;
                        return new ProxyResponse<bool>
                        {
                            Data = hasWeapon,
                            Status = ProxyStatus.Success,
                            Message = hasWeapon
                                ? $"Osoba o PESEL {pesel} posiada broń prywatną."
                                : $"Nie znaleziono informacji o broni prywatnej dla PESEL {pesel}.",
                            Source = source,
                            SourceStatusCode = StatusCodes.Status200OK.ToString(),
                            RequestId = requestId
                        };
                    },
                    onError: err =>
                    {
                        if (err.ErrorKind == ErrorKindEnum.Business)
                        {
                            return new ProxyResponse<bool>
                            {
                                Data = false,
                                Status = 0, //Sukces
                                Message = err.Message,
                                Source = source,
                                SourceStatusCode = StatusCodes.Status200OK.ToString(),
                                RequestId = requestId
                            };
                        }
                        
                        return ProxyResponseFactory.TechnicalError<bool>(
                                message: err.Message,
                                source: source,
                                sourceStatusCode: err.Code,
                                requestId: requestId);
                     }
                 );
            }
            catch (OperationCanceledException)
            {
                return ProxyResponseFactory.TechnicalError<bool>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponseFactory.TechnicalError<bool>(
                    $"Nieoczekiwany błąd: {ex.Message}", source, StatusCodes.Status500InternalServerError.ToString(), requestId);
            }
        }

        [HttpGet("osoba-zolnierz/by-pesel")]
        [Produces(typeof(ProxyResponse<SoldierResponse>))]
        [ProducesResponseType(typeof(ProxyResponse<SoldierResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<SoldierResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<SoldierResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProxyResponse<SoldierResponse>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
        Summary = "ZW – dane osoby żołnierza wg numeru PESEL",
        Description = @"
<p>Usługa zwraca dane osoby żołnierza z rejestru ZW na podstawie numeru PESEL.</p>
<ul>
  <li>odczytuje wpis z tabeli <code>piesp.OsobyZolnierze</code> na podstawie numeru PESEL,</li>
  <li>zwraca podstawowe informacje o żołnierzu (PESEL, stopień, jednostka, hash PESEL),</li>
  <li>w przypadku braku danych zwraca błąd biznesowy informujący o braku żołnierza dla podanego PESEL.</li>
</ul>
<p>W środowisku testowym dostępne są dane dla numerów PESEL:</p>
<ul>
  <li>82121017239</li>
  <li>85031806944</li>
</ul>"
    )]
        public async Task<ProxyResponse<SoldierResponse>> GetSoldierPersonAsync(
        [FromQuery] string pesel,
        CancellationToken ct = default)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ZW";

            try
            {
                if (string.IsNullOrWhiteSpace(pesel))
                {
                    return ProxyResponseFactory.BusinessError<SoldierResponse>(
                        message: "PESEL jest wymagany.",
                        source: source,
                        sourceStatusCode: StatusCodes.Status400BadRequest.ToString(),
                        requestId: requestId);
                }

                var request = new SoldierRequest { Pesel = pesel.Trim() };

                var result = await _zwSourceService.GetSoldierByPeselAsync(request, ct);

                return result.Match(
                    onSuccess: soldier =>
                    {
                        var found = soldier is not null;
                        return new ProxyResponse<SoldierResponse>
                        {
                            Data = soldier,
                            Status = ProxyStatus.Success,
                            Message = found
                                ? $"Znaleziono dane żołnierza dla PESEL {pesel}."
                                : "Nie znaleziono żołnierza dla podanego PESEL!",
                            Source = source,
                            SourceStatusCode = StatusCodes.Status200OK.ToString(),
                            RequestId = requestId
                        };
                    },
                    onError: err =>
                    {
                        if (err.ErrorKind == ErrorKindEnum.Business)
                        {
                            return ProxyResponseFactory.BusinessError<SoldierResponse>(
                                message: err.Message,
                                source: source,
                                sourceStatusCode: err.Code,
                                requestId: requestId);
                        }

                        // Pozostałe -> błąd techniczny
                        return ProxyResponseFactory.TechnicalError<SoldierResponse>(
                            message: err.Message,
                            source: source,
                            sourceStatusCode: err.Code,
                            requestId: requestId);
                    });
            }
            catch (OperationCanceledException)
            {
                return ProxyResponseFactory.TechnicalError<SoldierResponse>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponseFactory.TechnicalError<SoldierResponse>(
                    $"Nieoczekiwany błąd: {ex.Message}",
                    source,
                    StatusCodes.Status500InternalServerError.ToString(),
                    requestId);
            }
        }

        [HttpGet("osoba-zolnierz/check")]
        [Produces(typeof(ProxyResponse<bool>))]
        [ProducesResponseType(typeof(ProxyResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<bool>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
    Summary = "ZW – sprawdzenie, czy osoba jest żołnierzem",
    Description = @"
<p>Usługa sprawdza, czy osoba o podanym numerze PESEL widnieje w rejestrze żołnierzy ZW.</p>
<ul>
  <li>zwraca <code>true</code>, jeśli istnieje wpis w tabeli <code>piesp.OsobyZolnierze</code>,</li>
  <li>zwraca <code>false</code>, jeśli brak wpisu dla podanego PESEL (błąd biznesowy NotFound),</li>
  <li>błędy biznesowe i techniczne są mapowane na strukturę <code>ProxyResponse</code> z kodem HTTP 200.</li>
</ul>
<p>W środowisku testowym dostępne są dane dla numerów PESEL:</p>
<ul>
  <li>82121017239</li>
  <li>85031806944</li>
</ul>"
)]
        public async Task<ProxyResponse<bool>> CheckIfSoldierAsync(
    [FromQuery] string pesel,
    CancellationToken ct = default)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ZW";

            try
            {
                if (string.IsNullOrWhiteSpace(pesel))
                {
                    return ProxyResponseFactory.BusinessError<bool>(
                        message: "PESEL jest wymagany.",
                        source: source,
                        sourceStatusCode: StatusCodes.Status400BadRequest.ToString(),
                        requestId: requestId);
                }

                var request = new SoldierRequest { Pesel = pesel.Trim() };

                var result = await _zwSourceService.GetSoldierByPeselAsync(request, ct);

                return result.Match(
                    onSuccess: soldier =>
                    {
                        var isSoldier = soldier is not null;
                        return new ProxyResponse<bool>
                        {
                            Data = isSoldier,
                            Status = ProxyStatus.Success,
                            Message = isSoldier
                                ? $"Osoba o PESEL {pesel} jest żołnierzem. {soldier!.Stopien ?? ""}"
                                : "Nie znaleziono żołnierza dla podanego PESEL!",
                            Source = source,
                            SourceStatusCode = StatusCodes.Status200OK.ToString(),
                            RequestId = requestId
                        };
                    },
                    onError: err =>
                    {
                        if (err.ErrorKind == ErrorKindEnum.Business)
                        {
                            return new ProxyResponse<bool>
                            {
                                Data = false,
                                Status = 0,
                                Message = err.Message,
                                Source = source,
                                SourceStatusCode = StatusCodes.Status200OK.ToString(),
                                RequestId = requestId
                            };
                        }

                        // Błędy techniczne -> TechnicalError
                        return ProxyResponseFactory.TechnicalError<bool>(
                            message: err.Message,
                            source: source,
                            sourceStatusCode: err.Code,
                            requestId: requestId);
                    });
            }
            catch (OperationCanceledException)
            {
                return ProxyResponseFactory.TechnicalError<bool>(
                    "Żądanie zostało anulowane.", source, "499", requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponseFactory.TechnicalError<bool>(
                    $"Nieoczekiwany błąd: {ex.Message}",
                    source,
                    StatusCodes.Status500InternalServerError.ToString(),
                    requestId);
            }
        }

        [HttpPost("bron-osoba/by-address")]
        [Produces(typeof(ProxyResponse<PrivateWeaponHolderResponse>))]
        [ProducesResponseType(typeof(ProxyResponse<PrivateWeaponHolderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<PrivateWeaponHolderResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<PrivateWeaponHolderResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProxyResponse<PrivateWeaponHolderResponse>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
    Summary = "ZW – dane osoby posiadającej broń prywatną wg adresu",
    Description = @"
<p>Usługa zwraca dane osoby posiadającej broń prywatną na podstawie adresu przechowywania broni w rejestrze ZW.</p>
<ul>
  <li>wyszukuje wpis w tabeli <code>piesp.BronAdresy</code> na podstawie parametrów adresowych,</li>
  <li>wymagane minimalne kryteria wyszukiwania: miejscowość i numer domu,</li>
  <li>jeśli znaleziono więcej niż jedną lokalizację, zwracany jest błąd biznesowy z prośbą o doprecyzowanie kryteriów,</li>
  <li>jeśli nie znaleziono żadnej lokalizacji, zwracany jest błąd biznesowy typu NotFound,</li>
  <li>w środowisku testowym dostępna jest lokalizacja testowa: Miejscowość: <code>WARSZAW</code>, Ulica: <code>BRZOZOWA</code>, Numer domu: <code>12</code>.</li>
</ul>
<p>Parametry należy przekazać w formacie JSON w ciele żądania zgodnie z modelem <code>WeaponAddressSearchRequest</code>.</p>"
)]
        public async Task<ProxyResponse<PrivateWeaponHolderResponse>> GetPrivateWeaponHolderByAddressAsync(
    [FromBody] WeaponAddressSearchRequest body,
    CancellationToken ct = default)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string source = "ZW";

            try
            {
                if (body is null)
                {
                    return ProxyResponseFactory.BusinessError<PrivateWeaponHolderResponse>(
                        message: "Body żądania nie może być null.",
                        source: source,
                        sourceStatusCode: StatusCodes.Status400BadRequest.ToString(),
                        requestId: requestId);
                }

                var result = await _zwSourceService.GetWeaponHolderByAddressAsync(body, ct);
                var proxy = result.ToProxyResponse(source, requestId);
                if (result.IsSuccess)
                    proxy.Message = result.Value is null
                        ? "Nie znaleziono lokalizacji spełniającej zadane kryteria."
                        : "Znaleziono dane posiadacza broni prywatnej dla wskazanego adresu.";
                return proxy;
            }
            catch (OperationCanceledException)
            {
                return ProxyResponseFactory.TechnicalError<PrivateWeaponHolderResponse>(
                    message: "Żądanie zostało anulowane.",
                    source: source,
                    sourceStatusCode: "499",
                    requestId: requestId);
            }
            catch (Exception ex)
            {
                return ProxyResponseFactory.TechnicalError<PrivateWeaponHolderResponse>(
                    message: $"Nieoczekiwany błąd: {ex.Message}",
                    source: source,
                    sourceStatusCode: StatusCodes.Status500InternalServerError.ToString(),
                    requestId: requestId);
            }
        }





    }
}


