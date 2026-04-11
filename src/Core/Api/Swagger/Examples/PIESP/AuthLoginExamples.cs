
using Swashbuckle.AspNetCore.Filters;

namespace IntegrationHub.Api.Swagger.Examples.PIESP
{
    public sealed class Login401Example : IExamplesProvider<string>
    {
        public string GetExamples() =>
            "Nie udało się zalogować. Sprawdź numer odznaki i PIN i spróbuj ponownie.";
    }

    public sealed class Login403Example : IExamplesProvider<string>
    {
        public string GetExamples() =>
           "Konto jest zablokowane lub nieaktywne. Skontaktuj się z przełożonym.";
    }
}
