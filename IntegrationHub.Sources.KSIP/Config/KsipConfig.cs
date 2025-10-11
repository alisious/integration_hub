using IntegrationHub.Common.Config;

namespace IntegrationHub.Sources.KSIP.Config
{
    public class KSIPConfig :ExternalServiceConfigBase
    {
        /// <summary>
        /// Adres usługi rejestracji MRD5 w trybie testowym
        /// </summary>
        public string TestMRD5RegistrationServiceUrl { get; set; } = String.Empty;
        /// <summary>
        /// Adres usługi rejestracji MRD5 w trybie produkcyjnym
        /// </summary>    
        public string MRD5RegistrationServiceUrl { get; set; } = String.Empty;
        /// <summary>
        /// Adres usługi sprawdzenia osoby RD w trybie testowym
        /// </summary>
        public string TestSprawdzenieOsobyRDServiceUrl { get; set; } = String.Empty;
        /// <summary>
        /// Adres usługi sprawdzenia osoby RD w trybie produkcyjnym
        /// </summary>
        public string SprawdzenieOsobyRDServiceUrl { get; set; } = String.Empty;
        /// <summary>   
        /// Identyfikator Żandarmerii Wojskowej w KSIP
        public string UnitId { get; set; } = String.Empty;
    }
}
