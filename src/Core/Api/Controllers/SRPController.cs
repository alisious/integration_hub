using IntegrationHub.Api.Swagger.Examples.SRP;
using IntegrationHub.Common.Contracts;
using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.Net;


namespace IntegrationHub.Api.Controllers
{
    
    [ApiController]
    [Route("SRP")]
    [Authorize(Roles = "User")]
    //[ApiExplorerSettings(GroupName = "SRP")]
    public class SRPController : ControllerBase
    {

        private readonly ILogger<SRPController> _logger;
        private readonly IPeselService _peselService;
        private readonly IRdoService _rdoService;

        public SRPController(IPeselService peselService, IRdoService rdoService, ILogger<SRPController> logger)
        {
            _logger = logger;
            _peselService = peselService;
            _rdoService = rdoService;
        }


        [SwaggerOperation(
            Summary = "SRP – Udostępnij aktualne zdjęcie",
            Description =
        "<b>Reguły</b><br/>" +
        "Wymagane są oba pola: <code>Pesel</code> <b>i</b> <code>IdOsoby</code>." +
        "<ul>" +
        "<li><code>Pesel</code> – numer PESEL osoby.</li>" +
        "<li><code>IdOsoby</code> – identyfikator osoby w rejestrze PESEL.</li>" +
        "</ul>" +
        "<u>Jeśli któregokolwiek z pól brakuje, zapytanie zakończy się błędem 400 (BusinessError).</u>"
        )]
        [HttpPost("get-current-photo")]
        public async Task<ProxyResponse<GetCurrentPhotoResponse>> GetCurrentPhoto([FromBody] GetCurrentPhotoRequest body)
        {
            var requestId = Guid.NewGuid().ToString();
            var result = await _rdoService.GetCurrentPhotoAsync(body, requestId);
            return result.ToProxyResponse("SRP", requestId);
        }


        /// <summary>Udostępnij aktualny dowód osobisty po PESEL.</summary>
        [SwaggerOperation(
            Summary = "SRP – Udostępnij dane aktualnego dowodu po PESEL",
            Description =
         "<b>Parametry</b><br/>" +
         "<ul>" +
         "<li><code>Pesel</code> – numer PESEL osoby, dla której mają zostać udostępnione dane aktualnego dowodu.</li>" +
         "</ul>" +
         "<b>Środowisko testowe:</b>" +
         "<ul>" +
            "W środowisku testowym znają się dane dowodu osobistego osoby, której pesel = 11111111111" +
         "</ul>"
        )]
        [HttpPost("get-current-id")]
        [ProducesResponseType(typeof(ProxyResponse<GetCurrentIdByPeselResponse>), StatusCodes.Status200OK)]
        public async Task<ProxyResponse<GetCurrentIdByPeselResponse>> GetCurrentId([FromBody] GetCurrentIdByPeselRequest body)
        {
            var requestId = Guid.NewGuid().ToString();

            try
            {
                var result = await _rdoService.GetCurrentIdByPeselAsync(body, requestId);
                return result.ToProxyResponse("SRP", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd GetCurrentIdByPeselAsync, RequestId: {RequestId}", requestId);
                return new ProxyResponse<GetCurrentIdByPeselResponse>
                {
                    RequestId = requestId,
                    Data = null,
                    Source = "SRP",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
                    Message = ex.Message
                };
            }
        }



        /// <summary>Wyszukaj osoby w rejestrze PESEL.</summary>
        /// <remarks>
        /// Wymagania wejścia:
        /// 1) Podaj <b>PESEL</b> albo zestaw: <b>Nazwisko</b> i <b>Imię</b>
        ///    (Imię = <c>ImiePierwsze</c>; <c>ImieDrugie</c> jest opcjonalne).
        /// 2) Data urodzenia:
        ///    - <c>DataUrodzenia</c> akceptuje format <c>yyyyMMdd</c> lub <c>yyyy-MM-dd</c>.
        ///    - Zamiast dokładnej daty możesz podać zakres: <c>DataUrodzeniaOd</c>, <c>DataUrodzeniaDo</c>.
        ///    - Nie wolno podawać dokładnej daty i zakresu jednocześnie
        ///      (jeśli podasz dokładną datę, zakres zostanie zignorowany).
        /// 3) Dodatkowe kryteria opcjonalne: <c>ImieMatki</c>, <c>ImieOjca</c>.
        /// </remarks>
        [SwaggerOperation(
        Summary = "SRP – Wyszukiwanie osób w rejestrze PESEL",
        Description =
        "<b>Minimalne kryteria</b><br/>" +
        "Podaj <u>albo</u>: <code>Pesel</code>, <u>albo</u> zestaw: <code>Nazwisko</code> i <code>ImiePierwsze</code>." +
        "<ul>" +
        "<li><code>Pesel</code> – wyszukiwanie po numerze PESEL.</li>" +
        "<li><code>Nazwisko</code> + <code>ImiePierwsze</code> – wyszukiwanie po danych osobowych (opcjonalnie <code>ImieDrugie</code>).</li>" +
        "</ul>" +
        "<b>Data urodzenia</b><br/>" +
        "Podaj <u>albo</u> dokładną datę <code>DataUrodzenia</code> (yyyyMMdd lub yyyy-MM-dd), <u>albo</u> zakres: " +
        "<code>DataUrodzeniaOd</code>–<code>DataUrodzeniaDo</code>. Nie łącz obu wariantów – jeśli podasz dokładną datę, zakres będzie zignorowany." +
        "<br/><b>Dodatkowe pola:</b> <code>ImieMatki</code>, <code>ImieOjca</code> (opcjonalne)."+
            "</ul>" +
            "<b>Środowisko testowe:</b>" +
            "<ul>" +
            "W środowisku testowym znają się dane osoby, której pesel = 11111111111 albo dane 13 osób o parametrach: " +
            "<li>nazwisko = NOWAK</li>" +
            "<li>imiePierwsze = TOMASZ</li>" +
            "<li>imieOjca = KAZIMIERZ</li>" +
            "api zwraca Fault: Znaleziono więcej niż 50 osób. dla danych: " +
            "<li>nazwisko = NOWAK</li>" +
            "<li>imiePierwsze = TOMASZ</li>" +
            "inne parametry zapytania zwracają: Nie znaleziono osoby." +
            "</ul>"
        )]
        [Produces("application/json")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(ProxyResponse<SearchPersonResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProxyResponse<SearchPersonResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(SearchPerson200Example))]
        [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(SearchPerson400Example))]
        [HttpPost("search-person")]
        public async Task<ProxyResponse<SearchPersonResponse>> SearchPerson([FromBody] SearchPersonRequest body)
        {
            var requestId = Guid.NewGuid().ToString();

            try
            {
                var result = await _peselService.SearchPersonAsync(body, requestId);
                return result.ToProxyResponse("SRP", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blad SearchPerson, RequestId: {RequestId}", requestId);
                return new ProxyResponse<SearchPersonResponse>
                {
                    RequestId = requestId,
                    Data = null,
                    Source = "SRP",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
                    Message = ex.Message
                };
            }
        }


        [SwaggerOperation(
            Summary = "SRP – Udostępnij dane osoby po PESEL",
            Description =
        "<b>Parametry</b><br/>" +
        "<ul>" +
        "<li><code>Pesel</code> – numer PESEL osoby, dla której mają zostać udostępnione aktualne dane.</li>" +
        "</ul>" +
         "<b>Środowisko testowe:</b>" +
         "<ul>" +
            "W środowisku testowym znają się dane osoby, której pesel = 11111111111" +
         "</ul>"
        )]
        [HttpPost("get-person-by-pesel")]
        public async Task<ProxyResponse<GetPersonByPeselResponse>> GetPersonByPesel(GetPersonByPeselRequest body)
        {
            var requestId = Guid.NewGuid().ToString();
            
            try
            {
                var result = await _peselService.GetPersonByPeselAsync(body, requestId);
                return result.ToProxyResponse("SRP", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd GetPersonByPesel, RequestId: {RequestId}", requestId);
                return new ProxyResponse<GetPersonByPeselResponse>
                {
                    RequestId = requestId,
                    Data = null,
                    Source = "SRP",
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(),
                    Message = ex.Message
                };
            }


        }
              
    }
}
