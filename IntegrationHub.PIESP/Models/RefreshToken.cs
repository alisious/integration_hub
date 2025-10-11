using System;
using System.Text.Json.Serialization;

namespace IntegrationHub.PIESP.Models
{
    /// <summary>
    /// Opaque Refresh Token przechowywany w bazie w postaci SHA-256 (TokenHash).
    /// Rotowany przy każdym odświeżeniu; wykrywa reuse przez łańcuch ReplacedById.
    /// </summary>
    public class RefreshToken
    {
        /// <summary>Identyfikator rekordu (PK).</summary>
        public int Id { get; set; }

        /// <summary>Użytkownik, do którego należy token (FK do Users.Id).</summary>
        public Guid UserId { get; set; }

        /// <summary>Rodzina tokenów (wszystkie kolejne rotacje jednego “łańcucha”).</summary>
        public Guid FamilyId { get; set; }

        /// <summary>Hash (SHA-256) tokena — nie serializujemy.</summary>
        [JsonIgnore]
        public byte[] TokenHash { get; set; } = default!;

        /// <summary>Czas wystawienia tokena (UTC).</summary>
        public DateTime IssuedAt { get; set; }

        /// <summary>Czas wygaśnięcia tokena (UTC).</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Id nowego tokena, który zastąpił ten (rotacja).</summary>
        public int? ReplacedById { get; set; }

        /// <summary>Czas unieważnienia (jeśli unieważniony).</summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>Powód unieważnienia (opcjonalnie).</summary>
        public string? RevokedReason { get; set; }
    }
}
