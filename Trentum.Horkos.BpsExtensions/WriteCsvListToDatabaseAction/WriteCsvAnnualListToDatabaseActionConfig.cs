using Trentum.Horkos.BpsExtensions.Common;
using WebCon.WorkFlow.SDK.Common;
using WebCon.WorkFlow.SDK.ConfigAttributes;

namespace Trentum.Horkos.BpsExtensions
{
    public class WriteCsvAnnualListToDatabaseActionConfig : PluginConfiguration
    {

        [ConfigGroupBox(DisplayName = "Parametry podstawowe", Order = 1)]
        public CommonParams CommonParamsGroupBox { get; set; }

        [ConfigGroupBox(DisplayName = "Parametry listy", Order = 2)]
        public AnnualListParams ListParamsGroupBox { get; set; }


    }
}