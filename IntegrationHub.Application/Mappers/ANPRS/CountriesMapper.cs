//csharp IntegrationHub.Application\Mappers\ANPRS\CountriesMapper.cs
using System.Collections.Generic;
using IntegrationHub.Application.Mappers;
using IntegrationHub.Sources.ANPRS.Contracts;

namespace IntegrationHub.Application.Mappers.ANPRS
{
    /// <summary>
    /// Mapper odpowiedzialny za przekszta³cenie odpowiedzi ANPRS typu CountriesResponse
    /// na domenow¹ reprezentacjê (lista kodów krajów).
    /// </summary>
    public static class CountriesMapper
    {
        /// <summary>
        /// Mapuje <see cref="CountriesResponse"/> na sekwencjê znormalizowanych kodów krajów.
        /// Normalizacja: Trim + ToUpperInvariant. Zwraca tylko niepuste kody.
        /// </summary>
        /// <param name="resp">OdpowiedŸ ANPRS zawieraj¹ca columnsNames + data.</param>
        /// <returns>IEnumerable z kodami krajów (string).</returns>
        public static IEnumerable<string> ToCountryCodes(CountriesResponse resp)
        {
            if (resp?.Data is null || resp.ColumnsNames is null) yield break;

            var cols = MapperHelpers.Index(resp.ColumnsNames);
            var iCode = MapperHelpers.FirstIndex(cols, "kraj", "country", "code", "countrycode", "iso3");

            foreach (var row in resp.Data)
            {
                var code = MapperHelpers.Get(row, iCode)?.Trim();
                if (!string.IsNullOrWhiteSpace(code))
                    yield return code.ToUpperInvariant();
            }
        }
    }
}