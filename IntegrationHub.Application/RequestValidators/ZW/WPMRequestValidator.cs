// IntegrationHub.Application.RequestValidators.ZW/WPMRequestValidator.cs
using IntegrationHub.Common.RequestValidation;   // IRequestValidator<T>, ValidationResult
using IntegrationHub.Domain.Contracts.ZW;        // WPMRequest
using System.Text.RegularExpressions;

namespace IntegrationHub.Application.RequestValidators.ZW
{
    /// <summary>
    /// Walidacja i normalizacja zapytania WPM:
    /// - Wymaga podania co najmniej jednego z pól: NrRejestracyjny / NumerPodwozia / NrSerProducenta / NrSerSilnika.
    /// - Normalizuje: TRIM; UpperInvariant; usuwa spacje i myślniki z pól „technicznych”.
    /// - Sprawdza długości względem schematu (NVARCHAR(50)).
    /// </summary>
    public sealed class WPMRequestValidator : IRequestValidator<WPMRequest>
    {
        // Długości docelowych kolumn w dbo/piesp.PojazdyWojskowe
        private const int MaxLen = 50;

        public ValidationResult ValidateAndNormalize(WPMRequest body)
        {
            if (body is null)
                return ValidationResult.Fail("Brak treści żądania.");

            // --- Normalizacja ---
            body = Normalize(body);

            // --- Reguły minimalne ---
            var hasAny =
                !string.IsNullOrWhiteSpace(body.NrRejestracyjny) ||
                !string.IsNullOrWhiteSpace(body.NumerPodwozia) ||
                !string.IsNullOrWhiteSpace(body.NrSerProducenta) ||
                !string.IsNullOrWhiteSpace(body.NrSerSilnika);

            if (!hasAny)
                return ValidationResult.Fail("Podaj przynajmniej jedno kryterium: nrRejestracyjny / numerPodwozia / nrSerProducenta / nrSerSilnika.");

            // --- Limity długości (zgodnie z tabelą: NVARCHAR(50)) ---
            if (TooLong(body.NrRejestracyjny)) return ValidationResult.Fail("nrRejestracyjny przekracza 50 znaków po normalizacji.");
            if (TooLong(body.NumerPodwozia)) return ValidationResult.Fail("numerPodwozia przekracza 50 znaków po normalizacji.");
            if (TooLong(body.NrSerProducenta)) return ValidationResult.Fail("nrSerProducenta przekracza 50 znaków po normalizacji.");
            if (TooLong(body.NrSerSilnika)) return ValidationResult.Fail("nrSerSilnika przekracza 50 znaków po normalizacji.");

            return ValidationResult.Ok();
        }

        private static bool TooLong(string? s) => s != null && s.Length > MaxLen;

        private static WPMRequest Normalize(WPMRequest r)
        {
            // Pomocnicze
            static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            static string? Upper(string? s) => s?.ToUpperInvariant();

            // Usuwanie separatorów typowych dla numerów (VIN/rejestr/seryjne)
            static string? StripSeparators(string? s)
            {
                if (string.IsNullOrEmpty(s)) return s;
                // usuń spacje i myślniki; pozostaw litery/cyfry
                return Regex.Replace(s, @"[\s\-]", "");
            }

            // Normalizacja poszczególnych pól
            var nrRej = Upper(StripSeparators(TrimOrNull(r.NrRejestracyjny)));
            var vin = Upper(StripSeparators(TrimOrNull(r.NumerPodwozia)));
            var prod = Upper(StripSeparators(TrimOrNull(r.NrSerProducenta)));
            var silnik = Upper(StripSeparators(TrimOrNull(r.NrSerSilnika)));

            return new WPMRequest
            {
                NrRejestracyjny = nrRej,
                NumerPodwozia = vin,
                NrSerProducenta = prod,
                NrSerSilnika = silnik
            };
        }
    }
}
