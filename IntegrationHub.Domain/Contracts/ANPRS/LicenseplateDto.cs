using System;
using System.Text.Json.Serialization;

namespace IntegrationHub.Domain.Contracts.ANPRS
{
    /// <summary>
    /// DTO dla raportu LicenseplateWithGeo (mapowanie z kolumn: columnsNames z odpowiedzi ANPRS).
    /// Nazwy JSON w atrybutach są znormalizowane i rozpoczynają się od małej litery (camelCase / polskie skróty gdzie sensowne).
    /// </summary>
    public class LicenseplateDto
    {
        [JsonPropertyName("identyfikator")]
        public Guid Id { get; set; }

        [JsonPropertyName("numer")]
        public string Number { get; set; } = string.Empty;

        [JsonPropertyName("typPojazdu")]
        public string VehicleType { get; set; } = string.Empty;

        [JsonPropertyName("lokalizacjaNr")]
        public string Location { get; set; } = string.Empty;

        [JsonPropertyName("krajPoj")]
        public string Country { get; set; } = string.Empty;

        [JsonPropertyName("dataZdarzenia")]
        public DateTime EventDate { get; set; }

        [JsonPropertyName("krajBcp")]
        public string BcpCountry { get; set; } = string.Empty;

        [JsonPropertyName("bcp")]
        public string Bcp { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }
}