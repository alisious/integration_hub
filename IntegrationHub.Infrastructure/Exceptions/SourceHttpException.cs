using System;

namespace IntegrationHub.Infrastructure.Exceptions
{
    /// <summary>
    /// Bazowy wyjątek dla klientów zewnętrznych źródeł HTTP.
    /// Rzucany przy odpowiedziach nie-2xx. 
    /// </summary>
    public class SourceHttpException : Exception
    {
        public int StatusCode { get; }
        public string? ResponseBody { get; }

        public SourceHttpException(int statusCode, string? responseBody, string? message = null, Exception? inner = null)
            : base(message ?? $"HTTP {statusCode}.", inner)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
