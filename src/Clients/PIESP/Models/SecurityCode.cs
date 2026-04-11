namespace IntegrationHub.PIESP.Models
{
    /// <summary>
    /// Kod bezpieczeństwa przypisany do użytkownika, generowany przez przełożonego.
    /// </summary>
    public class SecurityCode
    {
        /// <summary>
        /// Identyfikator kodu (klucz główny).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Numer odznaki użytkownika, dla którego wygenerowano kod (bez zmiany na GUID – zgodnie z minimalnym zakresem modyfikacji).
        /// </summary>
        public required string BadgeNumber { get; set; }

        /// <summary>
        /// Właściwy 6-cyfrowy kod bezpieczeństwa.
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// Czas wygaśnięcia ważności kodu.
        /// </summary>
        public DateTime Expiry { get; set; }
    }
}
