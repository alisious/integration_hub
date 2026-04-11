using IntegrationHub.Common.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.Sources.ZW.Config
{
    public class ZandConfig : ExternalServiceConfigBase
    {
        public string ConnectionString { get; set; } = String.Empty;
        
    }
}
