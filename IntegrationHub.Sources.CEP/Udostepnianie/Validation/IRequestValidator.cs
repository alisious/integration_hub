using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Validation
{
    public interface IRequestValidator<TRequest>
    {
        /// <summary>
        /// Zwraca OK lub błąd z tylko MessageError. Normalizacja może modyfikować <paramref name="body"/>.
        /// </summary>
        ValidationResult ValidateAndNormalize(TRequest body);
    }
}
