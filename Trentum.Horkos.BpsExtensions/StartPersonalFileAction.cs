using System;
using System.Text;
using System.Threading.Tasks;
using WebCon.WorkFlow.SDK.ActionPlugins;
using WebCon.WorkFlow.SDK.ActionPlugins.Model;
using WebCon.WorkFlow.SDK.Documents;
using WebCon.WorkFlow.SDK.Documents.Model;

namespace Trentum.Horkos.BpsExtensions
{
    public class StartPersonalFileAction : CustomAction<StartPersonalFileActionConfig>
    {
        public override ActionTriggers AvailableActionTriggers => ActionTriggers.Recurrent;
        StringBuilder _log = new StringBuilder();

        public override Task RunWithoutDocumentContextAsync(RunCustomActionWithoutContextParams args)
        {
            try
            {
                _log.AppendLine("Start działania StartPersonalFileAction");
                _log.AppendLine($"ID elementu: {Configuration.SourceParamsGroupBox.SourceListElementId}");
            }
                        catch (Exception ex)
            {
                _log.AppendLine($"Błąd: {ex.Message}");
            }
            finally
            {
                args.LogMessage = _log.ToString();
            }
            return base.RunWithoutDocumentContextAsync(args);
        }

        public override async Task RunAsync(RunCustomActionParams args)
        {
            var ctx = args.Context;
            var docManager = new DocumentsManager(ctx);
            var workflowId = Configuration.DestinationParamsGroupBox.PersonFilesWorkflowId;
            var docTypeId = Configuration.DestinationParamsGroupBox.PersonFilesFormTypeId;

            var newDocumentParams = new GetNewDocumentParams(docTypeId, workflowId);
            newDocumentParams.CompanyID = ctx.CurrentDocument.CompanyID;
            newDocumentParams.SkipPermissionsCheck = true; // Pomija sprawdzenie uprawnień użytkownika do tworzenia dokumentu
            // 1) Utwórz NOWY dokument docelowego obiegu
            var newDoc = await docManager.GetNewDocumentAsync(newDocumentParams);

            //docManager.StartNewWorkFlowAsync(newDoc);


            //var newDocData = new NewDocumentData();



        }
    }
}