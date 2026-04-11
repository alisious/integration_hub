//csharp IntegrationHub.Application\Mappers\ANPRS\BcpMapper.cs

using IntegrationHub.Domain.Contracts.ANPRS;
using IntegrationHub.Sources.ANPRS.Contracts;

namespace IntegrationHub.Application.Mappers.ANPRS
{
    /// <summary>
    /// Mapper przekszta³caj¹cy odpowiedŸ ANPRS typu BCPResponse na domenowe DTO <see cref="BcpRowDto"/>.
    /// Zawiera regu³y parsowania i normalizacji pól takich jak code/system oraz parsowanie wartosci liczbowych.
    /// </summary>
    public static class BcpMapper
    {
        /// <summary>
        /// Mapuje <see cref="BCPResponse"/> na sekwencjê <see cref="BcpRowDto"/>.
        /// Pomija wiersze z brakuj¹cymi kluczowymi polami (id, kraj, system, nazwa).
        /// </summary>
        /// <param name="resp">OdpowiedŸ ANPRS z danymi BCP.</param>
        /// <returns>IEnumerable z domenowymi wierszami BCP.</returns>
        public static IEnumerable<BcpRowDto> ToBcp(BCPResponse resp)
        {
            if (resp?.Data is null || resp.ColumnsNames is null) yield break;

            var cols = MapperHelpers.Index(resp.ColumnsNames);
            var iBcpId = MapperHelpers.FirstIndex(cols, "bcpid", "id", "bcp", "identifier");
            var iCountry = MapperHelpers.FirstIndex(cols, "kraj", "country", "countrycode", "iso3", "code");
            var iSystem = MapperHelpers.FirstIndex(cols, "system", "systemcode");
            var iName = MapperHelpers.FirstIndex(cols, "nazwa", "name", "label", "bcpname");
            var iType = MapperHelpers.FirstIndex(cols, "typ", "type");
            var iLat = MapperHelpers.FirstIndex(cols, "lat", "latitude", "szerokosc", "szerokosc_geograficzna");
            var iLng = MapperHelpers.FirstIndex(cols, "lng", "longitude", "dlugosc", "dlugosc_geograficzna");

            foreach (var row in resp.Data)
            {
                var bcpId = MapperHelpers.Get(row, iBcpId)?.Trim();
                var country = MapperHelpers.Get(row, iCountry)?.Trim();
                var system = MapperHelpers.Get(row, iSystem)?.Trim();
                var name = MapperHelpers.Get(row, iName)?.Trim();
                var type = iType >= 0 ? MapperHelpers.Get(row, iType)?.Trim() : null;

                if (string.IsNullOrWhiteSpace(bcpId) ||
                    string.IsNullOrWhiteSpace(country) ||
                    string.IsNullOrWhiteSpace(system) ||
                    string.IsNullOrWhiteSpace(name))
                    continue;

                decimal? lat = MapperHelpers.ParseDecimalNullable(MapperHelpers.Get(row, iLat));
                decimal? lng = MapperHelpers.ParseDecimalNullable(MapperHelpers.Get(row, iLng));

                yield return new BcpRowDto(
                    bcpId,
                    country.ToUpperInvariant(),
                    system.ToUpperInvariant(),
                    name,
                    string.IsNullOrWhiteSpace(type) ? null : type,
                    lat, lng
                );
            }
        }
    }
}