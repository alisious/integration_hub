// IntegrationHub.Sources.ANPRS/Config/ANPRSConfig.cs
using IntegrationHub.Common.Config;

namespace IntegrationHub.Sources.ANPRS.Config
{
    /// <summary>Konfiguracja źródła ANPRS.</summary>
    public class ANPRSConfig : ExternalServiceConfigBase
    {
        /// <summary>Login do ANPRS (użyty do nagłówka Authentication: Bearer Base64(login:pass)).</summary>
        public string ANPRSUserID { get; set; } = string.Empty;

        /// <summary>Hasło do ANPRS (użyte do nagłówka Authentication: Bearer Base64(login:pass)).</summary>
        public string ANPRSPassword { get; set; } = string.Empty;

        /// <summary>Ścieżka dla endpointów /Service/api/Reports</summary>
        public string ReportsServiceUrl { get; set; } = "/Service/api/Reports";

        /// <summary>Ścieżka dla endpointów /Service/api/Source</summary>
        public string SourceServiceUrl { get; set; } = "/Service/api/Source";

        /// <summary>Ścieżka dla endpointów /Service/api/Dictionary</summary>
        public string DictionaryServiceUrl { get; set; } = "/Service/api/Dictionary";

        /// <summary>Ścieżka dla endpointu /Service/api/Test</summary>
        public string TestServiceUrl { get; set; } = "/Service/api/Test";
    }
}
