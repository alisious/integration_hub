// IntegrationHub.Sources.CEP.UpKi/RequestValidation/DaneDokumentuRequestValidator.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using IntegrationHub.Common.RequestValidation;      // IRequestValidator<TRequest>, ValidationResult
using IntegrationHub.Sources.CEP.UpKi.Contracts;    // DaneDokumentuRequest, DaneOsoby

namespace IntegrationHub.Sources.CEP.UpKi.RequestValidation
{
    /// <summary>
    /// Walidator żądania DaneDokumentuRequest dla usługi UpKi (uprawnienia kierowców – CEK).
    ///
    /// Minimalne kombinacje kryteriów wyszukiwania (wystarczy jedna z poniższych):
    ///  • numerPesel, opcjonalnie dataZapytania
    ///  • numerDokumentu, opcjonalnie dataZapytania
    ///  • seriaNumerDokumentu, opcjonalnie dataZapytania
    ///  • daneOsoby: imiePierwsze, nazwisko, dataUrodzenia, opcjonalnie dataZapytania
    ///  • osobaId, opcjonalnie dataZapytania
    ///  • idk, opcjonalnie dataZapytania
    ///
    /// dataZapytania i dataUrodzenia muszą być, jeśli podane, w formacie RRRR-MM-RR (yyyy-MM-dd).
    /// Validator dokonuje też normalizacji (trim + standaryzacja formatu daty).
    /// </summary>
    public sealed class DaneDokumentuRequestValidator : IRequestValidator<DaneDokumentuRequest>
    {
        public ValidationResult ValidateAndNormalize(DaneDokumentuRequest body)
        {
            if (body is null)
                return ValidationResult.Fail("Body (DaneDokumentuRequest) nie może być null.");

            var errors = new List<string>();

            // --- NORMALIZACJA STRINGÓW (trim / null) ---

            body.DataZapytania = Normalize(body.DataZapytania);
            body.NumerPesel = Normalize(body.NumerPesel);
            body.NumerDokumentu = Normalize(body.NumerDokumentu);
            body.SeriaNumerDokumentu = Normalize(body.SeriaNumerDokumentu);
            body.OsobaId = Normalize(body.OsobaId);
            body.Idk = Normalize(body.Idk);

            if (body.DaneOsoby is not null)
            {
                body.DaneOsoby.ImiePierwsze = Normalize(body.DaneOsoby.ImiePierwsze);
                body.DaneOsoby.Nazwisko = Normalize(body.DaneOsoby.Nazwisko);
                body.DaneOsoby.DataUrodzenia = Normalize(body.DaneOsoby.DataUrodzenia);
            }

            // --- WALIDACJA FORMATU DAT (jeśli podane) ---

            body.DataZapytania = ValidateAndNormalizeDate(
                body.DataZapytania,
                "dataZapytania",
                errors);

            if (body.DaneOsoby is not null)
            {
                body.DaneOsoby.DataUrodzenia = ValidateAndNormalizeDate(
                    body.DaneOsoby.DataUrodzenia,
                    "daneOsoby.dataUrodzenia",
                    errors);
            }

            // --- SPRAWDZENIE MINIMALNEJ KOMBINACJI KRYTERIÓW ---

            var hasPesel = !string.IsNullOrEmpty(body.NumerPesel);
            var hasNumerDokumentu = !string.IsNullOrEmpty(body.NumerDokumentu);
            var hasSeriaNumerDokumentu = !string.IsNullOrEmpty(body.SeriaNumerDokumentu);

            var hasDaneOsoby =
                body.DaneOsoby is not null &&
                !string.IsNullOrEmpty(body.DaneOsoby.ImiePierwsze) &&
                !string.IsNullOrEmpty(body.DaneOsoby.Nazwisko) &&
                !string.IsNullOrEmpty(body.DaneOsoby.DataUrodzenia);

            var hasOsobaId = !string.IsNullOrEmpty(body.OsobaId);
            var hasIdk = !string.IsNullOrEmpty(body.Idk);

            var anyMode =
                hasPesel ||
                hasNumerDokumentu ||
                hasSeriaNumerDokumentu ||
                hasDaneOsoby ||
                hasOsobaId ||
                hasIdk;

            if (!anyMode)
            {
                errors.Add(
                    "Wymagane jest podanie jednej z minimalnych kombinacji kryteriów wyszukiwania: " +
                    "(1) numerPesel, (2) numerDokumentu, (3) seriaNumerDokumentu, " +
                    "(4) daneOsoby (imiePierwsze, nazwisko, dataUrodzenia), (5) osobaId, (6) idk.");
            }

            // Jeżeli daneOsoby są wypełniane, ale niekompletnie – zasygnalizujmy to wprost
            if (body.DaneOsoby is not null && !hasDaneOsoby)
            {
                if (string.IsNullOrEmpty(body.DaneOsoby.ImiePierwsze) ||
                    string.IsNullOrEmpty(body.DaneOsoby.Nazwisko) ||
                    string.IsNullOrEmpty(body.DaneOsoby.DataUrodzenia))
                {
                    errors.Add(
                        "Dla wyszukiwania po daneOsoby wymagane są wszystkie pola: imiePierwsze, nazwisko, dataUrodzenia.");
                }
            }

            // --- ZWROT WYNIKU ---

            if (errors.Count == 0)
                return ValidationResult.Ok();

            var msg = string.Join(" ", errors);
            return ValidationResult.Fail(msg);
        }

        private static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }

        /// <summary>
        /// Jeżeli <paramref name="date"/> jest niepuste, sprawdza format "yyyy-MM-dd"
        /// i w razie sukcesu zwraca wartość znormalizowaną do dokładnie takiej postaci.
        /// W razie błędu dopisuje komunikat do <paramref name="errors"/> i zwraca oryginał.
        /// </summary>
        private static string? ValidateAndNormalizeDate(
            string? date,
            string fieldName,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(date))
                return date;

            var raw = date.Trim();

            if (!DateTime.TryParseExact(
                    raw,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed))
            {
                errors.Add($"{fieldName} musi być w formacie RRRR-MM-RR (yyyy-MM-dd).");
                return date;
            }

            return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
