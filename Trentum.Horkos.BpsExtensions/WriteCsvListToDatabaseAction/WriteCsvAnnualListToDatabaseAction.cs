using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using WebCon.WorkFlow.SDK.ActionPlugins;
using WebCon.WorkFlow.SDK.ActionPlugins.Model;
using WebCon.WorkFlow.SDK.Tools.Data;
using WebCon.WorkFlow.SDK.Tools.Data.Model;
using WebCon.WorkFlow.SDK.Tools.Log;

namespace Trentum.Horkos.BpsExtensions
{
    public class WriteCsvAnnualListToDatabaseAction : CustomAction<WriteCsvAnnualListToDatabaseActionConfig>
    {
        public override async Task RunAsync(RunCustomActionParams args)
        {
            var _writeActionParams = new WriteActionParameters
            {
                RodzajZobowiazania = RodzajZobowiazania.Roczne,
                HorkosDbConnectionId = Configuration.CommonParamsGroupBox.HorkosDbConnectionId,
                ObligatedListIdFormFieldId = Configuration.CommonParamsGroupBox.ObligatedListIdFormFieldId,
                InsertedRowsCountFormFieldID = Configuration.CommonParamsGroupBox.InsertedRowsCountFormFieldID,
                YearFormFieldId = Configuration.ListParamsGroupBox.YearFormFieldId
            };

            await WriteCsvAnnualListToDatabaseHelper.WriteCsvListToDatabaseAsync(args, _writeActionParams);
        }
    }
}