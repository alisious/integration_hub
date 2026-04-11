namespace IntegrationHub.PIESP.Models
{
    /// <summary>
    /// Typy ról przypisywanych użytkownikowi.
    /// </summary>
    public enum RoleType { User, Supervisor, PowerUser }

    /// <summary>
    /// Powiązanie użytkownika z konkretną rolą w systemie.
    /// </summary>
    public class UserRole
    {
        /// <summary>
        /// Identyfikator wpisu (klucz główny).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Rodzaj przypisanej roli.
        /// </summary>
        public RoleType Role { get; set; }

        /// <summary>
        /// Id użytkownika (GUID) – klucz obcy do Users.Id.
        /// </summary>
        public Guid UserId { get; set; }
                
    }
}
