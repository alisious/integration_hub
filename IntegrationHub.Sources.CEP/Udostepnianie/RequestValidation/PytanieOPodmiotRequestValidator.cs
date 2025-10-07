// IntegrationHub.Sources.CEP.Udostepnianie.Validation/PytanieOPodmiotRequestValidator.cs
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.RequestValidation
{
    /// <summary>
    /// Waliduje i normalizuje PytanieOPodmiotRequest.
    /// Reguły minimalne:
    /// 1) Wymagane: identyfikatorSystemowyPodmiotu (trim; niepuste).
    /// </summary>
    internal sealed class PytanieOPodmiotRequestValidator : IRequestValidator<PytanieOPodmiotRequest>
    {
        public ValidationResult ValidateAndNormalize(PytanieOPodmiotRequest body)
        {
            body.IdentyfikatorSystemowyPodmiotu =
                string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPodmiotu)
                    ? null
                    : body.IdentyfikatorSystemowyPodmiotu.Trim();

            if (string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPodmiotu))
                return ValidationResult.Fail("Podaj 'identyfikatorSystemowyPodmiotu'.");

            return ValidationResult.Ok();
        }
    }
}
