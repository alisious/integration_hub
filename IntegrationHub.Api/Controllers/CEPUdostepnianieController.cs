// IntegrationHub.Sources.CEP/Controllers/CEPUdostepnianieController.cs
using IntegrationHub.Common.Contracts;                       // ProxyResponse<T>, ProxyStatus
using IntegrationHub.Common.Primitives;                 // Error
using IntegrationHub.Common.RequestValidation;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;    // PytanieOPojazdRequest, PytanieOPojazdResponse
using IntegrationHub.Sources.CEP.Udostepnianie.Services;                   // ICEPUdostepnianieService
using IntegrationHub.Sources.CEP.UpKi.Contracts;
using IntegrationHub.Sources.CEP.UpKi.RequestValidation;
using IntegrationHub.Sources.CEP.UpKi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;                  // (opcjonalnie) ładniejszy opis w Swagger
//using IntegrationHub.Sources.CEP.UpKi.Contracts;        // DaneDokumentuRequestDto, DaneDokumentuResponseDto
//using IntegrationHub.Sources.CEP.UpKi.Services;


namespace IntegrationHub.Sources.CEP.Controllers
{
    [ApiController]
    [Route("CEP/udostepnianie")]
    [Produces("application/json")]
    [Authorize(Roles = "User")]
    public sealed class CEPUdostepnianieController : ControllerBase
    {
        private readonly ICEPUdostepnianieService _service;
        private readonly IUpKiService _upkiService;
        private readonly ILogger<CEPUdostepnianieController> _logger;
        

        public CEPUdostepnianieController(
            ICEPUdostepnianieService service,
            IUpKiService upkiService,
            ILogger<CEPUdostepnianieController> logger)
        {
            _service = service;
            _upkiService = upkiService;
            _logger = logger;
        }

        /// <summary>
        /// CEPIK – Pytanie o pojazd.
        /// Zwraca <see cref="ProxyResponse{T}"/> z danymi pojazdu.
        /// </summary>
        /// <remarks>
        /// <b>Reguły walidacji (minimalne kryteria wyszukiwania)</b><br/>
        /// Zapytanie jest akceptowane, gdy spełniony jest <u>co najmniej jeden</u> z warunków:
        /// <list type="bullet">
        /// <item><description><c>identyfikatorSystemowyPojazdu</c> – wyszukiwanie po identyfikatorze systemowym, albo</description></item>
        /// <item><description><c>numerRejestracyjny</c> – wyszukiwanie po aktualnym numerze rejestracyjnym, albo</description></item>
        /// <item><description><c>numerPodwoziaNadwoziaRamy</c> (VIN) – wyszukiwanie po VIN, albo</description></item>
        /// <item><description>
        /// para: <c>parametryDokumentuPojazdu.typDokumentu</c> <b>i</b> <c>parametryDokumentuPojazdu.dokumentSeriaNumer</c> – wyszukiwanie po typie i numerze dokumentu pojazdu.<br/>
        /// <b>Uwaga:</b> jeżeli nie podasz <c>typDokumentu</c>, to domyślnie zostanie ustawiony <c>DICT155_DR</c> (Dowód rejestracyjny).
        /// </description></item>
        /// </list>
        /// Dodatkowe pola:
        /// <list type="bullet">
        /// <item><description><c>dataPrezentacji</c> (opcjonalnie) – prezentuj dane na wskazany moment czasu.</description></item>
        /// <item><description><c>wyszukiwaniePoDanychHistorycznych</c> (bool, domyślnie <c>false</c>) – uwzględnij dane historyczne.</description></item>
        /// </list>
        /// Wszystkie pola są trimowane; kody słownikowe normalizowane do UPPER CASE.
        /// </remarks>
        [HttpPost("pytanie-o-pojazd")]
        [Consumes("application/json")]
        [Produces(typeof(ProxyResponse<PytanieOPojazdResponse>))]
        [ProducesResponseType(typeof(ProxyResponse<PytanieOPojazdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<PytanieOPojazdResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<PytanieOPojazdResponse>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
    Summary = "CEPIK – Pytanie o pojazd",
    Description =
    "<b>Minimalne kryteria zapytania</b><br/>" +
    "<ul>" +
      "<li><code>typDokumentu</code> <i>i</i> <code>dokumentSeriaNumer</code> <b>lub</b></li>" +
      "<li><code>numerRejestracyjny</code> <b>lub</b></li>" +
      "<li><code>numerRejestracyjnyZagraniczny</code> <b>lub</b></li>" +
      "<li><code>identyfikatorSystemowyPodmiotu</code> <b>lub</b></li>" +
      "<li><code>identyfikatorSystemowyPojazdu</code> <b>lub</b></li>" +
      "<li><code>identyfikatorCzynnosci</code> <i>i</i> <code>identyfikatorSystemowyPojazdu</code> <b>lub</b></li>" +
      "<li><code>numerPodwoziaNadwoziaRamy</code> (VIN) — <u>nie łączyć z innymi parametrami</u>.</li>" +
    "</ul>" +
    "<b>Zakres historii</b><br/>" +
    "W zależności od wartości <code>wyszukiwaniePoDanychHistorycznych</code>, identyfikacja jest prowadzona po całej historii (gdy <code>true</code>) lub tylko po stanie na moment wywołania (gdy <code>false</code>).<br/><br/>" +
    "<b>Semantyka wyniku</b><br/>" +
    "Wynikiem zapytania jest <i>pojazd</i> w stanie na:<br/>" +
    "<ul>" +
      "<li>przekazaną <code>dataPrezentacji</code>, lub</li>" +
      "<li>przekazany <code>identyfikatorCzynnosci</code>, lub</li>" +
      "<li>aktualny stan, jeśli nie podano <code>dataPrezentacji</code> ani <code>identyfikatorCzynnosci</code>.</li>" +
    "</ul>" +
    "<b>Uwaga: lista wyników</b><br/>" +
    "Metoda może zwrócić wiele pojazdów w polu <code>pojazdy</code>. Liczba zwróconych rekordów znajduje się w <code>meta.iloscZwroconychRekordow</code>. Obowiązuje globalny limit: maks. <b>1000</b> obiektów.<br/><br/>" +
    "<b>Dodatkowe reguły</b><br/>" +
    "<ul>" +
      "<li>Jeśli nie podasz <code>typDokumentu</code>, domyślnie przyjmowany jest <code>DICT155_DR</code> (Dowód rejestracyjny).</li>" +
      "<li>Wszystkie pola są trimowane; kody słownikowe normalizowane do UPPER CASE.</li>" +
    "</ul>" +
    "<b>Środowisko testowe:</b>" +
    "<ul>" +
      "Dostępne przykładowe dane:" +
      "<li><code>identyfikatorSystemowyPojazdu = 5302051569907661</code></li>" +
      "<li><code>numerRejestracyjny = LU638JU</code></li>" +
      "<li><code>dokumentSeriaNumer = BAS4381563</code></li>" +
      "<li><code>numerPodwoziaNadwoziaRamy = VF1JX0HSC81543224</code></li>" +
    "</ul>"
)]
        public async Task<ProxyResponse<PytanieOPojazdResponse>> PytanieOPojazd(
            [FromBody] PytanieOPojazdRequest body,
            CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                var result = await _service.PytanieOPojazdAsync(body, requestId, ct);

                if (result.Status != ProxyStatus.Success)
                {
                    _logger.LogWarning(
                        "CEPUdostepnianie.PytanieOPojazd: status={ProxyStatus}, http={Http}, reqId={ReqId}",
                        result.Status, result.SourceStatusCode, result.RequestId);
                }

                return result;
            }
            catch (Exception ex)
            {
                // Fallback na wypadek nieoczekiwanego wyjątku, który "uciekł" z warstwy serwisu
                _logger.LogError(ex, "Nieoczekiwany błąd w CEPUdostepnianieController.PytanieOPojazd. reqId={ReqId}", requestId);

                return new ProxyResponse<PytanieOPojazdResponse>
                {
                    RequestId = requestId,
                    Source = "CEP",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = "500",
                    Message = "Wystąpił nieoczekiwany błąd po stronie API."
                };
            }
        }

        /// <summary>
        /// CEPIK – Pytanie o pojazd (rozszerzone).
        /// Zwraca <see cref="ProxyResponse{T}"/> z rozszerzonym zestawem danych (m.in. SKP, homologacja, własność/podmiot).
        /// </summary>
        /// <remarks>
        /// <b>Reguły walidacji (minimalne kryteria wyszukiwania)</b><br/>
        /// Zapytanie jest akceptowane, gdy spełniony jest <u>co najmniej jeden</u> z warunków:
        /// <list type="bullet">
        /// <item><description><c>identyfikatorSystemowyPojazdu</c> – wyszukiwanie po identyfikatorze systemowym, albo</description></item>
        /// <item><description><c>numerRejestracyjny</c> – wyszukiwanie po aktualnym numerze rejestracyjnym, albo</description></item>
        /// <item><description><c>numerPodwoziaNadwoziaRamy</c> (VIN) – wyszukiwanie po VIN, albo</description></item>
        /// <item><description>
        /// para: <c>parametryDokumentuPojazdu.typDokumentu</c> <b>i</b> <c>parametryDokumentuPojazdu.dokumentSeriaNumer</c> – wyszukiwanie po typie i numerze dokumentu pojazdu.<br/>
        /// <b>Uwaga:</b> jeżeli nie podasz <c>typDokumentu</c>, to domyślnie zostanie ustawiony <c>DICT155_DR</c> (Dowód rejestracyjny).
        /// </description></item>
        /// </list>
        /// Dodatkowe pola:
        /// <list type="bullet">
        /// <item><description><c>dataPrezentacji</c> (opcjonalnie) – prezentuj dane na wskazany moment czasu.</description></item>
        /// <item><description><c>wyszukiwaniePoDanychHistorycznych</c> (bool, domyślnie <c>false</c>) – uwzględnij dane historyczne.</description></item>
        /// </list>
        /// Wszystkie pola są trimowane; kody słownikowe normalizowane do UPPER CASE.
        /// </remarks>
        [HttpPost("pytanie-o-pojazd-rozszerzone")]
        [Consumes("application/json")]
        [Produces(typeof(ProxyResponse<PytanieOPojazdRozszerzoneResponse>))]
        [ProducesResponseType(typeof(ProxyResponse<PytanieOPojazdRozszerzoneResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<PytanieOPojazdRozszerzoneResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<PytanieOPojazdRozszerzoneResponse>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
    Summary = "CEPIK – Pytanie o pojazd (rozszerzone)",
    Description =
    "<b>Minimalne kryteria zapytania</b><br/>" +
    "<ul>" +
      "<li><code>typDokumentu</code> <i>i</i> <code>dokumentSeriaNumer</code> <b>lub</b></li>" +
      "<li><code>numerRejestracyjny</code> <b>lub</b></li>" +
      "<li><code>numerRejestracyjnyZagraniczny</code> <b>lub</b></li>" +
      "<li><code>identyfikatorSystemowyPodmiotu</code> <b>lub</b></li>" +
      "<li><code>identyfikatorSystemowyPojazdu</code> <b>lub</b></li>" +
      "<li><code>identyfikatorCzynnosci</code> <i>i</i> <code>identyfikatorSystemowyPojazdu</code> <b>lub</b></li>" +
      "<li><code>numerPodwoziaNadwoziaRamy</code> (VIN) — <u>nie łączyć z innymi parametrami</u>.</li>" +
    "</ul>" +
    "<b>Zakres historii</b><br/>" +
    "Parametr <code>wyszukiwaniePoDanychHistorycznych</code> określa, czy identyfikacja odbywa się po całej historii (<code>true</code>), czy tylko po stanie bieżącym (<code>false</code>).<br/><br/>" +
    "<b>Semantyka wyniku</b><br/>" +
    "Wynikiem zapytania jest <i>pojazd</i> w stanie na:<br/>" +
    "<ul>" +
      "<li>przekazaną <code>dataPrezentacji</code>, lub</li>" +
      "<li>przekazany <code>identyfikatorCzynnosci</code>, lub</li>" +
      "<li>aktualny stan, jeśli nie podano <code>dataPrezentacji</code> ani <code>identyfikatorCzynnosci</code>.</li>" +
    "</ul>" +
    "W wyniku zapytania metoda zwraca maksymalnie 1 obiekt (globalny limit).<br/>" +
    "<b>Dodatkowe reguły</b><br/>" +
    "<ul>" +
      "<li>Jeśli nie podasz <code>typDokumentu</code>, domyślnie przyjmowany jest <code>DICT155_DR</code> (Dowód rejestracyjny).</li>" +
      "<li>Wszystkie pola są trimowane; kody słownikowe normalizowane do UPPER CASE.</li>" +
    "</ul>" +
    "<b>Środowisko testowe:</b>" +
    "<ul>" +
      "Dostępne przykładowe dane:" +
      "<li><code>identyfikatorSystemowyPojazdu = 5302051569907661</code></li>" +
      "<li><code>numerRejestracyjny = LU638JU</code></li>" +
      "<li><code>dokumentSeriaNumer = BAS4381563</code></li>" +
      "<li><code>numerPodwoziaNadwoziaRamy = VF1JX0HSC81543224</code></li>" +
    "</ul>"
)]
        public async Task<ProxyResponse<PytanieOPojazdRozszerzoneResponse>> PytanieOPojazdRozszerzone(
            [FromBody] PytanieOPojazdRequest body,
            CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                var result = await _service.PytanieOPojazdRozszerzoneAsync(body, requestId, ct);

                if (result.Status != ProxyStatus.Success)
                {
                    _logger.LogWarning(
                        "CEPUdostepnianie.PytanieOPojazdRozszerzone: status={ProxyStatus}, http={Http}, reqId={ReqId}",
                        result.Status, result.SourceStatusCode, result.RequestId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd w CEPUdostepnianieController.PytanieOPojazdRozszerzone. reqId={ReqId}", requestId);

                return new ProxyResponse<PytanieOPojazdRozszerzoneResponse>
                {
                    RequestId = requestId,
                    Source = "CEP",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = "500",
                    Message = "Wystąpił nieoczekiwany błąd po stronie API."
                };
            }
        }

        /// <summary>
        /// CEPIK – Pytanie o dokument pojazdu.
        /// Zwraca <see cref="ProxyResponse{T}"/> z danymi dokumentu (np. DR) oraz wybranymi informacjami o pojeździe.
        /// </summary>
        /// <remarks>
        /// <b>Reguły walidacji (minimalne kryteria wyszukiwania)</b><br/>
        /// Zapytanie jest akceptowane, gdy spełniony jest <u>co najmniej jeden</u> z warunków:
        /// <list type="bullet">
        /// <item><description><c>identyfikatorSystemowyDokumentuPojazdu</c>, albo</description></item>
        /// <item><description>para: <c>typDokumentu</c> <b>i</b> <c>dokumentSeriaNumer</c>.</description></item>
        /// </list>
        /// Dodatkowe reguły:
        /// <list type="bullet">
        /// <item><description><b>Uwaga:</b> jeżeli nie podasz <c>typDokumentu</c>, to domyślnie zostanie ustawiony <c>DICT155_DR</c> (Dowód rejestracyjny).</description></item>
        /// <item><description>Wszystkie pola są trimowane; kody słownikowe normalizowane do UPPER CASE.</description></item>
        /// </list>
        /// </remarks>
        [HttpPost("pytanie-o-dokument-pojazdu")]
        [Consumes("application/json")]
        [Produces(typeof(ProxyResponse<PytanieODokumentPojazduResponse>))]
        [ProducesResponseType(typeof(ProxyResponse<PytanieODokumentPojazduResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<PytanieODokumentPojazduResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<PytanieODokumentPojazduResponse>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "CEPIK – Pytanie o dokument pojazdu",
            Description =
            "<b>Reguły walidacji (minimalne kryteria wyszukiwania)</b><br/>" +
            "Zapytanie jest akceptowane, gdy spełniony jest <u>co najmniej jeden</u> z warunków:" +
            "<ul>" +
            "<li><code>identyfikatorSystemowyDokumentuPojazdu</code>, albo</li>" +
            "<li>para: <code>typDokumentu</code> i <code>dokumentSeriaNumer</code>.</li>" +
            "</ul>" +
            "<b>Dodatkowe reguły:</b>" +
            "<ul>" +
            "<li><b>Uwaga:</b> jeżeli nie podasz <code>typDokumentu</code>, to domyślnie zostanie ustawiony <code>DICT155_DR</code> (Dowód rejestracyjny).</li>" +
            "<li>Wszystkie pola są trimowane; kody słownikowe normalizowane do UPPER CASE.</li>" +
            "</ul>"+
            "<b>Środowisko testowe:</b>" +
            "<ul>"+
            "W środowisku testowym znają się dane dokumentu pojazdu, dla którego:" +
            "<li>identyfikatorSystemowyDokumentuPojazdu=3391181117602887</li>" +
            "albo"+
            "<li>dokumentSeriaNumer = BAV1144112</li>" +
            "</ul>"
        )]
        public async Task<ProxyResponse<PytanieODokumentPojazduResponse>> PytanieODokumentPojazdu(
            [FromBody] PytanieODokumentPojazduRequest body,
            CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                var result = await _service.PytanieODokumentPojazduAsync(body, requestId, ct);
                if (result.Status != ProxyStatus.Success)
                {
                    _logger.LogWarning(
                        "CEPUdostepnianie.PytanieODokumentPojazdu: status={ProxyStatus}, http={Http}, reqId={ReqId}",
                        result.Status, result.SourceStatusCode, result.RequestId);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd w CEPUdostepnianieController.PytanieODokumentPojazdu. reqId={ReqId}", requestId);
                return new ProxyResponse<PytanieODokumentPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP.Udostepnianie.Controller",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = "500",
                    Message = "Wystąpił nieoczekiwany błąd po stronie API."
                };
            }

        }

        // using ... Contracts; Services; Swagger;
        [HttpPost("pytanie-o-liste-czynnosci-pojazdu")]
        [Consumes("application/json")]
        [Produces(typeof(ProxyResponse<PytanieOListeCzynnosciPojazduResponse>))]
        [ProducesResponseType(typeof(ProxyResponse<PytanieOListeCzynnosciPojazduResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<PytanieOListeCzynnosciPojazduResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<PytanieOListeCzynnosciPojazduResponse>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "CEPIK – Pytanie o listę czynności pojazdu",
            Description =
            "<b>Reguły walidacji (minimalne kryteria wyszukiwania)</b><br/>" +
            "Zapytanie jest akceptowane, gdy podasz:" +
            "<ul>" +
            "<li><code>identyfikatorSystemowyPojazdu</code></li>" +
            "</ul>" +
            "<ul><li>Wszystkie pola są trimowane.</li></ul>" +
            "<b>Środowisko testowe:</b>" +
            "<ul>" +
            "W środowisku testowym znajdują się dane czynności dla pojazdu:" +
            "<li>identyfikatorSystemowyPojazdu = 6206473948761901</li>" +
            "</ul>"
        )]
        public async Task<ProxyResponse<PytanieOListeCzynnosciPojazduResponse>> PytanieOListeCzynnosciPojazdu(
            [FromBody] PytanieOListeCzynnosciPojazduRequest body,
            CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                var result = await _service.PytanieOListeCzynnosciPojazduAsync(body, requestId, ct);
                if (result.Status != ProxyStatus.Success)
                {
                    _logger.LogWarning("CEPUdostepnianie.PytanieOListeCzynnosciPojazdu: status={Status}, http={Http}, reqId={ReqId}",
                        result.Status, result.SourceStatusCode, result.RequestId);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd w CEPUdostepnianieController.PytanieOListeCzynnosciPojazdu. reqId={ReqId}", requestId);
                return new ProxyResponse<PytanieOListeCzynnosciPojazduResponse>
                {
                    RequestId = requestId,
                    Source = "CEP",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = "500",
                    Message = "Wystąpił nieoczekiwany błąd po stronie API."
                };
            }
        }

        /// <summary>
        /// CEPIK – Pytanie o historię licznika.
        /// Zwraca <see cref="ProxyResponse{T}"/> z listą historycznych odczytów licznika.
        /// </summary>
        /// <remarks>
        /// <b>Reguły walidacji</b><br/>
        /// Zapytanie jest akceptowane, gdy podasz: <code>identyfikatorSystemowyPojazdu</code>.
        /// Wszystkie pola są trimowane.
        /// </remarks>
        [HttpPost("pytanie-o-historie-licznika")]
        [Consumes("application/json")]
        [Produces(typeof(ProxyResponse<PytanieOHistorieLicznikaResponse>))]
        [ProducesResponseType(typeof(ProxyResponse<PytanieOHistorieLicznikaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<PytanieOHistorieLicznikaResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<PytanieOHistorieLicznikaResponse>), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
            Summary = "CEPIK – Pytanie o historię licznika",
            Description =
                "<b>Reguły walidacji</b><br/>" +
                "Wymagane jest podanie: <code>identyfikatorSystemowyPojazdu</code>." +
                "<ul><li>Wszystkie pola są trimowane.</li></ul>" +
                "<b>Środowisko testowe:</b>" +
                "<ul>" +
                "W środowisku testowym znajduje się historia licznika dla:" +
                "<li>identyfikatorSystemowyPojazdu = 6206473948761901</li>" +
                "</ul>"
        )]
        public async Task<ProxyResponse<PytanieOHistorieLicznikaResponse>> PytanieOHistorieLicznika(
            [FromBody] PytanieOHistorieLicznikaRequest body,
            CancellationToken ct)
        {
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                var result = await _service.PytanieOHistorieLicznikaAsync(body, requestId, ct);

                if (result.Status != ProxyStatus.Success)
                {
                    _logger.LogWarning(
                        "CEPUdostepnianie.PytanieOHistorieLicznika: status={ProxyStatus}, http={Http}, reqId={ReqId}",
                        result.Status, result.SourceStatusCode, result.RequestId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd w CEPUdostepnianieController.PytanieOHistorieLicznika. reqId={ReqId}", requestId);

                return new ProxyResponse<PytanieOHistorieLicznikaResponse>
                {
                    RequestId = requestId,
                    Source = "CEP",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = "500",
                    Message = "Wystąpił nieoczekiwany błąd po stronie API."
                };
            }
        }

        
        /// <summary>
        /// CEPIK – Pytanie o podmiot.
        /// Zwraca <see cref="ProxyResponse{T}"/> z danymi podmiotu (osoba lub firma).
        /// </summary>
        /// <remarks>
        /// <b>Minimalne kryteria zapytania</b><br/>
        /// <u>W przypadku identyfikacji firmy:</u><br/>
        /// - <c>identyfikatorSystemowyPodmiotu</c> lub<br/>
        /// - <c>REGON</c> lub<br/>
        /// - <c>nazwaFirmyDrukowana</c> lub<br/>
        /// - <c>identyfikatorSystemowyREGON</c> lub<br/>
        /// - <c>zagranicznyNumerIdentyfikacyjny</c> lub<br/>
        /// - <c>nazwaWojewodztwaStanu</c>, <c>nazwaMiejscowosci</c>, <c>kodPocztowy</c> i <c>numerDomu</c> lub<br/>
        /// - <c>nazwaWojewodztwaStanu</c>, <c>nazwaMiejscowosci</c>, <c>nazwaUlicy</c> i <c>numerDomu</c><br/><br/>
        /// <u>W przypadku identyfikacji osoby:</u><br/>
        /// - <c>identyfikatorSystemowyPodmiotu</c> lub<br/>
        /// - <c>imiePierwsze</c>, <c>miejsceUrodzenia</c> i <c>nazwisko</c> lub<br/>
        /// - <c>imiePierwsze</c>, <c>miejsceUrodzeniaKod</c> i <c>nazwisko</c> lub<br/>
        /// - <c>PESEL</c> lub<br/>
        /// - <c>nazwaDokumentu</c> i <c>seriaNumerDokumentu</c>.<br/>
        /// Wszystkie pola są trimowane.
        /// <br/><br/><b>Środowisko testowe:</b> są dostępne dane dla:
        /// <c>identyfikatorSystemowyPodmiotu = 46801643589320</c> (osoba) oraz
        /// <c>identyfikatorSystemowyPodmiotu = 7883995899331519</c> (firma).
        /// </remarks>
        [SwaggerOperation(
            Summary = "CEPIK – Pytanie o podmiot",
            Description =
                "<b>Minimalne kryteria zapytania</b><br/>" +
                "<u>W przypadku identyfikacji firmy:</u><br/>" +
                "- <code>identyfikatorSystemowyPodmiotu</code> lub<br/>" +
                "- <code>REGON</code> lub<br/>" +
                "- <code>nazwaFirmyDrukowana</code> lub<br/>" +
                "- <code>identyfikatorSystemowyREGON</code> lub<br/>" +
                "- <code>zagranicznyNumerIdentyfikacyjny</code> lub<br/>" +
                "- <code>nazwaWojewodztwaStanu</code>, <code>nazwaMiejscowosci</code>, <code>kodPocztowy</code> i <code>numerDomu</code> lub<br/>" +
                "- <code>nazwaWojewodztwaStanu</code>, <code>nazwaMiejscowosci</code>, <code>nazwaUlicy</code> i <code>numerDomu</code><br/><br/>" +
                "<u>W przypadku identyfikacji osoby:</u><br/>" +
                "- <code>identyfikatorSystemowyPodmiotu</code> lub<br/>" +
                "- <code>imiePierwsze</code>, <code>miejsceUrodzenia</code> i <code>nazwisko</code> lub<br/>" +
                "- <code>imiePierwsze</code>, <code>miejsceUrodzeniaKod</code> i <code>nazwisko</code> lub<br/>" +
                "- <code>PESEL</code> lub<br/>" +
                "- <code>nazwaDokumentu</code> i <code>seriaNumerDokumentu</code>.<br/>" +
                "<ul><li>Wszystkie pola są trimowane.</li></ul>" +
                "<b>Środowisko testowe:</b> " +
                "dostępne rekordy dla: " +
                "<code>identyfikatorSystemowyPodmiotu = 46801643589320</code> (osoba) oraz " +
                "<code>identyfikatorSystemowyPodmiotu = 7883995899331519</code> (firma)."
        )]
        [HttpPost("pytanie-o-podmiot")]
        [Consumes("application/json")]
        [Produces(typeof(ProxyResponse<PytanieOPodmiotResponse>))]
        [ProducesResponseType(typeof(ProxyResponse<PytanieOPodmiotResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<PytanieOPodmiotResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProxyResponse<PytanieOPodmiotResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<ProxyResponse<PytanieOPodmiotResponse>> PytanieOPodmiot(
            [FromBody] PytanieOPodmiotRequest body,
            CancellationToken ct)
        {
            var sourceName = "CEP";
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                var result = await _service.PytanieOPodmiotAsync(body, requestId, ct);

                if (result.Status != ProxyStatus.Success)
                {
                    _logger.LogWarning(
                        "CEPUdostepnianie.PytanieOPodmiot: status={ProxyStatus}, http={Http}, reqId={ReqId}",
                        result.Status, result.SourceStatusCode, result.RequestId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd w CEPUdostepnianieController.PytanieOPodmiot. reqId={ReqId}", requestId);
                return new ProxyResponse<PytanieOPodmiotResponse>
                {
                    RequestId = requestId,
                    Source = sourceName,
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = "500",
                    Message = "Wystąpił nieoczekiwany błąd po stronie API."
                };
            }
        }

        /// <summary>
        /// Sprawdzenie w CEK uprawnień kierowcy (PJ/PT), ważności dokumentu oraz zakazów/cofnięć.
        /// </summary>
        [HttpPost("uprawnienia-kierowcy")]
        [SwaggerOperation(
    Summary = "Sprawdzenie uprawnień kierowcy w CEK (PJ/PT)",
    Description = @"
Usługa umożliwia sprawdzenie w Centralnej Ewidencji Kierowców informacji o posiadanych przez daną osobę uprawnieniach do kierowania pojazdem (PJ) lub tramwajem (PT), potwierdzenie ważności dokumentu prawa jazdy oraz daty jego wydania.

Wyszukiwanie może być wykonane:
<ul>
  <li>na datę aktualną (gdy pole <b>dataZapytania</b> nie jest podane), lub</li>
  <li>na wskazaną datę podaną w polu <b>dataZapytania</b> (format RRRR-MM-DD).</li>
</ul>

Minimalna kombinacja pól potrzebnych do wyszukania (wystarczy jedno z poniższych kryteriów):
<ul>
  <li><b>numerPesel</b>, opcjonalnie <b>dataZapytania</b></li>
  <li><b>numerDokumentu</b>, opcjonalnie <b>dataZapytania</b></li>
  <li><b>seriaNumerDokumentu</b>, opcjonalnie <b>dataZapytania</b></li>
  <li><b>daneOsoby</b>:
      <ul>
        <li><b>imiePierwsze</b></li>
        <li><b>nazwisko</b></li>
        <li><b>dataUrodzenia</b></li>
      </ul>
      opcjonalnie <b>dataZapytania</b>
  </li>
  <li><b>osobaId</b>, opcjonalnie <b>dataZapytania</b></li>
  <li><b>idk</b>, opcjonalnie <b>dataZapytania</b></li>
</ul>
dataZapytania i dataUrodzenia w formacie <b>'RRRR-MM-DD' (yyyy-MM-dd)</b>.

W odpowiedzi zwracane są:
<ul>
  <li>lista dokumentów i ich aktualny stan,</li>
  <li>uprawnienia kierowcy (PJ/PT),</li>
  <li>zakazy i cofnięcia dotyczące dokumentów oraz uprawnień,</li>
  <li>komunikaty biznesowe aktualne dla danego zapytania.</li>
</ul>

Komunikaty błędów:
<ul>
  <li><b>UPKI-1000</b> – problem z komunikacją z serwerem</li>
  <li><b>UPKI-1010</b> – błąd walidacji</li>
  <li><b>UPKI-1020</b> – błąd słowników</li>
  <li><b>UPKI-1030</b> – brak danych</li>
</ul>

Dla środowiska testowego dostępne są dane przykładowe dla <b>numerPesel = 73020916558</b>.
")]


        [ProducesResponseType(typeof(ProxyResponse<DaneDokumentuResponse>), StatusCodes.Status200OK)]
        public async Task<ProxyResponse<DaneDokumentuResponse>> PytanieOUprawnieniaKierowcy(
            [FromBody] DaneDokumentuRequest body,
            CancellationToken ct = default)
        {
            var requestId = Guid.NewGuid().ToString("N");
            const string sourceName = "CEPiK";

            try
            {
                if (body is null)
                {
                    return ProxyResponses.BusinessError<DaneDokumentuResponse>(
                        "Body (DaneDokumentuRequest) nie może być null.",
                        sourceName,
                        StatusCodes.Status400BadRequest.ToString(),
                        requestId);
                }

                // 1) Walidacja + normalizacja (trim, format dat)
                var validator = new DaneDokumentuRequestValidator();
                var vr = validator.ValidateAndNormalize(body);
                if (!vr.IsValid)
                {
                    // Błąd walidacji – mapujemy przez ValidationResultExtensions
                    var baseResp = vr.ToProxyResponse(sourceName, requestId);
                    return new ProxyResponse<DaneDokumentuResponse>
                    {
                        RequestId = requestId,
                        Source = sourceName,
                        Status = baseResp.Status,
                        SourceStatusCode = baseResp.SourceStatusCode,
                        Message = baseResp.Message
                    };
                }

                // 3) Wywołanie serwisu UpKi
                var result = await _upkiService.GetDriverPermissionsAsync(body, ct);

                return result.Match(
                    onSuccess: dto => new ProxyResponse<DaneDokumentuResponse>
                    {
                        Data = dto,
                        Status = ProxyStatus.Success,
                        Message = "OK",
                        Source = sourceName,
                        SourceStatusCode = StatusCodes.Status200OK.ToString(),
                        RequestId = requestId
                    },
                    onError: err =>
                    {
                        var code = (err.HttpStatus ?? StatusCodes.Status500InternalServerError).ToString();

                        if (err.HttpStatus is >= 400 and < 500)
                        {
                            // Błąd biznesowy (np. brak danych UPKI-1030)
                            _logger.LogWarning(
                                "CEPiK UpKi business error: {Code} {Msg} (reqId={ReqId})",
                                err.Code, err.Message, requestId);

                            return ProxyResponses.BusinessError<DaneDokumentuResponse>(
                                message: err.Message,
                                source: sourceName,
                                sourceStatusCode: code,
                                requestId: requestId);
                        }

                        // Błąd techniczny (komunikacja, wyjątek, UPKI-1000 itp.)
                        _logger.LogError(
                            "CEPiK UpKi technical error: {Code} {Msg} (reqId={ReqId})",
                            err.Code, err.Message, requestId);

                        return ProxyResponses.TechnicalError<DaneDokumentuResponse>(
                            message: err.Message,
                            source: sourceName,
                            sourceStatusCode: code,
                            requestId: requestId);
                    });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return ProxyResponses.TechnicalError<DaneDokumentuResponse>(
                    "Żądanie zostało anulowane.",
                    sourceName,
                    "499",
                    requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Nieoczekiwany błąd w CEPUdostepnianieController.PytanieOUprawnieniaKierowcy. reqId={ReqId}",
                    requestId);

                return ProxyResponses.TechnicalError<DaneDokumentuResponse>(
                    $"Nieoczekiwany błąd: {ex.Message}",
                    sourceName,
                    StatusCodes.Status500InternalServerError.ToString(),
                    requestId);
            }
        }



    }
}
