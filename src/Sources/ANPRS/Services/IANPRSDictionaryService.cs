using System;
using System.Threading;
using System.Threading.Tasks;
using IntegrationHub.Sources.ANPRS.Client;
using IntegrationHub.Sources.ANPRS.Config;
using IntegrationHub.Sources.ANPRS.Contracts;

namespace IntegrationHub.Sources.ANPRS.Services
{
    public interface IANPRSDictionaryService
    {
        /// <summary>/Dictionary?type=bcp</summary>
        Task<BCPResponse?> GetBCPAsync(CancellationToken ct = default);

        /// <summary>/Dictionary?type=countries</summary>
        Task<CountriesResponse?> GetCountriesAsync(CancellationToken ct = default);

        /// <summary>/Dictionary/Systems?country=PLN</summary>
        Task<SystemsResponse?> GetSystemsAsync(string country, CancellationToken ct = default);

        /// <summary>/Dictionary/BCP?country=PLN&system=OCR</summary>
        Task<BCPResponse?> GetBCPByCountrySystemAsync(string country, string system, CancellationToken ct = default);
    
    }
}