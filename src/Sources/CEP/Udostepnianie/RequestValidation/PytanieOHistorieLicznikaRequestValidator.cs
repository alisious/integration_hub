// IntegrationHub.Sources.CEP.Udostepnianie.Validation/PytanieOHistorieLicznikaRequestValidator.cs
using IntegrationHub.Common.RequestValidation;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.RequestValidation
{
    internal sealed class PytanieOHistorieLicznikaRequestValidator : IRequestValidator<PytanieOHistorieLicznikaRequest>
    {
        public ValidationResult ValidateAndNormalize(PytanieOHistorieLicznikaRequest body)
        {
            body.IdentyfikatorSystemowyPojazdu =
                string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPojazdu)
                    ? null
                    : body.IdentyfikatorSystemowyPojazdu.Trim();

            if (string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPojazdu))
                return ValidationResult.Fail("Podaj 'identyfikatorSystemowyPojazdu'.");

            return ValidationResult.Ok();
        }
    }
}
