using IntegrationHub.Common.Config;
using IntegrationHub.Sources.ZW.Config;
using IntegrationHub.Sources.ZW.Interfaces;
using IntegrationHub.Sources.ZW.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationHub.Sources.ZW.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddZWSource(this IServiceCollection services, IConfiguration config, out string logMessage)
    {
        services.AddOptions<ZandConfig>()
                    .Bind(config.GetSection("ExternalServices:ZW")) // np. "Sources:ZW"
                    .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString),
                    "Sources:ZW:ConnectionString is required.")
                    .ValidateOnStart(); // opcjonalnie: fail-fast przy starcie

             
        services.AddScoped<IZWSourceService, ZWSourceService>();

        var serviceMode = config.GetValue<SourceMode?>("ExternalServices:ZW:SourceMode") ?? SourceMode.Production;
        switch (serviceMode)
        {
            case SourceMode.Test:
                logMessage = "ZW działa w trybie testowym.";
                services.AddScoped<IZWSourceService, ZWSourceService>();
                break;

            case SourceMode.Development:
                logMessage = "ZW działa w trybie deweloperskim bez połączenia ze źródłem.";
                services.AddScoped<IZWSourceService, ZWSourceService>();
                break;

            default:
                logMessage = "ZW działa w trybie produkcyjnym.";
                services.AddScoped<IZWSourceService, ZWSourceService>();
                break;
        }
    
        return services;

    }
}
