using IntegrationHub.Common.Config;

namespace IntegrationHub.Sources.CEP.Config
{
    public class CEPConfig : ExternalServiceConfigBase
    {
        public string DictionaryShareServiceUrl { get; set; } = string.Empty;
        public string ShareServiceUrl { get; set; } = string.Empty;
    }
}
