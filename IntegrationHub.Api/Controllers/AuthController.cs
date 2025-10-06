using IntegrationHub.PIESP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;


namespace IntegrationHub.PIESP.Controllers
{
    // AuthController.cs
    /// <summary>
    /// Kontroler odpowiedzialny za logowanie i reset PIN-u użytkowników.
    /// </summary>
    
    [ApiController]
    [Route("piesp/[controller]")]
    [ApiExplorerSettings(GroupName = "v1")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly SupervisorService _supervisorService;
        private readonly IConfiguration _config;

        public AuthController(AuthService authService, SupervisorService supervisorService, IConfiguration config)
        {
            _authService = authService;
            _supervisorService = supervisorService;
            _config = config;
        }

        /// <summary>
        /// Resetuje PIN użytkownika po weryfikacji kodu bezpieczeństwa.
        /// </summary>
        /// <param name="req">Numer odznaki, kod bezpieczeństwa i nowy PIN.</param>
        /// <returns>Kod 200 przy sukcesie, 400 przy błędnym kodzie, 404 jeśli użytkownik nie istnieje.</returns>
        [HttpPost("reset-pin")]
        public async Task<IActionResult> ResetPin([FromBody] ResetPinRequest req)
        {
            var valid = await _supervisorService.ValidateSecurityCodeAsync(req.BadgeNumber, req.SecurityCode);
            if (!valid) return BadRequest("Błędny kod bezpieczeństwa.");
            var user = await _authService.SetPinAsync(req.BadgeNumber, req.NewPin);
            return user != null ? Ok("Ustawiono nowy PIN.") : NotFound("Nie udało się ustawić nowego PIN.");
        }


        /// <summary>
        /// Zmienia PIN użytkownika.
        /// </summary>
        /// <param name="req">Numer odznaki, kod bezpieczeństwa i nowy PIN.</param>
        /// <returns>Kod 200 przy sukcesie, 400 przy błędnym kodzie, 404 jeśli użytkownik nie istnieje.</returns>
        [HttpPost("change-pin")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> ChangePin([FromBody] ChangePinRequest req)
        {
            var badge = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentPinIsValid = await _authService.ConfirmCurrentPinAsync(badge!, req.CurrentPin);
            if (!currentPinIsValid) return BadRequest("Nieprawidłowy PIN.");
                        
            var user = await _authService.SetPinAsync(badge!, req.NewPin);
            return user != null ? Ok("PIN został zmieniony.") : NotFound("Nie udało się zmienić PIN.");
        }


        /// <summary>
        /// Loguje użytkownika i zwraca token JWT.
        /// </summary>
        /// <param name="req">Numer odznaki i PIN.</param>
        /// <returns>Token JWT, role użytkownika oraz imię i nazwisko.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _authService.LoginAsync(req.BadgeNumber, req.Pin);
            if (user == null) return Unauthorized();

            var jwtKey = _config["Jwt:Key"];
            var token = _authService.GenerateJwtToken(user, jwtKey);

            var roles = user.Roles.Select(r => r.Role.ToString());
            return Ok(new { Token = token, Roles = roles, user.UserName });
        }

        /// <summary>
        /// Model żądania resetu PIN-u.
        /// </summary>
        /// <param name="BadgeNumber">Numer odznaki użytkownika.</param>
        /// <param name="SecurityCode">Kod bezpieczeństwa otrzymany od przełożonego.</param>
        /// <param name="NewPin">Nowy PIN.</param>
        public record ResetPinRequest(string BadgeNumber, string SecurityCode, string NewPin);

        /// <summary>
        /// Model żądania resetu PIN-u.
        /// </summary>
        /// <param name="BadgeNumber">Numer odznaki użytkownika.</param>
        /// <param name="CurrentPin">Aktualny PIN.</param>
        /// <param name="NewPin">Nowy PIN.</param>
        public record ChangePinRequest(string CurrentPin, string NewPin);

        /// <summary>
        /// Model żądania logowania.
        /// </summary>
        /// <param name="BadgeNumber">Numer odznaki użytkownika.</param>
        /// <param name="Pin">PIN użytkownika.</param>
        public record LoginRequest(string BadgeNumber, string Pin);

    }
}
