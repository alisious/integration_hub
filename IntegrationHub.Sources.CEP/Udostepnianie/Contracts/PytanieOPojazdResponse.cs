// IntegrationHub.Sources.CEP.Udostepnianie.Contracts/PytanieOPojazdResponse.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    public sealed class PytanieOPojazdResponse
    {
        [JsonPropertyName("meta")]
        public PytanieMeta Meta { get; set; } = new();

        [JsonPropertyName("pojazdy")]
        public List<PojazdDto> Pojazdy { get; set; } = new();
    }
       
}
