namespace IntegrationHub.Infrastructure.Audit
{
    public interface IRequestContext
    {
        /// <summary>
        /// Identyfikator żądania HTTP – ten sam, który zapisujesz w ApiRequestLog.
        /// Ustalany w ApiAuditMiddleware.
        /// </summary>
        string? RequestId { get; set; }

        /// <summary>
        /// Opcjonalny identyfikator korelacyjny (np. z nagłówka X-Correlation-Id).
        /// Jeśli nie używasz – można zostawić null.
        /// </summary>
        string? CorrelationId { get; set; }

        /// <summary>
        /// Techniczny identyfikator użytkownika (np. numer służbowy / SID).
        /// </summary>
        string? UserId { get; set; }

        /// <summary>
        /// Nazwa użytkownika do logów (np. Imię Nazwisko).
        /// </summary>
        string? UserDisplayName { get; set; }

        /// <summary>
        /// Jednostka / organizacja użytkownika, jeśli ją wyciągasz z claimów.
        /// </summary>
        string? UserUnitName { get; set; }
    }
}
