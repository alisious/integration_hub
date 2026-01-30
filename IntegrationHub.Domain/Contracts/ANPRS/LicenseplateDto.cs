using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using IntegrationHub.Sources.ANPRS.Contracts;

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

        /// <summary>Zdjęcia zdarzenia (dociągane przy generowaniu raportu).</summary>
        [JsonPropertyName("zdjecia")]
        public List<VehiclePhotoRowDto> Photos { get; set; } = new();

        /// <summary>"yes" | "no" – czy zdjęcia są kompletne.</summary>
        [JsonPropertyName("zdjeciaKompletne")]
        public string PhotosComplete { get; set; } = "Brak danych";
    }
}