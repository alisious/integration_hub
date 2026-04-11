using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.CEP.Contracts;

/// <summary>
/// Rodzaj zapytania dla pytanieOPojazd.
/// </summary>
public enum RodzajZapytania
{
    PoNumerzeRejestracyjnym, // numer rejestracyjny (NRREJ)
    PoVIN,                // VIN (numerPodwoziaNadwoziaRamy)
    PoNumerzeDowoduRejestracyjnego,         // dokument DR (typ DICT155_DR + seria/nr)
    PoSystemowymIdPojazdu            // identyfikatorSystemowyPojazdu
}

/// <summary>
/// Stabilny kontrakt wejściowy do pytanieOPojazd (4 wspierane warianty).
/// W mapowaniu zawsze: identyfikatorSystemuZewnetrznego=ŻW, limit=100, wnioskodawca=ŻW, znakSprawy=RequestId.
/// </summary>
public sealed class PytanieOPojazdRequestDto
{
    public RodzajZapytania Kind { get; init; }

    /// <summary>Twój identyfikator sprawy (wnioskodawca/znakSprawy) – np. "P002".</summary>
    public string RequestId { get; init; } = "P002";

    // Wariant: ByRegistrationNumber
    [JsonPropertyName("numerRejestracyjny")]
    public string? NumerRejestracyjny { get; init; }

    // Wariant: ByVin
    [JsonPropertyName("vin")]
    public string? Vin { get; init; }

    // Wariant: ByDocumentDR
    /// <summary>Seria+numer DR, np. "BAT0692471".</summary>
    [JsonPropertyName("numerDowoduRejestracyjnego")]
    public string? NumerDowoduRejestracyjnego { get; init; }

    // Wariant: BySystemId
    [JsonPropertyName("identyfikatorSystemowyPojazd")]
    public string? IdentyfikatorSystemowyPojazdu { get; init; }

    /// <summary>
    /// (Opcjonalnie) nadpisanie limitu; jeśli null, mapowanie ustawi 100 zgodnie z wymaganiem.
    /// </summary>
    public int? Limit { get; init; }

    // Fabryki dla czytelności użycia:
    public static PytanieOPojazdRequestDto FromRegistration(string requestId, string reg)
        => new() { Kind = RodzajZapytania.PoNumerzeRejestracyjnym, RequestId = requestId, NumerRejestracyjny = reg };

    public static PytanieOPojazdRequestDto FromVin(string requestId, string vin)
        => new() { Kind = RodzajZapytania.PoVIN, RequestId = requestId, Vin = vin };

    public static PytanieOPojazdRequestDto FromDocumentDr(string requestId, string seriesNumber)
        => new() { Kind = RodzajZapytania.PoNumerzeDowoduRejestracyjnego, RequestId = requestId, NumerDowoduRejestracyjnego = seriesNumber };

    public static PytanieOPojazdRequestDto FromSystemId(string requestId, string sysId)
        => new() { Kind = RodzajZapytania.PoSystemowymIdPojazdu, RequestId = requestId, IdentyfikatorSystemowyPojazdu = sysId };
}
