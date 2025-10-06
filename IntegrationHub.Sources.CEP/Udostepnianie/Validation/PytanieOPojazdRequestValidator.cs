// IntegrationHub.Sources.CEP.Udostepnianie.Validation/PytanieOPojazdRequestValidator.cs
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Validation
{
    /// <summary>
    /// Waliduje minimalne kryteria zapytania i normalizuje pola requestu.
    /// Zwraca false + ProxyResponse 400 gdy kryteria są niespełnione lub format niepoprawny.
    /// Reguły minimalne (spełnij co najmniej jedno z poniższych):
    /// 1) typDokumentu + dokumentSeriaNumer
    /// 2) numerRejestracyjny
    /// 3) numerRejestracyjnyZagraniczny
    /// 4) identyfikatorSystemowyPodmiotu
    /// 5) identyfikatorSystemowyPojazdu
    /// 6) identyfikatorCzynnosci + identyfikatorSystemowyPojazdu
    /// 7) numerPodwoziaNadwoziaRamy  (UWAGA: nie może być łączony z żadnym innym parametrem kryteriów)
    /// </summary>
    internal sealed class PytanieOPojazdRequestValidator : IRequestValidator<PytanieOPojazdRequest>
    {
        private static string N(string? s) => (s ?? string.Empty).Trim().ToUpperInvariant();
        private const string DateFmt = "yyyy-MM-dd'T'HH:mm:ss";

        /// <summary>
        /// Waliduje minimalne kryteria zapytania i normalizuje pola requestu.
        /// Zwraca ValidationResult.IsValid = true albo ValidationResult.IsValid = false gdy kryteria są niespełnione lub format niepoprawny.
        /// Reguły minimalne (spełnij co najmniej jedno z poniższych):
        /// 1) typDokumentu + dokumentSeriaNumer
        /// 2) numerRejestracyjny
        /// 3) numerRejestracyjnyZagraniczny
        /// 4) identyfikatorSystemowyPodmiotu
        /// 5) identyfikatorSystemowyPojazdu
        /// 6) identyfikatorCzynnosci + identyfikatorSystemowyPojazdu
        /// 7) numerPodwoziaNadwoziaRamy  (UWAGA: nie może być łączony z żadnym innym parametrem kryteriów)
        /// </summary>
        public ValidationResult ValidateAndNormalize(PytanieOPojazdRequest body)
        {
            // Normalizacje
            body.NumerRejestracyjny = string.IsNullOrWhiteSpace(body.NumerRejestracyjny) ? null : N(body.NumerRejestracyjny);
            body.NumerRejestracyjnyZagraniczny = string.IsNullOrWhiteSpace(body.NumerRejestracyjnyZagraniczny) ? null : N(body.NumerRejestracyjnyZagraniczny);
            body.IdentyfikatorSystemowyPojazdu = string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPojazdu) ? null : body.IdentyfikatorSystemowyPojazdu.Trim();
            body.IdentyfikatorSystemowyPodmiotu = string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPodmiotu) ? null : body.IdentyfikatorSystemowyPodmiotu.Trim();
            body.IdentyfikatorCzynnosci = string.IsNullOrWhiteSpace(body.IdentyfikatorCzynnosci) ? null : body.IdentyfikatorCzynnosci.Trim();

            if (body.ParametryDokumentuPojazdu is { } doc)
            {
                doc.TypDokumentu = string.IsNullOrWhiteSpace(doc.TypDokumentu) ? "DICT155_DR" : N(doc.TypDokumentu);
                doc.DokumentSeriaNumer = string.IsNullOrWhiteSpace(doc.DokumentSeriaNumer) ? null : N(doc.DokumentSeriaNumer);

                if (string.IsNullOrWhiteSpace(doc.TypDokumentu) && string.IsNullOrWhiteSpace(doc.DokumentSeriaNumer))
                    body.ParametryDokumentuPojazdu = null;
            }

            // VIN
            if (!string.IsNullOrWhiteSpace(body.NumerPodwoziaNadwoziaRamy))
            {
                var vin = body.NumerPodwoziaNadwoziaRamy.Replace(" ", "").Trim().ToUpperInvariant();
                if (!Regex.IsMatch(vin, "^[A-Z0-9]{3,30}$"))
                    return ValidationResult.Fail("Niepoprawny format numeru VIN/podwozia/nadwozia/ramy.");
                body.NumerPodwoziaNadwoziaRamy = vin;
            }

            // dataPrezentacji
            if (!string.IsNullOrWhiteSpace(body.DataPrezentacji))
            {
                if (!DateTime.TryParseExact(body.DataPrezentacji.Trim(), DateFmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    return ValidationResult.Fail($"Niepoprawny format dataPrezentacji. Oczekiwany: {DateFmt} (np. 2025-10-05T21:20:00).");

                body.DataPrezentacji = dt.ToString(DateFmt, CultureInfo.InvariantCulture);
            }

            // VIN nie łączymy z innymi kryteriami
            var hasVin = !string.IsNullOrWhiteSpace(body.NumerPodwoziaNadwoziaRamy);
            if (hasVin)
            {
                var anyOther =
                    !string.IsNullOrWhiteSpace(body.NumerRejestracyjny) ||
                    !string.IsNullOrWhiteSpace(body.NumerRejestracyjnyZagraniczny) ||
                    !string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPojazdu) ||
                    !string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPodmiotu) ||
                    body.ParametryDokumentuPojazdu != null ||
                    !string.IsNullOrWhiteSpace(body.IdentyfikatorCzynnosci);

                if (anyOther)
                    return ValidationResult.Fail("Wyszukiwanie po numerze VIN/podwozia/nadwozia/ramy nie może być łączone z innymi parametrami.");

                return ValidationResult.Ok();
            }

            // Minimalne kryteria
            var byDoc = body.ParametryDokumentuPojazdu is { TypDokumentu: { Length: > 0 }, DokumentSeriaNumer: { Length: > 0 } };
            var byRegPl = !string.IsNullOrWhiteSpace(body.NumerRejestracyjny);
            var byRegForeign = !string.IsNullOrWhiteSpace(body.NumerRejestracyjnyZagraniczny);
            var byEntityId = !string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPodmiotu);
            var byVehicleId = !string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPojazdu);
            var byActionPlusVehicle = !string.IsNullOrWhiteSpace(body.IdentyfikatorCzynnosci) && byVehicleId;

            if (!(byDoc || byRegPl || byRegForeign || byEntityId || byVehicleId || byActionPlusVehicle))
                return ValidationResult.Fail("Podaj przynajmniej jedno z kryteriów: (typDokumentu + dokumentSeriaNumer) / numerRejestracyjny / numerRejestracyjnyZagraniczny / identyfikatorSystemowyPodmiotu / identyfikatorSystemowyPojazdu / (identyfikatorCzynnosci + identyfikatorSystemowyPojazdu) / numerPodwoziaNadwoziaRamy.");

            return ValidationResult.Ok();
        }
    }
}
