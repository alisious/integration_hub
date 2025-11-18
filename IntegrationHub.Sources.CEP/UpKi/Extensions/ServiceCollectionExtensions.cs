// IntegrationHub.Sources.CEP.UpKi/Extensions/ServiceCollectionExtensions.cs
using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using IntegrationHub.Common.Config;
using IntegrationHub.Common.Interfaces; // IClientCertificateProvider
using IntegrationHub.Infrastructure.Audit.Http;
using IntegrationHub.Sources.CEP.Config;
using IntegrationHub.Sources.CEP.UpKi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IntegrationHub.Sources.CEP.UpKi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Rejestracja konfiguracji i serwisów CEPiK UpKi.
        /// Sekcja: "ExternalServices:CEPiK".
        /// </summary>
        public static IServiceCollection AddCEPiKUpKi(this IServiceCollection services,
            IConfiguration configuration,
            out string logMessage)
        {
            var section = configuration.GetSection("ExternalServices:CEPiK");
            var config = section.Get<CEPiKConfig>() ?? new CEPiKConfig();
            services.AddSingleton(config);

            switch (config.SourceMode)
            {
                case SourceMode.Test:
                    logMessage = "CEPiK.UpKi działa w trybie testowym (plikowe dane testowe).";
                    services.AddScoped<IUpKiService, UpKiServiceTest>();
                    break;

                case SourceMode.Development:
                    logMessage = "CEPiK.UpKi działa w trybie deweloperskim bez połączenia ze źródłem.";
                    services.AddScoped<IUpKiService, UpKiServiceTest>();
                    break;

                default:
                case SourceMode.Production:
                    logMessage = "CEPiK.UpKi działa w trybie produkcyjnym.";

                    // HttpClient do TRU/UprawnieniaKierowcow – bazuje na DriverDocumentsServiceUrl
                    services
                        .AddHttpClient("CEPiK.UpKiClient", (sp, http) =>
                        {
                            var logger = sp.GetRequiredService<ILoggerFactory>()
                                           .CreateLogger("CEPiK.UpKiClient");

                            var baseUrl = config.DriverDocumentsServiceUrl;
                            if (!string.IsNullOrWhiteSpace(baseUrl))
                            {
                                http.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
                            }

                            if (config.TimeoutSeconds is > 0)
                            {
                                http.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
                            }

                            // „nowocześnie”, jak w SRP – HTTP/2 z fallbackiem
                            http.DefaultRequestVersion = HttpVersion.Version20;
                            http.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

                            logger.LogInformation("Skonfigurowano HttpClient 'CEPiK.UpKiClient' z bazowym adresem {BaseAddress}",
                                http.BaseAddress);
                        })
                        // Audyt wywołań – jak w SRP
                        .AddHttpMessageHandler(sp =>
                        {
                            var factory = sp.GetRequiredService<Func<string, SourceAuditHandler>>();
                            var sourceName = string.IsNullOrWhiteSpace(config.ServiceName)
                                ? "CEPiK"
                                : config.ServiceName;
                            return factory(sourceName);
                        })
                        // Handler gniazd + MTLS + opcjonalne zaufanie do certyfikatu serwera
                        .ConfigurePrimaryHttpMessageHandler(sp =>
                        {
                            var logger = sp.GetRequiredService<ILoggerFactory>()
                                           .CreateLogger("CEPiK.UpKiHandler");
                            var certProvider = sp.GetRequiredService<IClientCertificateProvider>();

                            var handler = new SocketsHttpHandler
                            {
                                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                                EnableMultipleHttp2Connections = true,
                                MaxConnectionsPerServer = config.HttpMaxConnectionsPerServer.GetValueOrDefault() > 0
                                    ? config.HttpMaxConnectionsPerServer.GetValueOrDefault()
                                    : int.MaxValue
                            };

                            try
                            {
                                var clientCert = certProvider.GetClientCertificate(config);

                                handler.SslOptions = new SslClientAuthenticationOptions
                                {
                                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                                    ClientCertificates = new X509CertificateCollection { clientCert },
                                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                                        config.TrustServerCertificate || errors == SslPolicyErrors.None
                                };

                                logger.LogInformation(
                                    "Załadowano certyfikat klienta dla CEPIK/UpKi. Thumbprint={Thumbprint}",
                                    config.ClientCertificateThumbprint);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex,
                                    "Błąd podczas ładowania certyfikatu klienta dla CEPIK/UpKi. Thumbprint={Thumbprint}",
                                    config.ClientCertificateThumbprint);

                                // Fallback – bez certyfikatu klienta, tylko opcjonalne zaufanie do serwera
                                handler.SslOptions = new SslClientAuthenticationOptions
                                {
                                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                                    RemoteCertificateValidationCallback = config.TrustServerCertificate
                                        ? new RemoteCertificateValidationCallback((_, _, _, _) => true)
                                        : null
                                };
                            }

                            return handler;
                        });

                    services.AddScoped<IUpKiService, UpKiService>();
                    break;
            }

            return services;
        }
    }
}
