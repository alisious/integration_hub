using System.Collections.Concurrent;
using IntegrationHub.Common.Contracts;
using IntegrationHub.SRP.Contracts;

namespace IntegrationHub.SRP.Services;

public static class RdoBulkHelpers
{
    /// <summary>
    /// Wykonuje masowo GetCurrentPhotoAsync z ograniczeniem równoległości.
    /// Zwraca wyniki 1:1 z żądaniami, nie rzuca wyjątków – porażki zwraca jako ProxyResponse z błędem.
    /// </summary>
    public static async Task<IReadOnlyList<(GetCurrentPhotoRequest Request, ProxyResponse<GetCurrentPhotoResponse> Result)>>
        BulkGetCurrentPhotosAsync(
            IRdoService rdo,
            IEnumerable<GetCurrentPhotoRequest> requests,
            int maxParallel = 6,
            CancellationToken ct = default)
    {
        var bag = new ConcurrentBag<(GetCurrentPhotoRequest, ProxyResponse<GetCurrentPhotoResponse>)>();
        const string source = "SRP";

        await Parallel.ForEachAsync(requests, new ParallelOptions
        {
            MaxDegreeOfParallelism = maxParallel,
            CancellationToken = ct
        },
        async (req, token) =>
        {
            var requestId = Guid.NewGuid().ToString();
            try
            {
                var result = await rdo.GetCurrentPhotoAsync(req, requestId: requestId, token);
                bag.Add((req, result.ToProxyResponse(source, requestId)));
            }
            catch (OperationCanceledException)
            {
                bag.Add((req, new ProxyResponse<GetCurrentPhotoResponse>
                {
                    RequestId = requestId,
                    Source = source,
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = ((int)System.Net.HttpStatusCode.RequestTimeout).ToString(),
                    Message = "Przerwano (CancellationToken)."
                }));
            }
            catch (Exception ex)
            {
                bag.Add((req, new ProxyResponse<GetCurrentPhotoResponse>
                {
                    RequestId = requestId,
                    Source = source,
                    Status = ProxyStatus.TechnicalError,
                    SourceStatusCode = ((int)System.Net.HttpStatusCode.InternalServerError).ToString(),
                    Message = ex.Message
                }));
            }
        });

        return bag.ToList();
    }
}

