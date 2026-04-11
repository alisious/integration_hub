using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace IntegrationHub.Api.Controllers;

/// <summary>
/// Endpoint zwracający wersję aplikacji (bez autoryzacji).
/// </summary>
[ApiController]
[Route("version")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class VersionController : ControllerBase
{
    /// <summary>
    /// Zwraca wersję API (numer + hash commita z assembly).
    /// </summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Pobierz wersję API", Description = "Zwraca wersję aplikacji i hash commita. Endpoint publiczny.")]
    [ProducesResponseType(typeof(VersionResponse), StatusCodes.Status200OK)]
    public VersionResponse Get()
    {
        var assembly = Assembly.GetEntryAssembly();
        var info = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var informationalVersion = info?.InformationalVersion ?? "unknown";

        // Wersja bez sufiksu (np. "1.1.1" z "1.1.1+sha.abc123")
        var version = informationalVersion.Split('+')[0].Trim();

        return new VersionResponse
        {
            Version = version,
            InformationalVersion = informationalVersion,
            Product = assembly?.GetName().Name ?? "IntegrationHub.Api"
        };
    }
}

/// <summary>DTO odpowiedzi z wersją API.</summary>
public sealed record VersionResponse
{
    /// <summary>Numer wersji (np. 1.1.1).</summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>Pełna wersja z hashem commita.</summary>
    public string InformationalVersion { get; init; } = string.Empty;

    /// <summary>Nazwa produktu.</summary>
    public string Product { get; init; } = string.Empty;
}
