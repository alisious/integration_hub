using System;
using System.Threading.Tasks;
using WebCon.WorkFlow.SDK.ActionPlugins;
using WebCon.WorkFlow.SDK.ActionPlugins.Model;

namespace Trentum.Horkos.BpsExtensions
{
    public class WriteCsvDischargedListToDatabaseAction : CustomAction<WriteCsvDischargedListToDatabaseActionConfig>
    {
        public override async Task RunAsync(RunCustomActionParams args)
        {
            var _writeActionParams = new WriteActionParameters
            {
                RodzajZobowiazania = RodzajZobowiazania.PoZwolnieniu,
                HorkosDbConnectionId = Configuration.CommonParamsGroupBox.HorkosDbConnectionId,
                ObligatedListIdFormFieldId = Configuration.CommonParamsGroupBox.ObligatedListIdFormFieldId,
                InsertedRowsCountFormFieldID = Configuration.CommonParamsGroupBox.InsertedRowsCountFormFieldID,
                YearFormFieldId = Configuration.ListParamsGroupBox.YearFormFieldId,
                MonthFormFieldId = Configuration.ListParamsGroupBox.MonthFormFieldId
            };

            await WriteCsvAnnualListToDatabaseHelper.WriteCsvListToDatabaseAsync(args, _writeActionParams);
        }
    }
}