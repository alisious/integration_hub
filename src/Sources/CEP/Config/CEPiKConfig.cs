using IntegrationHub.Common.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.Sources.CEP.Config
{
    public class CEPiKConfig : ExternalServiceConfigBase
    {
        public string DictionaryShareServiceUrl { get; set; } = string.Empty;
        public string ShareServiceUrl { get; set; } = string.Empty;
        public string DriverDocumentsServiceUrl { get; set; } = string.Empty;
        
    }
}
