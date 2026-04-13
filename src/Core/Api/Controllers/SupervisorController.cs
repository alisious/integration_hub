using IntegrationHub.PIESP.Models;
using IntegrationHub.PIESP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace IntegrationHub.PIESP.Controllers
{
    /// <summary>
    /// Operacje przełożonego: zarządzanie rolami,
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

        /// <summary>Endpoint zachowany wyłącznie dla zgodności wstecznej. Reset PIN został wyłączony.</summary>
        [HttpPost("generate-code")]
        [SwaggerOperation(
            Summary = "Wyłączone po przejściu na AD",
            Description = "Generowanie kodu bezpieczeństwa jest niedostępne, ponieważ API nie używa już PIN-ów do logowania."
        )]
        [ProducesResponseType(typeof(string), StatusCodes.Status410Gone)]
        public IActionResult GenerateCode([FromBody] GenerateCodeRequest req) =>
            StatusCode(StatusCodes.Status410Gone, "Generowanie kodów bezpieczeństwa jest wyłączone. API używa logowania przez Active Directory.");

        /// <summary>Nadaje wskazaną rolę użytkownikowi.</summary>
        [HttpPost("assign-role")]
        [SwaggerOperation(
            Summary = "Przypisz rolę użytkownikowi",
            Description = "Nadaje użytkownikowi rolę (User/Supervisor/PowerUser) na podstawie loginu domenowego sAMAccountName."
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignRole([FromBody] RoleChangeRequest req)
        {
            var success = await _supervisorService.AssignRoleAsync(req.SamAccountName, req.Role);
            return success ? Ok() : NotFound("User not found or role already assigned.");
        }

        /// <summary>Odbiera wskazaną rolę użytkownikowi.</summary>
        [HttpPost("revoke-role")]
        [SwaggerOperation(
            Summary = "Odbierz rolę użytkownikowi",
            Description = "Usuwa wskazaną rolę użytkownikowi z podanym loginem domenowym sAMAccountName."
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokeRole([FromBody] RoleChangeRequest req)
        {
            var success = await _supervisorService.RevokeRoleAsync(req.SamAccountName, req.Role);
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
            var ok = await _authService.ForceLogoutBySamAccountNameAsync(req.SamAccountName);
            return ok ? Ok() : NotFound("Nie znaleziono użytkownika o podanym loginie domenowym.");
        }

        // =========================
        //  Request models
        // =========================

        /// <param name="SamAccountName">Login domenowy użytkownika (sAMAccountName).</param>
        public record GenerateCodeRequest(string SamAccountName);

        /// <param name="SamAccountName">Login domenowy użytkownika (sAMAccountName).</param>
        /// <param name="Role">Rola do przypisania/odebrania (User/Supervisor/PowerUser).</param>
        public record RoleChangeRequest(string SamAccountName, RoleType Role);

        /// <param name="SamAccountName">Login domenowy użytkownika do wymuszonego wylogowania.</param>
        public record ForceLogoutRequest(string SamAccountName);
    }
}
