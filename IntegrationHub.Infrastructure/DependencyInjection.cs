using IntegrationHub.Infrastructure.Cepik;
using IntegrationHub.Infrastructure.ANPRS;
using IntegrationHub.Infrastructure.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using IntegrationHub.Domain.Interfaces.ANPRS;

namespace IntegrationHub.Infrastructure.Sql;

public static class DependencyInjection
{
    /// <summary>
    /// Rejestruje fabrykę połączeń SQL i repozytoria IntegrationHub.
    /// </summary>
    public static IServiceCollection AddIntegrationHubSqlInfrastructure(
        this IServiceCollection services,
        string integrationHubDbConnectionString)
    {
        // Fabryka połączeń – singleton, bo trzyma tylko connection string.
        services.AddSingleton<IDbConnectionFactory>(
            _ => new SqlConnectionFactory(integrationHubDbConnectionString));

        // Repozytorium CEPIK słowników – per żądanie.
        services.AddScoped<ICepikDictionaryRepository, CepikDictionaryRepository>();
        // Repozytorium ANPRS – per żądanie.
        services.AddScoped<IANPRSDictionaryRepository, ANPRSDictionaryRepository>();

        return services;
    }
}
