using System;
using System.Collections.Generic;
using System.Text;
using WebCon.WorkFlow.SDK.Common;
using WebCon.WorkFlow.SDK.ConfigAttributes;

namespace Trentum.Horkos.BpsExtensions
{
    
    public class WriteCsvListToDatabaseConfig : PluginConfiguration
    {
        [ConfigGroupBox(DisplayName = "Parametry podstawowe", Order = 1)]
        public CommonParams CommonParamsGroupBox { get; set; }
       
    }

    
    public class CommonParams
    {
        
        [ConfigEditableConnectionID(DisplayName = "HORKOS_DB", Description = "Połączenie do bazy danych HORKOS_DB, gdzie znajduje się tabela ZobowiazaniaRoczne.", ConnectionsType = DataConnectionType.MSSQL, IsRequired = true)]
        public int HorkosDbConnectionId { get; set; }
        [ConfigEditableFormFieldID(DisplayName = "Id listy zobowiązanych.", Description = "Atrybut zawierający identyfikator importowanej listy zobowiązanych. Jeżeli nie jest podany ID listy zobowiązanych jest wartość Id elementu, w którego kontekście wykonywana jest akcja.", FormFieldTypes = FormFieldTypes.Integer, IsRequired = false)]
        public int? ObligatedListIdFormFieldId { get; set; }
        
        [ConfigEditableFormFieldID(DisplayName = "Wynik zapisu do bazy danych.", Description = "Pole z informacją o liczbie pozycji listy zapisanych do tabeli ZobowiazaniaRoczne.", IsRequired = false)]
        public int? InsertedRowsCountFormFieldID { get; set; }
    }

    public class AnnualListParams 
    {
        [ConfigEditableFormFieldID(DisplayName = "Rok zobowiązania.", Description = "Atrybut zawierający rok, którego dotyczy lista zobowiązanych. ", FormFieldTypes = FormFieldTypes.Integer, IsRequired = true)]
        public int YearFormFieldId { get; set; }
    }

    public class DischargedListParams : AnnualListParams
    {
        [ConfigEditableFormFieldID(DisplayName = "Miesiąc zwolnienia.", Description = "Atrybut zawierający miesiąc zwolnienia ze służby wojskowej. ", FormFieldTypes = FormFieldTypes.Integer, IsRequired = true)]
        public int MonthFormFieldId { get; set; }
    }

}
