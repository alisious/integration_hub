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
    /// Operacje uwierzytelniania: logowanie przez Active Directory, odświeżanie tokenu,
    /// dane bieżącego użytkownika oraz wylogowanie.
    /// </summary>
    [ApiController]
    [Route("piesp/[controller]")]
    [ApiExplorerSettings(GroupName = "v1")]
    [SwaggerTag("Uwierzytelnianie (PIESP)")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        // =========================
        //  OBSOLETE PIN
        // =========================

        /// <summary>Endpoint zachowany wyłącznie dla zgodności wstecznej. Logowanie PIN zostało wyłączone.</summary>
        /// <remarks>
        /// Użytkownik powinien logować się przez Active Directory podając <b>sAMAccountName</b> i hasło domenowe.
        /// </remarks>
        [HttpPost("reset-pin")]
        [SwaggerOperation(
            Summary = "Wyłączone po przejściu na AD",
            Description = "Operacja resetu PIN jest niedostępna, ponieważ API używa logowania domenowego Active Directory."
        )]
        [ProducesResponseType(typeof(string), StatusCodes.Status410Gone)]
        public IActionResult ResetPin([FromBody] ResetPinRequest req) =>
            StatusCode(StatusCodes.Status410Gone, "Reset PIN jest wyłączony. Użyj logowania przez Active Directory.");

        /// <summary>Endpoint zachowany wyłącznie dla zgodności wstecznej. Logowanie PIN zostało wyłączone.</summary>
        /// <remarks>Po migracji na AD zmiana PIN nie jest wspierana przez API.</remarks>
        [HttpPost("change-pin")]
        [Authorize]
        [SwaggerOperation(
            Summary = "Wyłączone po przejściu na AD",
            Description = "Operacja zmiany PIN jest niedostępna, ponieważ API używa logowania domenowego Active Directory."
        )]
        [ProducesResponseType(typeof(string), StatusCodes.Status410Gone)]
        public IActionResult ChangePin([FromBody] ChangePinRequest req) =>
            StatusCode(StatusCodes.Status410Gone, "Zmiana PIN jest wyłączona. Użyj logowania przez Active Directory.");

        // =========================
        //  LOGIN / REFRESH / LOGOUT
        // =========================

        /// <summary>Loguje użytkownika przez Active Directory i zwraca parę tokenów (access + refresh).</summary>
        /// <remarks>
        /// Wymagane pola: <b>samAccountName</b>, <b>password</b>.<br/>
        /// API najpierw weryfikuje poświadczenia w AD, a następnie odczytuje lokalny rekord użytkownika i role.
        /// </remarks>
        [HttpPost("login")]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "Logowanie przez AD (JWT + refresh)",
            Description = "Weryfikuje login sAMAccountName i hasło domenowe w Active Directory. Po poprawnym logowaniu zwraca access token i refresh token na podstawie lokalnego konta API."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status403Forbidden)]
        [SwaggerResponseExample(StatusCodes.Status401Unauthorized, typeof(Login401Example))]
        [SwaggerResponseExample(StatusCodes.Status403Forbidden, typeof(Login403Example))]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _authService.LoginAsync(req.SamAccountName, req.Password, HttpContext.RequestAborted);
            if (user == null) return Unauthorized("Nie udało się zalogować. Sprawdź login domenowy i hasło albo upewnij się, że konto ma lokalne uprawnienia w API.");
            if (!user.IsActive) return Forbid("Konto jest zablokowane lub nieaktywne. Skontaktuj się z przełożonym.");

            var access = _authService.IssueAccessToken(user);
            var (refresh, _, _) = await _authService.IssueRefreshTokenAsync(user.Id);

            var roles = user.Roles.Select(r => r.Role.ToString());
            return Ok(new { accessToken = access, refreshToken = refresh, roles, userName = user.UserName, samAccountName = user.SamAccountName });
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

        /// <param name="SamAccountName">Login domenowy użytkownika (sAMAccountName).</param>
        /// <param name="Password">Hasło domenowe użytkownika.</param>
        public record LoginRequest(string SamAccountName, string Password);

        /// <param name="UserId">Identyfikator użytkownika (GUID).</param>
        /// <param name="RefreshToken">Obecny refresh token (opaque Base64).</param>
        public record RefreshRequest(Guid UserId, string RefreshToken);

        /// <param name="RefreshToken">Opcjonalny refresh token do unieważnienia wraz z bieżącym accessem.</param>
        public record LogoutRequest(string? RefreshToken);
    }
}
