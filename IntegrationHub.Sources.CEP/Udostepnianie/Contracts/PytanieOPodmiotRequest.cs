// IntegrationHub.Sources.CEP.Udostepnianie.Contracts/PytanieOPodmiotRequest.cs
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    /// <summary>
    /// Żądanie „pytanie o podmiot”.
    /// Minimalne kryterium: identyfikatorSystemowyPodmiotu (trimowany).
    /// </summary>
    public sealed class PytanieOPodmiotRequest
    {
        [JsonPropertyName("identyfikatorSystemowyPodmiotu")]
        public string? IdentyfikatorSystemowyPodmiotu { get; set; }
    }
}
