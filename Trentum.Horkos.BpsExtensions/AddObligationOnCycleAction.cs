using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebCon.WorkFlow.SDK.ActionPlugins;
using WebCon.WorkFlow.SDK.ActionPlugins.Model;
using WebCon.WorkFlow.SDK.Documents;
using WebCon.WorkFlow.SDK.Documents.Model;
using WebCon.WorkFlow.SDK.Documents.Model.ItemLists;

namespace Trentum.Horkos.BpsExtensions
{
    public class AddObligationOnCycleAction : CustomAction<AddObligationOnCycleActionConfig>
    {
        public override ActionTriggers AvailableActionTriggers => ActionTriggers.Recurrent;
        StringBuilder _logger = new StringBuilder();
        public override async Task RunAsync(RunCustomActionParams args)
        {

        }

        private async Task UpdateDocumentsAsync(List<UpdateData> updateData, ActionWithoutDocumentContext context)
        {
            _logger.AppendLine("Updating documents");
            var documentsManager = new DocumentsManager(context);
            foreach (var data in updateData)
            {
                var doc = await documentsManager.GetDocumentByIdAsync(data.DocumentId, true);
                await doc.SetFieldValueAsync(Configuration.ObligationListIDFieldId, data.Value);
                var hasObligationList = doc.ItemsLists.TryGetByID(Configuration.ObligationListId, out ItemsList obligationList);
                if (!hasObligationList)
                {
                    _logger.AppendLine($"Document {data.DocumentId} does not have obligation list with ID {Configuration.ObligationListId}");
                }
                else
                {
                    var newRow = obligationList.Rows.AddNewRowAsync().Result;
                    //newRow.SetCellValueAsync();
                    //obligationList.Rows.Add(newRow);
                    //_logger.AppendLine($"Added obligation {data.Obligation} to document {data.DocumentId}");
                }
                await documentsManager.UpdateDocumentAsync(new UpdateDocumentParams(doc) { SkipPermissionsCheck = true });
            }
        }



    }
}