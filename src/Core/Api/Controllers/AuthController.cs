using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IntegrationHub.Api.Swagger.Examples.PIESP;
using IntegrationHub.PIESP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace IntegrationHub.PIESP.Controllers
{
    /// <summary>
    /// Operacje uwierzytelniania: logowanie, odświeżanie tokenu, zmiana/reset PIN, dane bieżącego użytkownika oraz wylogowanie.
    /// </summary>
    [ApiController]
    [Route("piesp/[controller]")]
    [ApiExplorerSettings(GroupName = "v1")]
    [SwaggerTag("Uwierzytelnianie (PIESP)")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly SupervisorService _supervisorService;

        public AuthController(AuthService authService, SupervisorService supervisorService)
        {
            _authService = authService;
            _supervisorService = supervisorService;
        }

        // =========================
        //  PIN
        // =========================

        /// <summary>Resetuje PIN użytkownika po poprawnym kodzie bezpieczeństwa.</summary>
        /// <remarks>
        /// Wymagane pola: <b>badgeNumber</b>, <b>securityCode</b>, <b>newPin</b>.
        /// </remarks>
        [HttpPost("reset-pin")]
        [SwaggerOperation(
            Summary = "Reset PIN (kod bezpieczeństwa)",
            Description = "Weryfikuje kod bezpieczeństwa i ustawia nowy PIN dla użytkownika o wskazanym numerze odznaki."
        )]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPin([FromBody] ResetPinRequest req)
        {
            var valid = await _supervisorService.ValidateSecurityCodeAsync(req.BadgeNumber, req.SecurityCode);
            if (!valid) return BadRequest("Błędny kod bezpieczeństwa.");

            var user = await _authService.SetPinAsync(req.BadgeNumber, req.NewPin);
            return user != null ? Ok("Ustawiono nowy PIN.") : NotFound("Nie udało się ustawić nowego PIN.");
        }

        /// <summary>Zmienia PIN bieżącego, zalogowanego użytkownika.</summary>
        /// <remarks>Wymaga ważnego tokena. Weryfikuje aktualny PIN (po UserId z claimu) i zapisuje nowy.</remarks>
        [HttpPost("change-pin")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Zmiana PIN przez zalogowanego użytkownika",
            Description = "Wymaga ważnego tokena. Weryfikuje aktualny PIN i zapisuje nowy."
        )]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePin([FromBody] ChangePinRequest req)
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier); // GUID UserId
            if (!Guid.TryParse(idStr, out var userId)) return Unauthorized();

            var ok = await _authService.ConfirmCurrentPinByUserIdAsync(userId, req.CurrentPin);
            if (!ok) return BadRequest("Nieprawidłowy PIN.");

            var user = await _authService.SetPinByUserIdAsync(userId, req.NewPin);
            return user != null ? Ok("PIN został zmieniony.") : NotFound("Nie udało się zmienić PIN.");
        }

        // =========================
        //  LOGIN / REFRESH / LOGOUT
        // =========================

        /// <summary>Loguje użytkownika i zwraca parę tokenów (access + refresh).</summary>
        /// <remarks>
        /// Wymagane pola: <b>badgeNumber</b>, <b>pin</b>.<br/>
        /// Access JWT zawiera claimy: <b>nameidentifier = UserId (GUID)</b>, <b>ver = TokenVersion</b>, listę ról oraz <b>jti</b>.
        /// </remarks>
        [HttpPost("login")]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "Logowanie (JWT + refresh)",
            Description = "Zwraca krótkotrwały access token i długotrwały refresh token. Access zawiera UserId (NameIdentifier), jti oraz ver (TokenVersion)."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status403Forbidden)]
        [SwaggerResponseExample(StatusCodes.Status401Unauthorized, typeof(Login401Example))]
        [SwaggerResponseExample(StatusCodes.Status403Forbidden, typeof(Login403Example))]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _authService.LoginAsync(req.BadgeNumber, req.Pin);
            if (user == null) return Unauthorized("Nie udało się zalogować. Sprawdź numer odznaki i PIN i spróbuj ponownie.");
            if (!user.IsActive) return Forbid("Konto jest zablokowane lub nieaktywne. Skontaktuj się z przełożonym.");

            var access = _authService.IssueAccessToken(user);
            var (refresh, _, _) = await _authService.IssueRefreshTokenAsync(user.Id);

            var roles = user.Roles.Select(r => r.Role.ToString());
            return Ok(new { accessToken = access, refreshToken = refresh, roles, userName = user.UserName });
        }

        /// <summary>Odświeża parę tokenów (rotacja refresh i nowy access).</summary>
        /// <remarks>
        /// Wymaga: <b>userId</b> (GUID) oraz <b>refreshToken</b> (opaque).<br/>
        /// Rotacja: bieżący refresh zostaje unieważniony i zastąpiony nowym w tej samej rodzinie.
        /// </remarks>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "Odśwież token (rotacja refresh)",
            Description = "Weryfikuje i rotuje refresh token, zwraca nową parę access+refresh. Wykrywa reuse."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
        {
            var res = await _authService.RefreshAsync(req.UserId, req.RefreshToken);
            if (res is null) return Unauthorized();

            return Ok(new { accessToken = res.Value.AccessToken, refreshToken = res.Value.RefreshToken });
        }

        /// <summary>Wylogowuje użytkownika: unieważnia bieżący access (JTI) oraz opcjonalnie przekazany refresh.</summary>
        /// <remarks>
        /// Bieżący access token identyfikowany jest przez claim <b>jti</b> w nagłówku <b>Authorization: Bearer ...</b>.<br/>
        /// Jeśli w body podasz <b>refreshToken</b>, zostanie on również unieważniony.
        /// </remarks>
        [HttpPost("logout")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Wylogowanie (unieważnienie bieżącego tokenu)",
            Description = "Dodaje JTI do blacklisty. Opcjonalnie unieważnia także bieżący refresh token."
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest? req)
        {
            var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (string.IsNullOrWhiteSpace(jti)) return Unauthorized();

            await _authService.LogoutAsync(jti, req?.RefreshToken);
            return Ok();
        }

        /// <summary>Wylogowuje użytkownika ze wszystkich urządzeń (podnosi TokenVersion i unieważnia wszystkie refresh tokeny).</summary>
        [HttpPost("logout-all")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Wyloguj ze wszystkich urządzeń",
            Description = "Podnosi Users.TokenVersion i unieważnia wszystkie refresh tokeny użytkownika."
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LogoutAll()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(idStr, out var userId)) return Unauthorized();

            await _authService.LogoutAllForUserAsync(userId);
            return Ok();
        }

        // =========================
        //  ME
        // =========================

        /// <summary>Zwraca pełne dane bieżącego użytkownika (łącznie z rolami).</summary>
        [HttpGet("me")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Bieżący użytkownik (profil)",
            Description = "Odczytuje GUID z claimu NameIdentifier i zwraca pełny rekord użytkownika z listą ról."
        )]
        [ProducesResponseType(typeof(IntegrationHub.PIESP.Models.User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Me()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(idStr, out var userId)) return Unauthorized();

            var me = await _authService.MeAsync(userId);
            if (me is null) return NotFound();

            return Ok(me);
        }

        // =========================
        //  Request models
        // =========================

        /// <param name="BadgeNumber">Numer odznaki użytkownika.</param>
        /// <param name="SecurityCode">Kod bezpieczeństwa od przełożonego.</param>
        /// <param name="NewPin">Nowy PIN.</param>
        public record ResetPinRequest(string BadgeNumber, string SecurityCode, string NewPin);

        /// <param name="CurrentPin">Aktualny PIN.</param>
        /// <param name="NewPin">Nowy PIN.</param>
        public record ChangePinRequest(string CurrentPin, string NewPin);

        /// <param name="BadgeNumber">Numer odznaki użytkownika.</param>
        /// <param name="Pin">PIN użytkownika.</param>
        public record LoginRequest(string BadgeNumber, string Pin);

        /// <param name="UserId">Identyfikator użytkownika (GUID).</param>
        /// <param name="RefreshToken">Obecny refresh token (opaque Base64).</param>
        public record RefreshRequest(Guid UserId, string RefreshToken);

        /// <param name="RefreshToken">Opcjonalny refresh token do unieważnienia wraz z bieżącym accessem.</param>
        public record LogoutRequest(string? RefreshToken);
    }
}
