// IntegrationHub.Sources.CEP.Udostepnianie.Validation/PytanieODokumentPojazduRequestValidator.cs
using IntegrationHub.Common.RequestValidation;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.RequestValidation
{
    internal sealed class PytanieODokumentPojazduRequestValidator : IRequestValidator<PytanieODokumentPojazduRequest>
    {
        private static string N(string? s) => (s ?? string.Empty).Trim().ToUpperInvariant();

        public ValidationResult ValidateAndNormalize(PytanieODokumentPojazduRequest body)
        {
            // Normalizacja
            body.IdentyfikatorSystemowyDokumentuPojazdu =
                string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyDokumentuPojazdu)
                    ? null : body.IdentyfikatorSystemowyDokumentuPojazdu.Trim();

            body.TypDokumentu = string.IsNullOrWhiteSpace(body.TypDokumentu) ? "DICT155_DR" : N(body.TypDokumentu);
            body.DokumentSeriaNumer = string.IsNullOrWhiteSpace(body.DokumentSeriaNumer) ? null : N(body.DokumentSeriaNumer);

            // Kryteria minimalne
            var byId = !string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyDokumentuPojazdu);
            var byTypeAndNo = !string.IsNullOrWhiteSpace(body.TypDokumentu) && !string.IsNullOrWhiteSpace(body.DokumentSeriaNumer);

            if (!(byId || byTypeAndNo))
                return ValidationResult.Fail("Podaj 'identyfikatorSystemowyDokumentuPojazdu' lub parę: 'typDokumentu' + 'dokumentSeriaNumer'.");

            return ValidationResult.Ok();
        }
    }
}
