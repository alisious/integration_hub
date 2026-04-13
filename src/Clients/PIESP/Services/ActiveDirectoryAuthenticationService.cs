using System.DirectoryServices.Protocols;
using System.Net;
using IntegrationHub.PIESP.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntegrationHub.PIESP.Services
{
    /// <summary>
    /// Weryfikuje poświadczenia użytkownika w Active Directory przez próbę bindowania LDAP.
    /// </summary>
    public sealed class ActiveDirectoryAuthenticationService
    {
        private readonly ActiveDirectoryOptions _options;
        private readonly ILogger<ActiveDirectoryAuthenticationService> _log;

        public ActiveDirectoryAuthenticationService(
            IOptions<ActiveDirectoryOptions> options,
            ILogger<ActiveDirectoryAuthenticationService> log)
        {
            _options = options.Value;
            _log = log;
        }

        public Task<bool> ValidateCredentialsAsync(string samAccountName, string password, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(samAccountName) || string.IsNullOrWhiteSpace(password))
                return Task.FromResult(false);

            if (string.IsNullOrWhiteSpace(_options.Server))
                throw new InvalidOperationException("Missing Authentication:ActiveDirectory:Server in configuration.");

            if (string.IsNullOrWhiteSpace(_options.Domain))
                throw new InvalidOperationException("Missing Authentication:ActiveDirectory:Domain in configuration.");

            try
            {
                var identifier = new LdapDirectoryIdentifier(_options.Server, _options.Port, false, false);
                using var connection = new LdapConnection(identifier)
                {
                    AuthType = AuthType.Negotiate,
                    Credential = new NetworkCredential(samAccountName, password, _options.Domain),
                    Timeout = TimeSpan.FromSeconds(10)
                };

                connection.SessionOptions.ProtocolVersion = 3;
                connection.SessionOptions.SecureSocketLayer = _options.UseLdaps;

                if (_options.TrustServerCertificate)
                    connection.SessionOptions.VerifyServerCertificate += static (_, _) => true;

                ct.ThrowIfCancellationRequested();
                connection.Bind();
                return Task.FromResult(true);
            }
            catch (LdapException ex)
            {
                _log.LogWarning(ex, "Nieudane logowanie AD dla użytkownika {SamAccountName}.", samAccountName);
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Błąd połączenia z Active Directory podczas logowania użytkownika {SamAccountName}.", samAccountName);
                return Task.FromResult(false);
            }
        }
    }
}
