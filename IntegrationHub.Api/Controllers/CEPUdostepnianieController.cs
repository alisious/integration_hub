// IntegrationHub.Sources.CEP/Controllers/CEPUdostepnianieController.cs
using IntegrationHub.Common.Contracts;                       // ProxyResponse<T>, ProxyStatus
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;    // PytanieOPojazdRequest, PytanieOPojazdResponse
using IntegrationHub.Sources.CEP.Udostepnianie.Services;                   // ICEPUdostepnianieService
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;                  // (opcjonalnie) ładniejszy opis w Swagger


namespace IntegrationHub.Sources.CEP.Controllers
{
    [ApiController]
    [Route("CEP/udostepnianie")]
    [Produces("application/json")]
    public sealed class CEPUdostepnianieController : ControllerBase
    {
        private readonly ICEPUdostepnianieService _service;
        private readonly ILogger<CEPUdostepnianieController> _logger;

        public CEPUdostepnianieController(
            ICEPUdostepnianieService service,
            ILogger<CEPUdostepnianieController> logger)
        {
            _service = service;
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
            "<b>Reguły walidacji (minimalne kryteria wyszukiwania)</b><br/>" +
            "Zapytanie jest akceptowane, gdy spełniony jest <u>co najmniej jeden</u> z warunków:" +
            "<ul>" +
            "<li><code>identyfikatorSystemowyPojazdu</code>, albo</li>" +
            "<li><code>numerRejestracyjny</code>, albo</li>" +
            "<li><code>numerPodwoziaNadwoziaRamy</code> (VIN), albo</li>" +
            "<li>para: <code>parametryDokumentuPojazdu.typDokumentu</code> i <code>parametryDokumentuPojazdu.dokumentSeriaNumer</code>." +
            "<br/><b>Uwaga:</b> jeżeli nie podasz <code>typDokumentu</code>, to domyślnie zostanie ustawiony <code>DICT155_DR</code> (Dowód rejestracyjny).</li>" +
            "</ul>" +
            "<b>Dodatkowe pola:</b>" +
            "<ul>" +
            "<li><code>dataPrezentacji</code> (opcjonalnie) – prezentuj dane na wskazany moment czasu.</li>" +
            "<li><code>wyszukiwaniePoDanychHistorycznych</code> (bool, domyślnie <code>false</code>).</li>" +
            "</ul>" +
            "<ul>" +
             "<li>Wszystkie pola są trimowane; kody słownikowe normalizowane do UPPER CASE.</li>" +
            "</ul>" +
            "<b>Środowisko testowe:</b>" +
            "<ul>" +
            "W środowisku testowym znają się dane pojazdu, dla którego:" +
            "<li>identyfikatorSystemowyPojazdu = 5302051569907661</li>" +
            "albo" +
            "<li>numerRejestracyjny = LU638JU</li>" +
            "albo" +
            "<li>dokumentSeriaNumer = BAS4381563</li>" +
            "albo" +
            "<li>numerPodwoziaNadwoziaRamy = VF1JX0HSC81543224</li>" +
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
                    Source = "CEP.Udostepnianie.Controller",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = 500,
                    ErrorMessage = "Wystąpił nieoczekiwany błąd po stronie API."
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
            "<b>Reguły walidacji (minimalne kryteria wyszukiwania)</b><br/>" +
            "Zapytanie jest akceptowane, gdy spełniony jest <u>co najmniej jeden</u> z warunków:" +
            "<ul>" +
            "<li><code>identyfikatorSystemowyPojazdu</code>, albo</li>" +
            "<li><code>numerRejestracyjny</code>, albo</li>" +
            "<li><code>numerPodwoziaNadwoziaRamy</code> (VIN), albo</li>" +
            "<li>para: <code>parametryDokumentuPojazdu.typDokumentu</code> i <code>parametryDokumentuPojazdu.dokumentSeriaNumer</code>." +
            "<br/><b>Uwaga:</b> jeżeli nie podasz <code>typDokumentu</code>, to domyślnie zostanie ustawiony <code>DICT155_DR</code> (Dowód rejestracyjny).</li>" +
            "</ul>" +
            "<b>Dodatkowe pola:</b>" +
            "<ul>" +
            "<li><code>dataPrezentacji</code> (opcjonalnie) – prezentuj dane na wskazany moment czasu.</li>" +
            "<li><code>wyszukiwaniePoDanychHistorycznych</code> (bool, domyślnie <code>false</code>).</li>" +
            "</ul>" +
            "<ul>" +
             "<li>Wszystkie pola są trimowane; kody słownikowe normalizowane do UPPER CASE.</li>" +
            "</ul>" +
            "<b>Środowisko testowe:</b>" +
            "<ul>" +
            "W środowisku testowym znają się dane pojazdu, dla którego:" +
            "<li>identyfikatorSystemowyPojazdu = 5302051569907661</li>" +
            "albo" +
            "<li>numerRejestracyjny = LU638JU</li>" +
            "albo" +
            "<li>dokumentSeriaNumer = BAS4381563</li>" +
            "albo" +
            "<li>numerPodwoziaNadwoziaRamy = VF1JX0HSC81543224</li>" +
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
                    Source = "CEP.Udostepnianie.Controller",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = 500,
                    ErrorMessage = "Wystąpił nieoczekiwany błąd po stronie API."
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
                    SourceStatusCode = 500,
                    ErrorMessage = "Wystąpił nieoczekiwany błąd po stronie API."
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
                    Source = "CEP.Udostepnianie.Controller",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = 500,
                    ErrorMessage = "Wystąpił nieoczekiwany błąd po stronie API."
                };
            }
        }

    }
}
