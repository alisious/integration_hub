// IntegrationHub.Sources.CEP.Udostepnianie.Contracts/PytanieOHistorieLicznikaRequest.cs
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    public sealed class PytanieOHistorieLicznikaRequest
    {
        [JsonPropertyName("identyfikatorSystemowyPojazdu")]
        public string? IdentyfikatorSystemowyPojazdu { get; set; }
    }
}
