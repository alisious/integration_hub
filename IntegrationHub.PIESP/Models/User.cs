using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IntegrationHub.PIESP.Models
{
    /// <summary>
    /// Reprezentuje użytkownika systemu PIESP.
    /// </summary>
    public class User
    {
        /// <summary>Identyfikator użytkownika (GUID, klucz główny).</summary>
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        /// <summary>Stopień, imię i nazwisko użytkownika.</summary>
        [JsonPropertyName("userName")]
        public required string UserName { get; set; }

        /// <summary>Numer odznaki (unikalny w PIESP).</summary>
        [JsonPropertyName("badgeNumber")]
        public required string BadgeNumber { get; set; }

        /// <summary>Nazwa jednostki organizacyjnej.</summary>
        [JsonPropertyName("unitName")]
        public string? UnitName { get; set; }

        /// <summary>Flaga aktywności – czy może się logować.</summary>
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Id użytkownika w systemie KSIP (opcjonalny). Może być NULL dopóki nie nastąpi powiązanie.
        /// </summary>
        [JsonPropertyName("ksipUserId")]
        public string? KsipUserId { get; set; }

        /// <summary>Hash PIN (nie jest serializowany do JSON).</summary>
        [JsonIgnore]
        public string? PinHash { get; set; }

        /// <summary>
        /// Wersja tokenu (security stamp). Inkrementowana przy force-logout / zmianie ról.
        /// Służy tylko do walidacji serwerowej.
        /// </summary>
        [JsonIgnore]
        public int TokenVersion { get; set; } = 0;

        /// <summary>Lista ról przypisanych do użytkownika.</summary>
        [JsonPropertyName("roles")]
        public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
    }
}
