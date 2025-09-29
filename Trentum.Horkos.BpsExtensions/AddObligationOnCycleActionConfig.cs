using WebCon.WorkFlow.SDK.Common;
using WebCon.WorkFlow.SDK.ConfigAttributes;

namespace Trentum.Horkos.BpsExtensions
{
    public class AddObligationOnCycleActionConfig : PluginConfiguration
    {
        [ConfigEditableText("Sql query", Description = @"Sql zwracający dwie kolumny: WfdId z id akt osoby i pesel osoby.", Multiline = true, TagEvaluationMode = EvaluationMode.SQL)]
        public string SqlQuery { get; set; }
    }
}