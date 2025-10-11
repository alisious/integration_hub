using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.KSIP.Contracts
{
    public sealed class SprawdzenieOsobyWRuchuDrogowymResponse
    {
        [JsonPropertyName("person")]
        public PersonDto? Person { get; set; }

        [JsonPropertyName("offenseRecords")]
        public List<OffenseRecordDto> OffenseRecords { get; set; } = new();

        /// <summary>0 – brak wykroczeń; 1 – są wykroczenia (wg próbek)</summary>
        [JsonPropertyName("state")]
        public string? State { get; set; }
    }

    public sealed class PersonDto
    {
        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("peselNumber")]
        public string? PeselNumber { get; set; }

        [JsonPropertyName("birthDate")]
        public DateTime? BirthDate { get; set; }
    }

    public sealed class OffenseRecordDto
    {
        [JsonPropertyName("incidentDate")]
        public DateTime? IncidentDate { get; set; }

        [JsonPropertyName("finePaymentDate")]
        public DateTime? FinePaymentDate { get; set; }

        [JsonPropertyName("validationOfDecisionDate")]
        public DateTime? ValidationOfDecisionDate { get; set; }

        [JsonPropertyName("classifications")]
        public List<ClassificationDto> Classifications { get; set; } = new();
    }

    public sealed class ClassificationDto
    {
        [JsonPropertyName("legalClassificationCode")]
        public string? LegalClassificationCode { get; set; }

        [JsonPropertyName("classificationCode")]
        public string? ClassificationCode { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
