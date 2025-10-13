using WebCon.WorkFlow.SDK.Common;
using WebCon.WorkFlow.SDK.ConfigAttributes;

namespace Trentum.Horkos.BpsExtensions
{
    public class AddObligationOnCycleActionConfig : PluginConfiguration
    {
        [ConfigEditableText("Sql query", Description = @"Sql zwracający dwie kolumny: WfdId z id akt osoby i pesel osoby.", Multiline = true, TagEvaluationMode = EvaluationMode.SQL)]
        public string SqlQuery { get; set; }

        [ConfigEditableFormFieldID("ID listy zobowiązanych", Description = "Pole zawierające ID listy zobowiązanych")]
        public int ObligationListIDFieldId { get; set; }
        [ConfigEditableItemList("Lista zobowiązań", Description = "Lista zobowiązań")]
        public int ObligationListId { get; set; }

        [ConfigGroupBox(DisplayName = "Kolumny listy zobowiązań", Order = 2)]
        public ListaZobowiazanColumns ListaZobowiazanGroupBox { get; set; }
    }

    public class ListaZobowiazanColumns : ValidationConditions
    {
        [ConfigEditableFormFieldID(DisplayName = "Sprawdzaj datę zwolnienia.", Description = "Zaznaczenie oznacza, że procedura będzie sprawdzała, czy podano prawidłową datę zwolnienia ze służby.", FormFieldTypes = FormFieldTypes.Boolean)]
        public int? ValidateDischargedDateFormFieldId { get; set; }

    }


}