// IntegrationHub.Sources.ANPRS/Extensions/ServiceCollectionExtensions.cs
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using IntegrationHub.Sources.ANPRS.Client;
using IntegrationHub.Sources.ANPRS.Config;
using IntegrationHub.Sources.ANPRS.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IntegrationHub.Common.Interfaces; // IClientCertificateProvider

namespace IntegrationHub.Sources.ANPRS.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddANPRS(this IServiceCollection services, IConfiguration cfg)
        {
            var section = cfg.GetSection("ExternalServices:ANPRS");
            var config = section.Get<ANPRSConfig>() ?? new ANPRSConfig();
            services.AddSingleton(config);

            services.AddHttpClient<ANPRSHttpClient>((sp, http) =>
            {
                http.BaseAddress = new Uri(config.EndpointUrl.TrimEnd('/'));
                if (config.TimeoutSeconds is > 0)
                    http.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
                // Inne stałe nagłówki (User-Agent itp.) – jeśli potrzebujesz, dodaj tutaj.
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var certProvider = sp.GetRequiredService<IClientCertificateProvider>();
                try
                {
                    var clientCert = certProvider.GetClientCertificate(config); // <-- jak w SRP

                    var handler = new SocketsHttpHandler
                    {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                        EnableMultipleHttp2Connections = true,
                        MaxConnectionsPerServer = config.HttpMaxConnectionsPerServer.GetValueOrDefault() > 0
                            ? config.HttpMaxConnectionsPerServer.GetValueOrDefault()
                            : int.MaxValue,
                        SslOptions = new SslClientAuthenticationOptions
                        {
                            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                            ClientCertificates = new X509CertificateCollection { clientCert },
                            RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                                config.TrustServerCertificate || errors == SslPolicyErrors.None
                        }
                    };

                    return handler;
                }
                catch (Exception ex)
                {
                    throw;
                }
            });

            // Serwisy domenowe (pozostają bez zmian)
            services.AddScoped<IANPRSReportsService, ANPRSReportsService>();
            services.AddScoped<IANPRSSourceService, ANPRSSourceService>();
            services.AddScoped<IANPRSDictionaryService, ANPRSDictionaryService>();

            return services;
        }
    }
}
