using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.KSIP.Contracts
{
    public sealed class SprawdzenieOsobyRequest
    {
        /// <summary>
        /// Identyfikator użytkownika w KSIP (spr:UserID).
        /// </summary>
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        /// <summary>
        /// PESEL osoby sprawdzanej (spr:NrPESEL).
        /// </summary>
        [JsonPropertyName("nrPesel")]
        public string? NrPesel { get; set; }

        /// <summary>
        /// Imię osoby sprawdzanej (spr:Person/PersonName/FirstName).
        /// </summary>
        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        /// <summary>
        /// Nazwisko osoby sprawdzanej (spr:Person/PersonName/LastName).
        /// </summary>
        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        /// <summary>
        /// Data urodzenia osoby sprawdzanej w formacie yyyy-MM-dd (spr:BirthDate).
        /// </summary>
        [JsonPropertyName("birthDate")]
        public string? BirthDate { get; set; }
        [JsonPropertyName("terminalName")]
        public string? TerminalName { get; set; }
    }
}
