using System;
using System.Globalization;
using System.Text.RegularExpressions;
using IntegrationHub.Common.RequestValidation;
using IntegrationHub.Sources.KSIP.Contracts;

namespace IntegrationHub.Sources.KSIP.RequestValidation
{
    /// <summary>
    /// Reguły minimalne:
    /// - UserID + NrPESEL
    ///   ALBO
    /// - UserID + FirstName + LastName + BirthDate (yyyy-MM-dd)
    /// </summary>
    internal sealed class SprawdzenieOsobyWRuchuDrogowymRequestValidator
        : IRequestValidator<SprawdzenieOsobyWRuchuDrogowymRequest>
    {
        private static string N(string? s) => (s ?? string.Empty).Trim();

        public ValidationResult ValidateAndNormalize(SprawdzenieOsobyWRuchuDrogowymRequest body)
        {
            if (body is null) return ValidationResult.Fail("Brak treści żądania.");

            body.UserId = string.IsNullOrWhiteSpace(body.UserId) ? null : N(body.UserId);

            // PESEL – jeśli podany, normalizujemy/czyścimy.
            if (!string.IsNullOrWhiteSpace(body.NrPesel))
            {
                var pesel = Regex.Replace(body.NrPesel, "[^0-9]", "");
                if (pesel.Length != 11)
                    return ValidationResult.Fail("NrPESEL musi mieć 11 cyfr.");
                body.NrPesel = pesel;
            }
            else
            {
                body.NrPesel = null;
            }

            body.FirstName = string.IsNullOrWhiteSpace(body.FirstName) ? null : N(body.FirstName);
            body.LastName = string.IsNullOrWhiteSpace(body.LastName) ? null : N(body.LastName);

            // BirthDate (opcjonalnie, ale wymagane w wariancie B)
            if (!string.IsNullOrWhiteSpace(body.BirthDate))
            {
                if (!DateTime.TryParseExact(body.BirthDate.Trim(), "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    return ValidationResult.Fail("BirthDate ma mieć format yyyy-MM-dd.");
                body.BirthDate = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            else
            {
                body.BirthDate = null;
            }

            // Minimalne kryteria
            var hasUser = !string.IsNullOrWhiteSpace(body.UserId);
            var byPesel = !string.IsNullOrWhiteSpace(body.NrPesel);
            var byNameDob = !string.IsNullOrWhiteSpace(body.FirstName)
                            && !string.IsNullOrWhiteSpace(body.LastName)
                            && !string.IsNullOrWhiteSpace(body.BirthDate);

            if (!hasUser)
                return ValidationResult.Fail("Wymagany identyfikator użytkownika KSIP (userId).");

            if (!(byPesel || byNameDob))
                return ValidationResult.Fail("Podaj (userId + nrPesel) lub (userId + firstName + lastName + birthDate).");

            return ValidationResult.Ok();
        }
    }
}
