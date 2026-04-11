using WebCon.WorkFlow.SDK.Common;
using WebCon.WorkFlow.SDK.ConfigAttributes;

namespace Trentum.Horkos.BpsExtensions
{
    public class StartPersonalFileActionConfig : PluginConfiguration
    {

        [ConfigGroupBox(DisplayName = "Dane źródłowe.", Order = 1)]
        public SourceParams SourceParamsGroupBox { get; set; }

        [ConfigGroupBox(DisplayName = "Obieg akt osoby", Order = 2)]
        public DestinationParams DestinationParamsGroupBox { get; set; }


    }

    public class SourceParams
    {
        [ConfigEditableText(DisplayName = "ID elementu - dokument zawierający listę zobowiązanych.", Order = 1)]
        public int SourceListElementId { get; set; }
        [ConfigEditableText("Sql query", Description = @"Sql zwracający dwie kolumny: WfdId z id akt osoby i pesel osoby.", Multiline = true, TagEvaluationMode = EvaluationMode.SQL)]
        public string SqlQuery { get; set; }

        [ConfigEditableItemList(DisplayName = "Kolumna z ID akt osoby", Order = 2)]
        public ListOfObligation ObligationList { get; set; }

    }

    public class DestinationParams
    {
        [ConfigEditableInteger(DisplayName = "ID procesu akt osoby.", Order = 1)]
        public int PersonFilesProcessId { get; set; }
        [ConfigEditableInteger(DisplayName = "ID obiegu akt osoby.", Order = 2)]
        public int PersonFilesWorkflowId { get; set; }
        [ConfigEditableInteger(DisplayName = "ID typu dokumentu akt osoby.", Order = 3)]
        public int PersonFilesFormTypeId { get; set; }

    }

    public class ListOfObligation : IConfigEditableItemList
    {
        public int ItemListId { get; set;}
    }
}