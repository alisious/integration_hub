// w IntegrationHub.Common.Contracts (ok, bo Contracts może referencjonować Primitives)
using IntegrationHub.Common.Primitives;

namespace IntegrationHub.Common.Contracts;

/// <summary>Mapuje Result na ProxyResponse z przekazaniem source i requestId.</summary>
public static class ProxyResponseMapper
{
    /// <summary>Konwertuje Result na ProxyResponse. Caller powinien ustawić Source i RequestId.</summary>
    public static ProxyResponse<T> ToProxyResponse<T>(this Result<T, Error> result)
        => result.ToProxyResponse("", "");

    /// <summary>Konwertuje Result na ProxyResponse z uzupełnieniem Source, RequestId i poprawnym Status.</summary>
    public static ProxyResponse<T> ToProxyResponse<T>(this Result<T, Error> result, string source, string requestId)
        => result.Match(
            onSuccess: v => new ProxyResponse<T>
            {
                Data = v,
                Status = ProxyStatus.Success,
                Source = source,
                SourceStatusCode = "200",
                RequestId = requestId
            },
            onError: e => new ProxyResponse<T>
            {
                Data = default,
                Status = e.ErrorKind == ErrorKindEnum.Business ? ProxyStatus.BusinessError : ProxyStatus.TechnicalError,
                Message = e.Message,
                Source = source,
                SourceStatusCode = e.HttpStatus?.ToString(),
                RequestId = requestId
            });
}
