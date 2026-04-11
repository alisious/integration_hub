// IntegrationHub.Sources.ANPRS/Extensions/ServiceCollectionExtensions.cs
using IntegrationHub.Common.Config;
using IntegrationHub.Common.Interfaces; // IClientCertificateProvider
using IntegrationHub.Sources.ANPRS.Client;
using IntegrationHub.Sources.ANPRS.Config;
using IntegrationHub.Sources.ANPRS.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace IntegrationHub.Sources.ANPRS.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddANPRS(this IServiceCollection services, IConfiguration cfg,out string logMessage)
        {
            var section = cfg.GetSection("ExternalServices:ANPRS");
            var config = section.Get<ANPRSConfig>() ?? new ANPRSConfig();
            services.AddSingleton(config);


            // Log trybu pracy ANPRS (analogicznie jak dla KSIP)
            switch (config?.SourceMode)
            {
                case SourceMode.Test:
                    logMessage = "ANPRS działa w trybie testowym.";
                    // Serwisy domenowe (pozostają bez zmian)
                    services.AddScoped<IANPRSReportsService, ANPRSReportsServiceTest>();
                    services.AddScoped<IANPRSSourceService, ANPRSSourceServiceTest>();
                    services.AddScoped<IANPRSDictionaryService, ANPRSDictionaryServiceTest>();
                    break;

                case SourceMode.Development:
                    logMessage = "ANPRS działa w trybie deweloperskim bez połączenia ze źródłem.";
                    // Serwisy domenowe (pozostają bez zmian)
                    services.AddScoped<IANPRSReportsService, ANPRSReportsServiceTest>();
                    services.AddScoped<IANPRSSourceService, ANPRSSourceServiceTest>();
                    services.AddScoped<IANPRSDictionaryService, ANPRSDictionaryServiceTest>();
                    break;

                default:
                case SourceMode.Production:
                    logMessage = "ANPRS działa w trybie produkcyjnym.";
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
                                            }//SslOptions
                                        };//handler
                                    return handler;
                                }
                                catch (Exception ex)
                                {
                                    throw;
                                }
                    });//ConfigurePrimaryHttpMessageHandler

                    // Serwisy domenowe (pozostają bez zmian)
                    services.AddScoped<IANPRSReportsService, ANPRSReportsService>();
                    services.AddScoped<IANPRSSourceService, ANPRSSourceService>();
                    services.AddScoped<IANPRSDictionaryService, ANPRSDictionaryService>();

                    break;
                    
            }
            
            return services;
        }
    }
}
