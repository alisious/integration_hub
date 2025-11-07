namespace IntegrationHub.Sources.ZW.Contracts
{
    /// <summary>Rekord pojazdu z bazy piesp.PojazdyWojskowe.</summary>
    public sealed class WPMResponse
    {
        public long Id { get; init; }
        public string? NrRejestracyjny { get; init; }
        public string? Opis { get; init; }
        public int? RokProdukcji { get; init; }
        public string? NumerPodwozia { get; init; }
        public string? NrSerProducenta { get; init; }
        public string? NrSerSilnika { get; init; }
        public string? Miejscowosc { get; init; }
        public string? JednostkaWojskowa { get; init; }
        public string? JednostkaGospodarcza { get; init; }
    }
}
