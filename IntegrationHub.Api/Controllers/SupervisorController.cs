using IntegrationHub.PIESP.Models;
using IntegrationHub.PIESP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace IntegrationHub.PIESP.Controllers
{
    /// <summary>
    /// Operacje przełożonego: generowanie kodów bezpieczeństwa, zarządzanie rolami,
    /// wymuszone wylogowanie użytkownika (force-logout).
    /// </summary>
    [ApiController]
    [Route("piesp/[controller]")]
    [Authorize(Roles = "Supervisor")]
    [ApiExplorerSettings(GroupName = "v1")]
    [SwaggerTag("Operacje przełożonego (PIESP)")]
    [Produces("application/json")]
    public class SupervisorController : ControllerBase
    {
        private readonly SupervisorService _supervisorService;
        private readonly AuthService _authService;

        public SupervisorController(SupervisorService supervisorService, AuthService authService)
        {
            _supervisorService = supervisorService;
            _authService = authService;
        }

        /// <summary>Generuje jednorazowy kod bezpieczeństwa dla wskazanego numeru odznaki.</summary>
        [HttpPost("generate-code")]
        [SwaggerOperation(
            Summary = "Generowanie kodu bezpieczeństwa",
            Description = "Tworzy jednorazowy kod bezpieczeństwa dla użytkownika. Kod służy do resetu PIN."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateCode([FromBody] GenerateCodeRequest req)
        {
            var code = await _supervisorService.GenerateCodeAsync(req.BadgeNumber);
            return Ok(new { securityCode = code });
        }

        /// <summary>Nadaje wskazaną rolę użytkownikowi.</summary>
        [HttpPost("assign-role")]
        [SwaggerOperation(
            Summary = "Przypisz rolę użytkownikowi",
            Description = "Nadaje użytkownikowi rolę (User/Supervisor/PowerUser) na podstawie numeru odznaki."
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignRole([FromBody] RoleChangeRequest req)
        {
            var success = await _supervisorService.AssignRoleAsync(req.BadgeNumber, req.Role);
            return success ? Ok() : NotFound("User not found or role already assigned.");
        }

        /// <summary>Odbiera wskazaną rolę użytkownikowi.</summary>
        [HttpPost("revoke-role")]
        [SwaggerOperation(
            Summary = "Odbierz rolę użytkownikowi",
            Description = "Usuwa wskazaną rolę użytkownikowi z podanym numerem odznaki."
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokeRole([FromBody] RoleChangeRequest req)
        {
            var success = await _supervisorService.RevokeRoleAsync(req.BadgeNumber, req.Role);
            return success ? Ok() : NotFound("User or role not found.");
        }

        /// <summary>
        /// Wymusza wylogowanie użytkownika: inkrementuje jego <c>TokenVersion</c>
        /// i unieważnia wszystkie refresh tokeny. Stare access tokeny przestają działać,
        /// ale użytkownik może natychmiast zalogować się ponownie.
        /// </summary>
        [HttpPost("force-logout")]
        [SwaggerOperation(
            Summary = "Wymuszone wylogowanie użytkownika",
            Description = "Unieważnia wszystkie dotychczasowe tokeny użytkownika przez podniesienie TokenVersion i revoke refresh tokenów."
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ForceLogout([FromBody] ForceLogoutRequest req)
        {
            var ok = await _authService.ForceLogoutByBadgeAsync(req.BadgeNumber);
            return ok ? Ok() : NotFound("Nie znaleziono użytkownika o podanym numerze odznaki.");
        }

        // =========================
        //  Request models
        // =========================

        /// <param name="BadgeNumber">Numer odznaki użytkownika.</param>
        public record GenerateCodeRequest(string BadgeNumber);

        /// <param name="BadgeNumber">Numer odznaki użytkownika.</param>
        /// <param name="Role">Rola do przypisania/odebrania (User/Supervisor/PowerUser).</param>
        public record RoleChangeRequest(string BadgeNumber, RoleType Role);

        /// <param name="BadgeNumber">Numer odznaki użytkownika do wymuszonego wylogowania.</param>
        public record ForceLogoutRequest(string BadgeNumber);
    }
}
