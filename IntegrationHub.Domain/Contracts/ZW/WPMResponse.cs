// IntegrationHub.Domain.Contracts.ZW/WPMResponse.cs
using System.Text.Json.Serialization;

namespace IntegrationHub.Domain.Contracts.ZW
{
    /// <summary>Rekord pojazdu z bazy piesp.PojazdyWojskowe.</summary>
    public sealed class WPMResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("nrRejestracyjny")]
        public string? NrRejestracyjny { get; init; }

        [JsonPropertyName("opis")]
        public string? Opis { get; init; }

        [JsonPropertyName("rokProdukcji")]
        public int? RokProdukcji { get; init; }

        [JsonPropertyName("numerPodwozia")]
        public string? NumerPodwozia { get; init; }

        [JsonPropertyName("nrSerProducenta")]
        public string? NrSerProducenta { get; init; }

        [JsonPropertyName("nrSerSilnika")]
        public string? NrSerSilnika { get; init; }

        [JsonPropertyName("miejscowosc")]
        public string? Miejscowosc { get; init; }

        [JsonPropertyName("jednostkaWojskowa")]
        public string? JednostkaWojskowa { get; init; }

        [JsonPropertyName("jednostkaGospodarcza")]
        public string? JednostkaGospodarcza { get; init; }

        [JsonPropertyName("dataAktualizacji")]
        public string? DataAktualizacji { get; init; }
    }
}
