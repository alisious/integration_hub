// IntegrationHub.Sources.CEP.Udostepnianie.Validation/ValidationResultExtensions.cs
using System.Net;
using IntegrationHub.Common.Contracts; // ProxyResponse, ProxyStatus

namespace IntegrationHub.Common.RequestValidation
{
    public static class ValidationResultExtensions
    {
        public static ProxyResponse<object> ToProxyResponse(this ValidationResult vr,string sourceName, string requestId)
        {
            // Zakładamy wywołanie tylko dla błędów.
            return new ProxyResponse<object>
            {
                RequestId = requestId,
                Source = sourceName,
                Status = ProxyStatus.BusinessError,
                SourceStatusCode = ((int)HttpStatusCode.BadRequest).ToString(),
                Message = vr.MessageError ?? "Błąd walidacji żądania."
            };
        }

     
        //Zwraca ProxyResponse<object> jeśli walidacja NIE jest poprawna; w przeciwnym razie null.
        public static ProxyResponse<object>? ToProxyResponseOrNull(this ValidationResult vr, string sourceName, string requestId)
            => vr.IsValid ? null : vr.ToProxyResponse(sourceName, requestId);

    }
}
