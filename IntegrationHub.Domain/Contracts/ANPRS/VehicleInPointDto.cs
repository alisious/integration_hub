using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace IntegrationHub.Domain.Contracts.ANPRS
{
    /// <summary>
    /// Data transfer object representing a vehicle record from ANPRS "Vehicles in point" response.
    /// Columns mapping (by index):
    /// 0 - Identyfikator (Guid)
    /// 1 - Numer (string)
    /// 2 - Data zdarzenia (DateTime, "yyyy-MM-dd HH:mm:ss")
    /// 3 - Typ pojazdu (string)
    /// 4 - Lokalizacja nr. (string)
    /// 5 - Kraj poj. (string)
    /// 6 - ADR (string)
    /// 7 - BCP (string)
    /// 8 - Latitude (double)
    /// 9 - Longitude (double)
    /// </summary>
    public class VehicleInPointDto
    {
        [JsonPropertyName("identyfikator")]
        public Guid Id { get; set; }

        [JsonPropertyName("numer")]
        public string Number { get; set; } = string.Empty;

        [JsonPropertyName("dataZdarzenia")]
        public DateTime EventDate { get; set; }

        [JsonPropertyName("typPojazdu")]
        public string VehicleType { get; set; } = string.Empty;

        [JsonPropertyName("lokalizacjaNr")]
        public string Location { get; set; } = string.Empty;

        [JsonPropertyName("krajPoj")]
        public string Country { get; set; } = string.Empty;

        [JsonPropertyName("adr")]
        public string Adr { get; set; } = string.Empty;

        [JsonPropertyName("bcp")]
        public string Bcp { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }
}