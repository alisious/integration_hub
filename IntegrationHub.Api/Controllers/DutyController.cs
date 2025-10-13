using IntegrationHub.PIESP.Exceptions;
using IntegrationHub.PIESP.Models;
using IntegrationHub.PIESP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult GetDutiesPlannedForMe([FromQuery] DateTime? date = null)
        {
            if (!TryGetUserId(out var userId)) return Forbid();
            var duties = _duties.GetDutiesPlannedForUser(userId, date);
            return Ok(duties);
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
        [ProducesResponseType(typeof(Duty), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        public IActionResult GetMyCurrentDuty()
        {
            if (!TryGetUserId(out var userId)) return Forbid();

            try
            {
                var duty = _duties.GetCurrentDutyForUser(userId);
                return duty is null ? NotFound() : Ok(duty);
            }
            catch (InvalidOperationException)
            {
                // SingleOrDefault wykrył >1 rekord – naruszenie założenia biznesowego
                return Conflict("W systemie istnieje więcej niż jedna służba w toku dla tego użytkownika.");
            }
        }

        public record StartEndDutyRequest(DateTime DateTimeUtc);

        /// <summary>Rozpoczyna służbę (ustawia Status=InProgress i ActualStart).</summary>
        /// <param name="dutyId">Id służby.</param>
        /// <param name="req">Czas rozpoczęcia w UTC.</param>
        [HttpPost("{dutyId}/start")]
        [SwaggerOperation(
            Summary = "Start służby",
            Description = "Startuje wskazaną służbę użytkownika. Waliduje brak innej trwającej służby."
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        public IActionResult StartDuty(int dutyId, [FromBody] StartEndDutyRequest req)
        {
            if (!TryGetUserId(out var userId)) return Forbid();
            var ok = _duties.StartDuty(dutyId, userId, req.DateTimeUtc);
            return ok ? Ok() : Conflict("Nie można rozpocząć tej służby.");
        }

        /// <summary>Kończy służbę (ustawia Status=Finished i ActualEnd).</summary>
        /// <param name="dutyId">Id służby.</param>
        /// <param name="req">Czas zakończenia w UTC.</param>
        [HttpPost("{dutyId}/finish")]
        [SwaggerOperation(
            Summary = "Zakończenie służby",
            Description = "Kończy wskazaną służbę użytkownika. Wymaga, by służba była InProgress."
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        public IActionResult FinishDuty(int dutyId, [FromBody] StartEndDutyRequest req)
        {
            if (!TryGetUserId(out var userId)) return Forbid();
            var ok = _duties.FinishDuty(dutyId, userId, req.DateTimeUtc);
            return ok ? Ok() : Conflict("Nie można zakończyć tej służby.");
        }
    }
}
