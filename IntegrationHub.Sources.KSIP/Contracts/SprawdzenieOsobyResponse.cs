using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.KSIP.Contracts
{
    public sealed class SprawdzenieOsobyResponse
    {
        [JsonPropertyName("person")]
        public SprawdzenieOsobyPerson? Person { get; set; }

        [JsonPropertyName("offenseRecords")]
        public List<SprawdzenieOsobyOffenseRecord> OffenseRecords { get; set; } = new();

        /// <summary>
        /// Stan odpowiedzi – np. 0 (brak wykroczeń), 1 (są wpisy). 
        /// </summary>
        [JsonPropertyName("state")]
        public int State { get; set; }
    }

    public sealed class SprawdzenieOsobyPerson
    {
        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("peselNumber")]
        public string? PeselNumber { get; set; }

        [JsonPropertyName("birthDate")]
        public string? BirthDate { get; set; }
    }

    public sealed class SprawdzenieOsobyOffenseRecord
    {
        [JsonPropertyName("incidentDate")]
        public string? IncidentDate { get; set; }

        [JsonPropertyName("finePaymentDate")]
        public string? FinePaymentDate { get; set; }

        [JsonPropertyName("validationOfDecisionDate")]
        public string? ValidationOfDecisionDate { get; set; }

        [JsonPropertyName("classification")]
        public SprawdzenieOsobyPersonCriminalRecordClassification? Classification { get; set; }
    }

    public sealed class SprawdzenieOsobyPersonCriminalRecordClassification
    {
        [JsonPropertyName("legalClassificationCode")]
        public string? LegalClassificationCode { get; set; }

        [JsonPropertyName("classificationCode")]
        public string? ClassificationCode { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
