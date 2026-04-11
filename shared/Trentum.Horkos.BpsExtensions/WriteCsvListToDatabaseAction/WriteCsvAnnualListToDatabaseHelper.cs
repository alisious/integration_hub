using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using WebCon.WorkFlow.SDK.ActionPlugins.Model;
using WebCon.WorkFlow.SDK.Tools.Data;
using WebCon.WorkFlow.SDK.Tools.Data.Model;

namespace Trentum.Horkos.BpsExtensions
{
    public enum RodzajZobowiazania
    {
        Roczne = 0,
        PoZwolnieniu = 1
    }
    public static class WriteCsvAnnualListToDatabaseHelper
    {
        public async static Task<bool> WriteCsvListToDatabaseAsync(RunCustomActionParams args, WriteActionParameters writeActionParameters) 
        {
            
            var _log = new StringBuilder();
            var _horkosListId = args.Context.CurrentDocument.ID;
            var _result = true;
            try
            {

                if (writeActionParameters.ObligatedListIdFormFieldId.HasValue)
                {
                    _horkosListId = args.Context.CurrentDocument.IntegerFields.GetByID(writeActionParameters.ObligatedListIdFormFieldId.Value).Value.Value;
                }

                _log.AppendLine($"ID listy zobowiązanych: {_horkosListId}, Id dokumentu: {args.Context.CurrentDocument.ID}");


                var _year = args.Context.CurrentDocument.IntegerFields.GetByID(writeActionParameters.YearFormFieldId).Value;
                if (_year == null)
                    throw new ArgumentException("Nieprawidłowa wartość lub nie podano roku zobowiązania.");

                var _month = "00";
                if (writeActionParameters.RodzajZobowiazania == RodzajZobowiazania.PoZwolnieniu)
                {
                    var monthInt = args.Context.CurrentDocument.IntegerFields.GetByID(writeActionParameters.MonthFormFieldId).Value;
                    if (monthInt == null || monthInt < 1 || monthInt > 12)
                        throw new ArgumentException("Nieprawidłowa wartość lub nie podano miesiąca zwolnienia.");
                    _month = monthInt.Value.ToString("D2");
                }


                //Pobranie załącznika z listą zobowiązanych
                var atts = args.Context.CurrentDocument.Attachments;
                if (atts.Count == 0) throw new Exception("Nie znaleziono załącznika.");
                if (atts.Count > 1) throw new Exception("Dozwolony jest tylko 1 załącznik.");

                var attachment = atts[0];
                if (!attachment.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Załącznik nie ma prawidłowego formatu. (.csv)");

                _log.AppendLine($"Załącznik: {attachment.FileName}");

                // >>> ZAWSZE await na metodach async SDK
                var content = await attachment.GetContentAsync();
                _log.AppendLine($"Pobrano zawartość. Rozmiar: {content.Length} B");

                //Zapis do bazy
                var _insertedRowsCount = 0;
                var connectionsHelper = new ConnectionsHelper(args.Context);
                var horkosDbConnParams = new GetByConnectionParams(writeActionParameters.HorkosDbConnectionId);
                var horkosDbConnStr = connectionsHelper.GetConnectionStringToDatabase(horkosDbConnParams);

                // Wymuś brak auto-enlistu do ambient TX
                if (!horkosDbConnStr.Contains("Enlist="))
                    horkosDbConnStr += ";Enlist=false";

                _log.AppendLine($"Połączono z: {horkosDbConnStr}");

                using (var suppress = new TransactionScope(
                   TransactionScopeOption.Suppress,
                   new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                   TransactionScopeAsyncFlowOption.Enabled))
                {
                    var obligationService = new ObligationsService(new SqlConnectionFactory(horkosDbConnStr));
                    using (var inMs = new MemoryStream(content, writable: false))
                    {
                        inMs.Position = 0;
                        if (writeActionParameters.RodzajZobowiazania == RodzajZobowiazania.PoZwolnieniu)
                            _insertedRowsCount = await obligationService.ImportDischargedList(
                                                                            inMs, 
                                                                            _horkosListId, 
                                                                            _year.Value, 
                                                                            _month);
                        else
                            _insertedRowsCount = await obligationService.ImportAnnualList(
                                                                            inMs, 
                                                                            _horkosListId, 
                                                                            _year.Value);
                        _log.AppendLine($"Zapisano {_insertedRowsCount} pozycji do bazy danych.");
                        await args.Context.CurrentDocument.SetFieldValueAsync(writeActionParameters.InsertedRowsCountFormFieldID.Value, _insertedRowsCount);

                    }

                    suppress.Complete();
                } // koniec using suppress
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Błąd: {ex.Message}");
                args.Message = _log.ToString();
                args.Context.PluginLogger.AppendInfo(_log.ToString());
                _result = false;
                throw;
            }
            finally
            {
                _log.AppendLine("Koniec zapisu listy rocznej do bazy danych.");
                args.Message = _log.ToString();
                args.Context.PluginLogger.AppendInfo(_log.ToString());
            }

            return _result;
        }
    
    }

    public class WriteActionParameters
    {
        public RodzajZobowiazania RodzajZobowiazania { get; set; }
        public int HorkosDbConnectionId { get; set; }
        public int? ObligatedListIdFormFieldId { get; set; }
        public int YearFormFieldId { get; set; }
        public int MonthFormFieldId { get; set; }
        public int? InsertedRowsCountFormFieldID { get; set; }
    }
}
