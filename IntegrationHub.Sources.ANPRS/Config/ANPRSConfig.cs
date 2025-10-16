using IntegrationHub.Common.Config;
namespace IntegrationHub.Sources.ANPRS.Config
{
    public class ANPRSConfig : ExternalServiceConfigBase
    {
        /// <summary>
        /// Token do usługi ANPRS w formacie Base64
        /// </summary>
        public string ANPRSTokenBase64 { get; set; } = string.Empty;
    }
}
