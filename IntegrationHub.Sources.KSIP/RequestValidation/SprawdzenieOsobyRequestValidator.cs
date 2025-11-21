using System;
using System.Globalization;
using IntegrationHub.Common.RequestValidation;
using IntegrationHub.Sources.KSIP.Contracts;

namespace IntegrationHub.Sources.KSIP.RequestValidation
{
    public sealed class SprawdzenieOsobyRequestValidator
        : IRequestValidator<SprawdzenieOsobyRequest>
    {
        public ValidationResult ValidateAndNormalize(SprawdzenieOsobyRequest body)
        {
            if (body is null)
            {
                return ValidationResult.Fail("Żądanie (SprawdzenieOsobyRequest) nie może być null.");
            }

            // Prosta normalizacja – przycinamy białe znaki
            body.UserId = body.UserId?.Trim();
            body.NrPesel = body.NrPesel?.Trim();
            body.FirstName = body.FirstName?.Trim();
            body.LastName = body.LastName?.Trim();
            body.BirthDate = body.BirthDate?.Trim();

            if (string.IsNullOrWhiteSpace(body.UserId))
            {
                return ValidationResult.Fail("Wymagany identyfikator użytkownika KSIP (UserID).");
            }

            var hasPesel = !string.IsNullOrWhiteSpace(body.NrPesel);
            var hasPersonTriple =
                !string.IsNullOrWhiteSpace(body.FirstName) &&
                !string.IsNullOrWhiteSpace(body.LastName) &&
                !string.IsNullOrWhiteSpace(body.BirthDate);

            if (!hasPesel && !hasPersonTriple)
            {
                return ValidationResult.Fail(
                    "Wymagane jest podanie numeru PESEL (NrPESEL) albo " +
                    "kombinacji: imię (FirstName), nazwisko (LastName) i data urodzenia (BirthDate).");
            }

            if (hasPesel && hasPersonTriple)
            {
                return ValidationResult.Fail(
                    "Podano jednocześnie PESEL oraz imię, nazwisko i datę urodzenia. " +
                    "Dla zapytania należy wybrać tylko jedno z kryteriów: PESEL albo (FirstName, LastName, BirthDate).");
            }

            if (hasPersonTriple)
            {
                // Walidacja formatu daty urodzenia: yyyy-MM-dd
                if (!DateTime.TryParseExact(body.BirthDate!,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsed))
                {
                    return ValidationResult.Fail(
                        "Data urodzenia (BirthDate) musi być w formacie 'yyyy-MM-dd'.");
                }

                // Normalizacja formatu (np. gdyby przyszło 2025-1-2 – wymusimy 2025-01-02)
                body.BirthDate = parsed.ToString("yyyy-MM-dd");
            }

            return ValidationResult.Ok();
        }
    }
}
