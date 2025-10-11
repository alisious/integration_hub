namespace IntegrationHub.Common.Config
{
    /// <summary>
    /// Konfiguracja dostępu do systemu SRP.
    /// </summary>
    public class ExternalServiceConfigBase
    {
        public int? HttpMaxConnectionsPerServer { get; set; } = 16;
        public string ServiceName { get; set; } = default!;
        public string EndpointUrl { get; set; } = default!;
        public string ClientCertificateThumbprint { get; set; } = default!;
        public int TimeoutSeconds { get; set; } = 30;
        public SourceMode SourceMode { get; set; } = SourceMode.Production;
        public bool TestMode { get { return SourceMode == SourceMode.Test; } }
        public bool TrustServerCerificate { get; set; } = true;
        

    }

    public enum SourceMode 
    {
        Production,
        Test,
        Development
    }
}
