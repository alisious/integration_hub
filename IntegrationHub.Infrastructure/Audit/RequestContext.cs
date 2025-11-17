using System;

namespace IntegrationHub.Infrastructure.Audit;

/// <summary>
/// Prosta implementacja kontekstu żądania.
/// Rejestrowana jako Scoped – jedna instancja na żądanie HTTP.
/// </summary>
public sealed class RequestContext : IRequestContext
{
    public string? RequestId { get; set; }

    public string? CorrelationId { get; set; }

    public string? UserId { get; set; }

    public string? UserDisplayName { get; set; }

    public string? UserUnitName { get; set; }
}
