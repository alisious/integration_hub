// IntegrationHub.Sources.CEP.Udostepnianie.Contracts/PytanieOListeCzynnosciPojazduRequest.cs
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    public sealed class PytanieOListeCzynnosciPojazduRequest
    {
        [JsonPropertyName("identyfikatorSystemowyPojazdu")]
        public string? IdentyfikatorSystemowyPojazdu { get; set; }
    }
}
