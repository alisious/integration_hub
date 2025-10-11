using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.KSIP.Contracts
{
    /// <summary>
    /// Spłaszczony request: UserID + kryteria w RequestBody (PESEL lub Imię+Nazwisko+DataUr.).
    /// </summary>
    public sealed class SprawdzenieOsobyWRuchuDrogowymRequest
    {
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        // Wariant A: PESEL
        [JsonPropertyName("nrPesel")]
        public string? NrPesel { get; set; }

        // Wariant B: Imię, Nazwisko, Data urodzenia (yyyy-MM-dd)
        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("birthDate")]
        public string? BirthDate { get; set; } // yyyy-MM-dd
    }
}
