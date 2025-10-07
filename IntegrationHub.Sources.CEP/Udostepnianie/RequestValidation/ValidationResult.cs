// IntegrationHub.Sources.CEP.Udostepnianie.Validation/ValidationResult.cs
namespace IntegrationHub.Sources.CEP.Udostepnianie.RequestValidation
{
    public sealed class ValidationResult
    {
        public bool IsValid { get; }
        public string? MessageError { get; }

        private ValidationResult(bool ok, string? msg)
        {
            IsValid = ok;
            MessageError = msg;
        }

        public static ValidationResult Ok() => new(true, null);
        public static ValidationResult Fail(string messageError) => new(false, messageError);
    }

    
}
