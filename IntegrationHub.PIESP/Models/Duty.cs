using System.Text.Json.Serialization;

namespace IntegrationHub.PIESP.Models
{
    /// <summary>
    /// Status służby patrolowej.
    /// </summary>
    public enum DutyStatus { Planned, InProgress, Finished }

    /// <summary>
    /// Reprezentuje pojedynczą służbę przypisaną do użytkownika.
    /// </summary>
    public class Duty
    {
        /// <summary>
        /// Identyfikator służby (klucz główny).
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Identyfikator użytkownika (GUID) przypisanego do służby (FK do Users.Id).
        /// </summary>
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Rodzaj służby (np. „Patrol zapobiegawczy”).
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        /// <summary>
        /// Planowana data rozpoczęcia służby.
        /// </summary>
        [JsonPropertyName("start")]
        public DateTime Start { get; set; }

        /// <summary>
        /// Planowana data zakończenia służby (opcjonalnie).
        /// </summary>
        [JsonPropertyName("end")]
        public DateTime? End { get; set; }

        /// <summary>
        /// Jednostka organizacyjna (np. „OŻW Bydgoszcz”).
        /// </summary>
        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        /// <summary>
        /// Aktualny status służby.
        /// </summary>
        [JsonPropertyName("status")]
        public DutyStatus Status { get; set; }

        /// <summary>
        /// Faktyczny czas rozpoczęcia służby (opcjonalnie).
        /// </summary>
        [JsonPropertyName("actualStart")]
        public DateTime? ActualStart { get; set; }

        /// <summary>
        /// Faktyczny czas zakończenia służby (opcjonalnie).
        /// </summary>
        [JsonPropertyName("actualEnd")]
        public DateTime? ActualEnd { get; set; }

        /// <summary>
        /// Szerokość geograficzna miejsca faktycznego rozpoczęcia służby.
        /// </summary>
        [JsonPropertyName("actualStartLatitude")]
        public decimal? ActualStartLatitude { get; set; }

        /// <summary>
        /// Długość geograficzna miejsca faktycznego rozpoczęcia służby.
        /// </summary>
        [JsonPropertyName("actualStartLongitude")]
        public decimal? ActualStartLongitude { get; set; }

        /// <summary>
        /// Szerokość geograficzna miejsca faktycznego zakończenia służby.
        /// </summary>
        [JsonPropertyName("actualEndLatitude")]
        public decimal? ActualEndLatitude { get; set; }

        /// <summary>
        /// Długość geograficzna miejsca faktycznego zakończenia służby.
        /// </summary>
        [JsonPropertyName("actualEndLongitude")]
        public decimal? ActualEndLongitude { get; set; }
    }
}
