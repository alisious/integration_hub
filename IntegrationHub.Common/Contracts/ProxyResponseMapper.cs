// w IntegrationHub.Common.Contracts (ok, bo Contracts może referencjonować Primitives)
using IntegrationHub.Common.Primitives;

namespace IntegrationHub.Common.Contracts;
public static class ProxyResponseMapper
{
    public static ProxyResponse<T> ToProxyResponse<T>(this Result<T, Error> result)
        => result.Match(
            onSuccess: v => new ProxyResponse<T>
            {
                Data = v,
                Status = 0,
                Message = $"OK"
            },
            onError: e => new ProxyResponse<T>
            {
                Data = default,
                Status = ProxyStatus.TechnicalError,
                Message = e.Message,
                Source = e.Code,               // opcjonalnie
                SourceStatusCode = e.HttpStatus?.ToString()
            });
}
