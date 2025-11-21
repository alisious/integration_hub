// IntegrationHub.Sources.KSIP/Extensions/ServiceCollectionExtensions.cs
using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using IntegrationHub.Common.Config;
using IntegrationHub.Common.Interfaces; // IClientCertificateProvider
using IntegrationHub.Infrastructure.Audit.Http;
using IntegrationHub.Sources.KSIP.Config;
using IntegrationHub.Sources.KSIP.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IntegrationHub.Sources.KSIP.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Rejestracja konfiguracji i serwisów KSIP.
        /// Sekcja konfiguracji: "ExternalServices:KSIP".
        /// </summary>
        public static IServiceCollection AddKSIP(
            this IServiceCollection services,
            IConfiguration configuration,
            out string logMessage)
        {
            var section = configuration.GetSection("ExternalServices:KSIP");
            var config = section.Get<KSIPConfig>() ?? new KSIPConfig();
            services.AddSingleton(config);

            // Tryb DEV – całkowicie przełączamy się na KSIPServiceTest (pliki XML, bez HttpClient).
            if (config.SourceMode == SourceMode.Development)
            {
                logMessage = "KSIP działa w trybie DEV – wykorzystywany jest KSIPServiceTest (odpowiedzi z plików XML w katalogu KSIP).";

                services.AddScoped<IKSIPService, KSIPServiceTest>();
                return services;
            }

            // Poniżej konfiguracja dla Test / Production – jak w wersji produkcyjnej.
            string baseUrl;
            string clientCertThumbprint;
            //var cp = services.BuildServiceProvider().GetRequiredService<IClientCertificateProvider>();

            switch (config.SourceMode)
            {
                case SourceMode.Test:
                    baseUrl = config.TestSprawdzenieOsobyRDServiceUrl;
                    clientCertThumbprint = config.TestClientCertificateThumbprint;
                    logMessage = "KSIP działa w trybie testowym.";
                    // Próba pobrania certyfikatu testowego
                    //var cert = cp.GetClientCertificate(config, clientCertThumbprint);
                    //if (cert != null)
                    //{
                    //    logMessage += $" Załadowano certyfikat klienta dla KSIP w trybie TEST. Thumbprint: {cert.Thumbprint}";
                    //}
                    //else
                    //{
                    //    logMessage += " Nie udało się załadować certyfikatu klienta w trybie TEST.";
                    //}


                    break;

                default:
                case SourceMode.Production:
                    baseUrl = config.SprawdzenieOsobyRDServiceUrl;
                    clientCertThumbprint = config.ClientCertificateThumbprint;
                    logMessage = "KSIP działa w trybie produkcyjnym.";
                    // Próba pobrania certyfikatu produkcyjnego
                    //var prodCert = cp.GetClientCertificate(config, clientCertThumbprint);
                    //if (prodCert != null)
                    //{
                    //    logMessage += $" Załadowano certyfikat klienta dla KSIP w trybie PROD. Thumbprint: {prodCert.Thumbprint}";
                    //}
                    //else
                    //{
                    //    logMessage += " Nie udało się załadować certyfikatu klienta w trybie PROD.";
                    //}
                    break;
            }



            // HttpClient do usługi KSIP SprawdzenieOsobyWRD
            services
                .AddHttpClient("KSIP.SprawdzenieOsobyClient", (sp, http) =>
                {
                    var logger = sp.GetRequiredService<ILoggerFactory>()
                                   .CreateLogger("KSIP.SprawdzenieOsobyClient");

                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        http.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
                    }

                    if (config.TimeoutSeconds is > 0)
                    {
                        http.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
                    }

                    // HTTP/2 z fallbackiem – jak w SRP/UpKi
                    http.DefaultRequestVersion = HttpVersion.Version20;
                    http.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

                    logger.LogInformation(
                        "Skonfigurowano HttpClient 'KSIP.SprawdzenieOsobyClient' z bazowym adresem {BaseAddress} i timeoutem {TimeoutSeconds}s",
                        http.BaseAddress,
                        config.TimeoutSeconds);
                })
                // Audyt wywołań HTTP do KSIP – wspólny handler
                .AddHttpMessageHandler(sp =>
                {
                    var factory = sp.GetRequiredService<Func<string, SourceAuditHandler>>();
                    var sourceName = string.IsNullOrWhiteSpace(config.ServiceName)
                        ? "KSIP"
                        : config.ServiceName;
                    return factory(sourceName);
                })
                // Handler gniazd + MTLS + opcjonalne zaufanie do certyfikatu serwera
                .ConfigurePrimaryHttpMessageHandler(sp =>
                {
                    var logger = sp.GetRequiredService<ILoggerFactory>()
                                   .CreateLogger("KSIP.SprawdzenieOsobyHandler");
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

                        var clientCert = certProvider.GetClientCertificate(config, clientCertThumbprint);


                        handler.SslOptions = new SslClientAuthenticationOptions
                        {
                            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                            ClientCertificates = new X509CertificateCollection { clientCert },
                            RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                                config.TrustServerCertificate || errors == SslPolicyErrors.None
                        };


                        if (config.SourceMode == SourceMode.Test)
                        {
                            logger.LogInformation(
                                "Załadowano certyfikat klienta dla KSIP w trybie TEST. Thumbprint: {Thumbprint}",
                                clientCert.Thumbprint);
                        }
                        else
                        {
                            logger.LogInformation(
                               "Załadowano certyfikat klienta dla KSIP w trybie PROD. Thumbprint: {Thumbprint}",
                               clientCert.Thumbprint);
                        }

                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Błąd podczas pobierania certyfikatu klienta dla KSIP. Używam konfiguracji bez certyfikatu klienta.");

                        handler.SslOptions = new SslClientAuthenticationOptions
                        {
                            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                            RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                                config.TrustServerCertificate || errors == SslPolicyErrors.None
                        };
                    }

                    return handler;
                });

            // Serwis domenowy KSIP (realny endpoint)
            services.AddScoped<IKSIPService, KSIPService>();

            return services;
        }
    }

      
}
