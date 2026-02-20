using IntegrationHub.Common.Contracts;
using IntegrationHub.PIESP.Exceptions;
using IntegrationHub.PIESP.Models;
using IntegrationHub.PIESP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using System.Security.Claims;

namespace IntegrationHub.PIESP.Controllers
{
    /// <summary>
    /// Operacje na służbach (przegląd własnych służb, start/finish).
    /// </summary>
    [ApiController]
    [Route("piesp/[controller]")]
    [Authorize]
    [SwaggerTag("Służby użytkownika (PIESP)")]
    [Produces("application/json")]
    public class DutyController : ControllerBase
    {
        private readonly DutyService _duties;
        private readonly string _sourceName = "PIESP";

        public DutyController(DutyService duties)
        {
            _duties = duties;
        }

        private bool TryGetUserId(out Guid userId)
        {
            userId = Guid.Empty;
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idStr, out userId);
        }

        /// <summary>Zwraca zaplanowane służby (Status = Planned) dla bieżącego użytkownika w danym dniu.</summary>
        /// <param name="date">Dzień (UTC). Gdy null – dziś.</param>
        [HttpGet("my-planned-duties")]
        [SwaggerOperation(
            Summary = "Moje służby – zaplanowane",
            Description = "Pobiera z bazy służby użytkownika o statusie Planned w wybranym dniu (lub dziś)."
        )]
        [ProducesResponseType(typeof(ProxyResponse<IEnumerable<Duty> >), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ProxyResponse<IEnumerable<Duty>> GetDutiesPlannedForMe([FromQuery] DateTime? date = null)
        {
            var requestId = Guid.NewGuid().ToString();

            if (!TryGetUserId(out var userId)) return ProxyResponseFactory.BusinessError<IEnumerable<Duty>>(
                "Nieznany użytkownik!",
                _sourceName,
                HttpStatusCode.Unauthorized.ToString(),
                requestId);
            
            var duties = _duties.GetDutiesPlannedForUser(userId, date);
            if (duties.IsNullOrEmpty())
            {
                return ProxyResponseFactory.BusinessError<IEnumerable<Duty>>(
                    $"Brak zaplanowanych służb dla użytkownika w dniu {(date ?? DateTime.Today).ToString("yyyy-MM-dd")}.",
                    _sourceName,
                    HttpStatusCode.NotFound.ToString(),
                    requestId);
            }

            return ProxyResponseFactory.Success(
                duties, 
                _sourceName, 
                HttpStatusCode.OK.ToString(), 
                requestId);
        }

        /// <summary>Zwraca służby bieżącego użytkownika w danym dniu.</summary>
        /// <param name="date">Dzień (UTC). Gdy null – dziś.</param>
        [HttpGet("my-duties")]
        [SwaggerOperation(
            Summary = "Moje służby – wszystkie w dniu",
            Description = "Pobiera z bazy wszystkie służby użytkownika w wybranym dniu (lub dziś)."
        )]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult GetMyDuties([FromQuery] DateTime? date = null)
        {
            if (!TryGetUserId(out var userId)) return Forbid();
            var duties = _duties.GetDutiesForUserByDay(userId, date);
            return Ok(duties);
        }

        /// <summary>
        /// Zwraca aktualnie trwającą służbę bieżącego użytkownika (Status = InProgress).
        /// Założenie: użytkownik może mieć tylko jedną służbę w toku.
        /// </summary>
        [HttpGet("my-current-duty")]
        [SwaggerOperation(
            Summary = "Moja służba w toku",
            Description = "Zwraca pojedynczy rekord służby o statusie InProgress dla aktualnie zalogowanego użytkownika. " +
                          "Jeśli brak – HTTP 404. Jeśli wykryto wiele rekordów InProgress (błąd danych) – HTTP 409."
        )]
        [ProducesResponseType(typeof(ProxyResponse<Duty>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ProxyResponse<Duty> GetMyCurrentDuty()
        {
            
            
            var requestId = Guid.NewGuid().ToString();

            if (!TryGetUserId(out var userId)) return ProxyResponseFactory.BusinessError<Duty>(
                "Nieznany użytkownik!",
                _sourceName,
                HttpStatusCode.NotFound.ToString(),
                requestId);


            try
            {
                var duty = _duties.GetCurrentDutyForUser(userId);
                return duty is null ? 
                    ProxyResponseFactory.BusinessError<Duty>(
                        "Brak służby w toku dla tego użytkownika.",
                        _sourceName, 
                        HttpStatusCode.NotFound.ToString(),
                        requestId) 
                    :
                    ProxyResponseFactory.Success(
                        duty, 
                        _sourceName,
                        HttpStatusCode.OK.ToString(),
                        requestId);
            }
            catch (InvalidOperationException)
            {
                // SingleOrDefault wykrył >1 rekord – naruszenie założenia biznesowego
                return ProxyResponseFactory.BusinessError<Duty>(
                    "W systemie istnieje więcej niż jedna służba w toku dla tego użytkownika.",
                    _sourceName,
                    HttpStatusCode.Conflict.ToString(),
                    requestId);
            }
        }

        public record StartEndDutyRequest(
            int DutyId,
            DateTime DateTimeUtc,
            decimal? Latitude,
            decimal? Longitude
        );

        /// <summary>Rozpoczyna służbę (ustawia Status=InProgress i ActualStart).</summary>
        /// <param name="dutyId">Id służby.</param>
        /// <param name="req">Czas rozpoczęcia w UTC.</param>
        [HttpPost("start")]
        [SwaggerOperation(
            Summary = "Start służby",
            Description = "Startuje wskazaną służbę użytkownika. Waliduje brak innej trwającej służby."
        )]
        [ProducesResponseType(typeof(ProxyResponse<Duty>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]

        public ProxyResponse<Duty> StartDuty([FromBody] StartEndDutyRequest req)
        {
            var requestId = Guid.NewGuid().ToString();
            var result = new ProxyResponse<Duty>();

            try
            {
                if (!TryGetUserId(out var userId)) result = ProxyResponseFactory.BusinessError<Duty>(
                    "Nieznany użytkownik!",
                    _sourceName,
                    HttpStatusCode.NotFound.ToString(),
                    requestId);

                var ok = _duties.StartDuty(
                    req.DutyId,
                    userId,
                    req.DateTimeUtc,
                    req.Latitude,
                    req.Longitude);
                if (ok)
                {
                    var currentDuty = _duties.GetCurrentDutyForUser(userId);
                    result = ProxyResponseFactory.Success(currentDuty!, _sourceName, HttpStatusCode.OK.ToString(), requestId);
                    result.Message = "Służba rozpoczęta!";
                }
                else
                {
                    result = ProxyResponseFactory.BusinessError<Duty>("Nie można rozpocząć tej służby.", _sourceName, HttpStatusCode.Conflict.ToString(), requestId);
                }
            }
            catch (Exception ex)
            {
                result = ProxyResponseFactory.TechnicalError<Duty>(
                    ex.Message,
                    _sourceName,
                    HttpStatusCode.InternalServerError.ToString(),
                    requestId);
            }
            return result;
        }

        /// <summary>Kończy służbę (ustawia Status=Finished i ActualEnd).</summary>
        /// <param name="dutyId">Id służby.</param>
        /// <param name="req">Czas zakończenia w UTC.</param>
        [HttpPost("finish")]
        [SwaggerOperation(
            Summary = "Zakończenie służby",
            Description = "Kończy wskazaną służbę użytkownika. Wymaga, by służba była InProgress."
        )]
        [ProducesResponseType(typeof(ProxyResponse<Duty>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ProxyResponse<Duty> FinishDuty([FromBody] StartEndDutyRequest req)
        {

            var requestId = Guid.NewGuid().ToString();
            var result = new ProxyResponse<Duty>();

            try
            {
                if (!TryGetUserId(out var userId)) result = ProxyResponseFactory.BusinessError<Duty>(
                        "Nieznany użytkownik!",
                        _sourceName,
                        HttpStatusCode.NotFound.ToString(),
                        requestId);

                var ok = _duties.FinishDuty(
                    req.DutyId,
                    userId,
                    req.DateTimeUtc,
                    req.Latitude,
                    req.Longitude);
                if (ok)
                {
                    var currentDuty = _duties.GetDuty(req.DutyId);
                    result = ProxyResponseFactory.Success(currentDuty!, _sourceName, HttpStatusCode.OK.ToString(), requestId);
                    result.Message = "Służba zakończona!";
                }
                else
                {
                    result = ProxyResponseFactory.BusinessError<Duty>("Nie można zakończyć tej służby.", _sourceName, HttpStatusCode.Conflict.ToString(), requestId);
                }
            }
            catch (Exception ex)
            {
                result = ProxyResponseFactory.TechnicalError<Duty>(
                    ex.Message,
                    _sourceName,
                    HttpStatusCode.InternalServerError.ToString(),
                    requestId);
            }
            
            return result;
            
        }
    }
}
