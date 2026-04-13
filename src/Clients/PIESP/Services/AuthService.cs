using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IntegrationHub.PIESP.Data;
using IntegrationHub.PIESP.Models;
using IntegrationHub.PIESP.Security;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace IntegrationHub.PIESP.Services
{
    /// <summary>
    /// Serwis odpowiedzialny za logikę uwierzytelniania, tokeny i operacje sesyjne.
    /// </summary>
    public sealed class AuthService
    {
        private readonly PiespDbContext _context;
        private readonly ILogger<AuthService> _log;
        private readonly ActiveDirectoryAuthenticationService _activeDirectoryAuthenticationService;
        private readonly string _jwtKey;

        public AuthService(
            PiespDbContext context,
            ILogger<AuthService> log,
            IConfiguration cfg,
            ActiveDirectoryAuthenticationService activeDirectoryAuthenticationService)
        {
            _context = context;
            _log = log;
            _activeDirectoryAuthenticationService = activeDirectoryAuthenticationService;
            _jwtKey = cfg["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key in configuration.");
        }

        // =========================
        //  PIN / UŻYTKOWNIK
        // =========================

        /// <summary>Ustawia nowy PIN dla użytkownika wyszukanego po numerze odznaki.</summary>
        public async Task<User?> SetPinAsync(string badge, string newPin)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.BadgeNumber == badge);
            if (user == null) return null;
            user.PinHash = PinHasher.Hash(newPin);
            await _context.SaveChangesAsync();
            _log.LogInformation("PIN zmieniony (badge={Badge})", badge);
            return user;
        }

        /// <summary>Weryfikuje obecny PIN użytkownika (po badge).</summary>
        public async Task<bool> ConfirmCurrentPinAsync(string badge, string pin)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.BadgeNumber == badge);
            if (user == null) return false;
            return PinHasher.Verify(pin, user.PinHash);
        }

        /// <summary>Weryfikuje obecny PIN użytkownika (po UserId).</summary>
        public async Task<bool> ConfirmCurrentPinByUserIdAsync(Guid userId, string pin)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;
            return PinHasher.Verify(pin, user.PinHash);
        }

        /// <summary>Ustawia nowy PIN (po UserId).</summary>
        public async Task<User?> SetPinByUserIdAsync(Guid userId, string newPin)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;
            user.PinHash = PinHasher.Hash(newPin);
            await _context.SaveChangesAsync();
            _log.LogInformation("PIN zmieniony (userId={UserId})", userId);
            return user;
        }

        /// <summary>
        /// Logowanie przez Active Directory: najpierw weryfikuje poświadczenia domenowe,
        /// następnie wyszukuje lokalnego użytkownika po sAMAccountName.
        /// </summary>
        public async Task<User?> LoginAsync(string samAccountName, string password, CancellationToken ct = default)
        {
            try
            {
                var normalizedLogin = NormalizeSamAccountName(samAccountName);
                if (string.IsNullOrWhiteSpace(normalizedLogin))
                    return null;

                var valid = await _activeDirectoryAuthenticationService.ValidateCredentialsAsync(normalizedLogin, password, ct);
                if (!valid) return null;

                var user = await _context.Users
                    .Include(u => u.Roles)
                    .FirstOrDefaultAsync(u => u.SamAccountName == normalizedLogin, ct);

                if (user == null) return null;

                return user;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Błąd podczas logowania AD (samAccountName={SamAccountName})", samAccountName);
                return null;
            }
        }

        /// <summary>Zwraca pełne dane bieżącego użytkownika (łącznie z rolami).</summary>
        public Task<User?> MeAsync(Guid userId) =>
            _context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId);

        // =========================
        //  ACCESS JWT
        // =========================

        /// <summary>
        /// Generuje krótkotrwały access token JWT.
        /// Claimy: NameIdentifier = UserId (GUID), Name = UserName, jti (losowe), ver = Users.TokenVersion, role = lista ról.
        /// </summary>
        public string IssueAccessToken(User user, TimeSpan? ttl = null)
        {
            var now = DateTime.UtcNow;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(System.Security.Claims.ClaimTypes.Name, user.UserName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new("ver", user.TokenVersion.ToString())
            };
            //Dodanie nazwy jednostki do claimów, jeśli istnieje
            if (!string.IsNullOrWhiteSpace(user.UnitName))
            {
                claims.Add(new System.Security.Claims.Claim("unit_name", user.UnitName));
            }
            //Dodanie numeru odznaki do claimów jeśli istnieje
            if (!string.IsNullOrWhiteSpace(user.BadgeNumber))
            {
                claims.Add(new System.Security.Claims.Claim("badge_number", user.BadgeNumber));
            }
            if (!string.IsNullOrWhiteSpace(user.SamAccountName))
            {
                claims.Add(new System.Security.Claims.Claim("sam_account_name", user.SamAccountName));
            }

            foreach (var r in user.Roles.Select(x => x.Role.ToString()))
                claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, r));

            var jwt = new JwtSecurityToken(
                claims: claims,
                notBefore: now,
                expires: now.Add(ttl ?? TimeSpan.FromMinutes(60)),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        /// <summary>
        /// Zachowuje zgodność wsteczną z kontrolerem – wywołuje <see cref="IssueAccessToken"/>.
        /// Parametr <c>jwtKey</c> jest ignorowany (używamy konfiguracji z konstruktora).
        /// </summary>
        public string GenerateJwtToken(User user, string jwtKey) => IssueAccessToken(user);

        // =========================
        //  REFRESH TOKENS (opaque)
        // =========================

        /// <summary>
        /// Wystawia refresh token (opaque), zapisując tylko jego SHA-256 w bazie. Zwraca surową wartość do klienta.
        /// </summary>
        public async Task<(string RefreshToken, int RefreshId, Guid FamilyId)> IssueRefreshTokenAsync(
            Guid userId, Guid? familyId = null, TimeSpan? ttl = null)
        {
            var raw = RandomNumberGenerator.GetBytes(32); // 256-bit
            var token = Convert.ToBase64String(raw);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(token));

            var rt = new RefreshToken
            {
                UserId = userId,
                FamilyId = familyId ?? Guid.NewGuid(),
                TokenHash = hash,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromDays(7))
            };

            _context.RefreshTokens.Add(rt);
            await _context.SaveChangesAsync();

            return (token, rt.Id, rt.FamilyId);
        }

        /// <summary>
        /// Odświeża parę tokenów: waliduje bieżący refresh, rotuje go na nowy i zwraca nowy access+refresh.
        /// Wykrycie reuse może skutkować unieważnieniem całej rodziny.
        /// </summary>
        public async Task<(string AccessToken, string RefreshToken)?> RefreshAsync(Guid userId, string currentRefreshToken)
        {
            using var sha = SHA256.Create();
            var currHash = sha.ComputeHash(Encoding.UTF8.GetBytes(currentRefreshToken));

            var now = DateTime.UtcNow;

            var current = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.UserId == userId &&
                                          r.RevokedAt == null &&
                                          r.ExpiresAt > now &&
                                          r.TokenHash == currHash);
            if (current == null) return null;

            // załaduj usera (z rolami do access tokenu)
            var user = await _context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || !user.IsActive) return null;

            // rotacja: oznacz bieżący jako revoked i wystaw nowy w tej samej rodzinie
            var (newRefresh, newId, _) = await IssueRefreshTokenAsync(userId, current.FamilyId);
            current.RevokedAt = now;
            current.RevokedReason = "rotated";
            current.ReplacedById = newId;

            await _context.SaveChangesAsync();

            var access = IssueAccessToken(user);
            return (access, newRefresh);
        }

        /// <summary>
        /// Logout użytkownika: unieważnia JTI bieżącego access tokenu (blacklista) oraz opcjonalnie bieżący refresh (jeśli przekazany).
        /// </summary>
        public async Task LogoutAsync(string jti, string? refreshToken)
        {
            // Access token -> RevokedTokens
            await RevokeTokenAsync(jti, DateTime.UtcNow.AddMinutes(30)); // bufor > TTL access

            // Opcjonalnie – unieważnij przekazany refresh
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                using var sha = SHA256.Create();
                var h = sha.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));

                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE piesp.RefreshTokens SET RevokedAt = SYSUTCDATETIME(), RevokedReason = 'logout' " +
                    "WHERE TokenHash = @h AND RevokedAt IS NULL",
                    new SqlParameter("@h", h));
            }
        }

        // =========================
        //  REVOKE / FORCE LOGOUT
        // =========================

        /// <summary>Unieważnia pojedynczy access token przez jego JTI (np. /auth/logout).</summary>
        public async Task RevokeTokenAsync(string jti, DateTime expiresAtUtc)
        {
            await _context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM piesp.RevokedTokens WHERE Jti = {0})
                    INSERT INTO piesp.RevokedTokens (Jti, ExpiresAt) VALUES ({0}, {1});",
                jti, expiresAtUtc);
        }

        /// <summary>
        /// Sprawdza, czy dany JTI znajduje się na blacklist i nie wygasł.
        /// (Używane w OnTokenValidated)
        /// </summary>
        public async Task<bool> IsTokenRevokedAsync(string jti)
        {
            var conn = _context.Database.GetDbConnection();
            var needClose = conn.State != ConnectionState.Open;
            if (needClose) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT TOP (1) 1
                    FROM piesp.RevokedTokens
                    WHERE Jti = @j AND ExpiresAt > SYSUTCDATETIME()";
                var p = cmd.CreateParameter();
                p.ParameterName = "@j";
                p.Value = jti;
                cmd.Parameters.Add(p);

                var scalar = await cmd.ExecuteScalarAsync();
                return scalar is not null;
            }
            finally
            {
                if (needClose) await conn.CloseAsync();
            }
        }

        /// <summary>
        /// Sprawdza, czy wersja tokenu (claim 'ver') jest starsza niż aktualna w bazie (Users.TokenVersion).
        /// (Używane w OnTokenValidated)
        /// </summary>
        public async Task<bool> IsTokenVersionStaleAsync(Guid userId, int tokenVer)
        {
            var current = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.TokenVersion)
                .FirstOrDefaultAsync();

            return current > tokenVer;
        }

        /// <summary>
        /// Force-logout (Supervisor): inkrementuje TokenVersion danego użytkownika oraz unieważnia wszystkie jego refresh tokeny.
        /// Stare access tokeny odpadają (ver &lt; Users.TokenVersion), użytkownik może od razu zalogować się ponownie.
        /// </summary>
        public async Task<bool> ForceLogoutBySamAccountNameAsync(string samAccountName)
        {
            var normalizedLogin = NormalizeSamAccountName(samAccountName);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.SamAccountName == normalizedLogin);
            if (user is null) return false;

            user.TokenVersion += 1;
            await _context.SaveChangesAsync();

            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE piesp.RefreshTokens SET RevokedAt = SYSUTCDATETIME(), RevokedReason = 'force-logout' " +
                "WHERE UserId = {0} AND RevokedAt IS NULL",
                user.Id);

            _log.LogInformation("ForceLogout: TokenVersion++ for user {UserId} (samAccountName={SamAccountName}) -> {Ver}", user.Id, normalizedLogin, user.TokenVersion);
            return true;
        }

        /// <summary>
        /// Wyloguj ze wszystkich urządzeń (dla samego użytkownika): TokenVersion++, revoke all refresh.
        /// </summary>
        public async Task LogoutAllForUserAsync(Guid userId)
        {
            await _context.Database.ExecuteSqlRawAsync("UPDATE piesp.Users SET TokenVersion = TokenVersion + 1 WHERE Id = {0}", userId);
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE piesp.RefreshTokens SET RevokedAt = SYSUTCDATETIME(), RevokedReason = 'logout-all' " +
                "WHERE UserId = {0} AND RevokedAt IS NULL",
                userId);
        }

        private static string NormalizeSamAccountName(string samAccountName) =>
            samAccountName.Trim().ToLowerInvariant();
    }
}
