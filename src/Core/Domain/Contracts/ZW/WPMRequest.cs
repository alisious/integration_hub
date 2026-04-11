namespace IntegrationHub.Domain.Contracts.ZW;

/// <summary>
/// Kryteria wyszukiwania WPM (pojazdów wojskowych).
/// Podaj przynajmniej jedno kryterium.
/// </summary>
public sealed class WPMRequest
{
    /// <summary>Nr rejestracyjny (dokładne dopasowanie, porównanie bez spacji brzegowych)</summary>
    public string? NrRejestracyjny { get; init; }

    /// <summary>Numer podwozia (VIN)</summary>
    public string? NumerPodwozia { get; init; }

    /// <summary>Numer seryjny producenta</summary>
    public string? NrSerProducenta { get; init; }

    /// <summary>Numer seryjny silnika</summary>
    public string? NrSerSilnika { get; init; }
}
