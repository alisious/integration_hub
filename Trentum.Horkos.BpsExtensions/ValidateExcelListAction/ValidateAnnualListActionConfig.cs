using Trentum.Horkos.BpsExtensions.Common;
using WebCon.WorkFlow.SDK.Common;
using WebCon.WorkFlow.SDK.ConfigAttributes;

namespace Trentum.Horkos.BpsExtensions
{
    public class ValidateAnnualListActionConfig : PluginConfiguration
    {
        [ConfigGroupBox(DisplayName = "Zakres walidacji listy", Order = 1)]
        public ValidationConditions ValidationConditionsGroupBox { get; set; }

        [ConfigGroupBox(DisplayName = "Źródła danych do walidacji listy", Order = 3)]
        public ValidationDataSources ValidationDataSourcesGroupBox { get; set; }

        [ConfigGroupBox(DisplayName = "Wyniki walidacji listy", Order = 2)]
        public ValidationResults ValidationResultsGroupBox { get; set; }



    }


    
}