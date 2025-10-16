// IntegrationHub.Sources.ANPRS/DTO/ANPRSDtos.cs
using System.Collections.Generic;

namespace IntegrationHub.Sources.ANPRS.Contracts
{
    /// <summary>
    /// Ogólny kształt odpowiedzi siatkowej ANPRS: listy nazw kolumn i wierszy danych (string[][]).
    /// </summary>
    public record ANPRSGridResponse
    {
        public List<string> ColumnsNames { get; init; } = new();
        public List<List<string>> Data { get; init; } = new();
    }

    // --- Raporty (/Service/api/Reports/...) ---

    /// <summary>Raport: /Reports/LicenseplateWithGeo</summary>
    public record LicensePlateReportResponse : ANPRSGridResponse { }

    /// <summary>Raport: /Reports/VehiclesInPointWithGeo</summary>
    public record VehiclesInPointResponse : ANPRSGridResponse { }

    // --- Zawartość źródłowa (/Service/api/Source/...) ---

    /// <summary>
    /// Zdarzenie (X-Event-Version=2): { "content": "&lt;event&gt;...xml...&lt;/event&gt;" }.
    /// Dla wersji 1 serwis zwraca czysty XML jako string JSON, ale na potrzeby integracji używamy wersji 2.
    /// </summary>
    public record EventContentResponse
    {
        public string? Content { get; init; }
    }

    /// <summary>
    /// Zdjęcia (Base64) oraz ewentualnie kolumna "Położenie numeru" w wersji 2.
    /// Zgodnie ze specyfikacją to także układ columnsNames + data.
    /// </summary>
    public record PhotoResponse : ANPRSGridResponse { }

    // --- Słowniki (/Service/api/Dictionary...) ---

    /// <summary>/Dictionary?type=bcp lub /Dictionary?type=countries</summary>
    public record DictionaryResponse : ANPRSGridResponse { }

    /// <summary>/Dictionary/Systems?country=PLN</summary>
    public record SystemsResponse : ANPRSGridResponse { }

    /// <summary>/Dictionary/BCP?country=PLN&amp;system=OCR</summary>
    public record BCPResponse : ANPRSGridResponse { }

    // --- Test (/Service/api/Test) ---

    /// <summary>
    /// /Service/api/Test – specyfikacja nie narzuca schematu; pozostawiamy placeholder.
    /// Jeśli będziesz chciał, można tu dopasować do rzeczywistej odpowiedzi serwera.
    /// </summary>
    public record TestResponse
    {
        // Dodaj właściwości po ustaleniu schematu odpowiedzi testowej (jeśli potrzebne).
    }
}
