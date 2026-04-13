namespace IntegrationHub.PIESP.Config
{
    /// <summary>
    /// Konfiguracja logowania użytkowników przez Active Directory.
    /// </summary>
    public sealed class ActiveDirectoryOptions
    {
        public const string SectionName = "Authentication:ActiveDirectory";

        public string Server { get; set; } = string.Empty;

        public string Domain { get; set; } = string.Empty;

        public int Port { get; set; } = 389;

        public bool UseLdaps { get; set; }

        public bool TrustServerCertificate { get; set; }
    }
}
