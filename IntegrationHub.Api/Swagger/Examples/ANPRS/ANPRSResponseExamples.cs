using IntegrationHub.Common.Contracts;
using Swashbuckle.AspNetCore.Filters;

namespace IntegrationHub.Api.Swagger.Examples.ANPRS
{
    // 401 – brak nagłówka z danymi logowania
    public sealed class Code401Example : IExamplesProvider<ProxyResponse<object>>
    {
        public ProxyResponse<object> GetExamples() => new()
        {
            Status = ProxyStatus.BusinessError,
            ErrorMessage = "Client Header Authentication Credential Required",
            Source = "ANPRS",
            SourceStatusCode = "401",
            RequestId = "example-request-id"
        };
    }

    // 401 – nieprawidłowe/niekompletne dane logowania
    public sealed class Code401ValidAuthRequiredExample : IExamplesProvider<ProxyResponse<object>>
    {
        public ProxyResponse<object> GetExamples() => new()
        {
            Status = ProxyStatus.BusinessError,
            ErrorMessage = "Valid authentication data is required",
            Source = "ANPRS",
            SourceStatusCode = "401",
            RequestId = "example-request-id"
        };
    }

    // 403 – wymagany HTTPS
    public sealed class Code403HttpsRequiredExample : IExamplesProvider<ProxyResponse<object>>
    {
        public ProxyResponse<object> GetExamples() => new()
        {
            Status = ProxyStatus.BusinessError,
            ErrorMessage = "HTTPS Required",
            Source = "ANPRS",
            SourceStatusCode = "403",
            RequestId = "example-request-id"
        };
    }

    // 403 – wymagany certyfikat klienta
    public sealed class Code403ClientCertRequiredExample : IExamplesProvider<ProxyResponse<object>>
    {
        public ProxyResponse<object> GetExamples() => new()
        {
            Status = ProxyStatus.BusinessError,
            ErrorMessage = "Client Certificate Required",
            Source = "ANPRS",
            SourceStatusCode = "403",
            RequestId = "example-request-id"
        };
    }

    // 403 – nieważny certyfikat klienta
    public sealed class Code403ClientCertInvalidExample : IExamplesProvider<ProxyResponse<object>>
    {
        public ProxyResponse<object> GetExamples() => new()
        {
            Status = ProxyStatus.BusinessError,
            ErrorMessage = "Client Certificate is not valid",
            Source = "ANPRS",
            SourceStatusCode = "403",
            RequestId = "example-request-id"
        };
    }

    // 400 – zły parametr zapytania
    public sealed class Code400InvalidParameterExample : IExamplesProvider<ProxyResponse<object>>
    {
        public ProxyResponse<object> GetExamples() => new()
        {
            Status = ProxyStatus.BusinessError,
            ErrorMessage = "Niepoprawny parametr zapytania (musi być podany)!",
            Source = "ANPRS",
            SourceStatusCode = "400",
            RequestId = "example-request-id"
        };
    }

    // 404 – zasób/endpoint nie istnieje
    public sealed class Code404NotFoundExample : IExamplesProvider<ProxyResponse<object>>
    {
        public ProxyResponse<object> GetExamples() => new()
        {
            Status = ProxyStatus.BusinessError,
            ErrorMessage = "Nie znaleziono elementów spełniających kryteria zapytania.",
            Source = "ANPRS",
            SourceStatusCode = "404",
            RequestId = "example-request-id"
        };
    }
}
