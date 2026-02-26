using IntegrationHub.Common.RequestValidation;
using IntegrationHub.Sources.ZW.Contracts;

namespace IntegrationHub.Sources.ZW.RequestValidation
{
    public sealed class WeaponAddressSearchRequestValidator : IRequestValidator<WeaponAddressSearchRequest>
    {
        public ValidationResult ValidateAndNormalize(WeaponAddressSearchRequest body)
        {
            if (body is null)
            {
                return ValidationResult.Fail("Request nie może być null.");
            }

            static string? Norm(string? s) =>
                string.IsNullOrWhiteSpace(s) ? null : s.Trim();

            body.Miejscowosc = Norm(body.Miejscowosc);
            body.Ulica = Norm(body.Ulica);
            body.NumerDomu = Norm(body.NumerDomu);
            body.NumerLokalu = Norm(body.NumerLokalu);
            body.KodPocztowy = Norm(body.KodPocztowy);
            body.Poczta = Norm(body.Poczta);

            // Minimalne kryteria wyszukiwania: Miejscowość i NumerDomu
            if (string.IsNullOrWhiteSpace(body.Miejscowosc) ||
                string.IsNullOrWhiteSpace(body.NumerDomu))
            {
                return ValidationResult.Fail(
                    "Minimalne kryteria wyszukiwania: Miejscowość i NumerDomu są wymagane.");
            }

            return ValidationResult.Ok();
        }
    }
}
