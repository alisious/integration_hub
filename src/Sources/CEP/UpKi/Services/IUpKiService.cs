// IntegrationHub.Sources.CEP.UpKi/Services/IUpKiService.cs
using IntegrationHub.Common.Primitives;          // Result<T, Error>
using IntegrationHub.Sources.CEP.UpKi.Contracts; // DaneDokumentuRequest, DaneDokumentuResponse
// IntegrationHub.Sources.CEP.UpKi/Services/IUpKiService.cs

namespace IntegrationHub.Sources.CEP.UpKi.Services
{
    /// <summary>
    /// Serwis UpKi – pytanie o uprawnienia kierowcy (CEK – uprawnienia-kierowcow).
    /// </summary>
    public interface IUpKiService
    {
        /// <summary>
        /// Zwraca informacje o uprawnieniach kierowcy na podstawie numeru PESEL
        /// lub danych osobowych (imię, nazwisko, data urodzenia).
        ///
        /// Wynik: Result&lt;DaneDokumentuResponse, Error&gt;.
        /// </summary>
        Task<Result<DaneDokumentuResponse, Error>> GetDriverPermissionsAsync(
            DaneDokumentuRequest body,
            CancellationToken ct = default);
    }
}
