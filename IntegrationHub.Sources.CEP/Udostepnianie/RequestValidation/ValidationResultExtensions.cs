// IntegrationHub.Sources.CEP.Udostepnianie.Validation/ValidationResultExtensions.cs
using System.Net;
using IntegrationHub.Common.Contracts; // ProxyResponse, ProxyStatus

namespace IntegrationHub.Sources.CEP.Udostepnianie.RequestValidation
{
    public static class ValidationResultExtensions
    {
        private const string SourceName = "CEP";

        public static ProxyResponse<object> ToProxyResponse(this ValidationResult vr, string requestId)
        {
            // Zakładamy wywołanie tylko dla błędów.
            return new ProxyResponse<object>
            {
                RequestId = requestId,
                Source = SourceName,
                Status = ProxyStatus.BusinessError,
                SourceStatusCode = (int)HttpStatusCode.BadRequest,
                ErrorMessage = vr.MessageError ?? "Błąd walidacji żądania."
            };
        }
    }
}
