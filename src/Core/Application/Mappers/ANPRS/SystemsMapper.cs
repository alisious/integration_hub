//csharp IntegrationHub.Application\Mappers\ANPRS\SystemsMapper.cs
using IntegrationHub.Domain.Contracts.ANPRS;
using IntegrationHub.Sources.ANPRS.Contracts;

namespace IntegrationHub.Application.Mappers.ANPRS
{
    /// <summary>
    /// Mapper dla kontraktu SystemsResponse oraz konwersji wyników DB na format grid ANPRS.
    /// </summary>
    public static class SystemsMapper
    {
        /// <summary>
        /// Mapuje <see cref="SystemsResponse"/> (ANPRS) na kolekcjê domenowych <see cref="SystemRowDto"/>.
        /// Normalizuje kod systemu (Trim + ToUpperInvariant).
        /// </summary>
        /// <param name="resp">OdpowiedŸ ANPRS z danymi systemów.</param>
        /// <returns>IEnumerable z domenowymi wierszami systemów.</returns>
        public static IEnumerable<SystemRowDto> ToSystems(SystemsResponse resp)
        {
            if (resp?.Data is null || resp.ColumnsNames is null) yield break;

            var cols = MapperHelpers.Index(resp.ColumnsNames);
            var iCode = MapperHelpers.FirstIndex(cols, "nazwa", "system", "systemcode", "code");
            var iDesc = MapperHelpers.FirstIndex(cols, "opis", "description", "desc");

            foreach (var row in resp.Data)
            {
                var code = MapperHelpers.Get(row, iCode)?.Trim();
                var desc = MapperHelpers.Get(row, iDesc)?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(code))
                    continue;

                yield return new SystemRowDto(
                    code.ToUpperInvariant(),
                    desc
                );
            }
        }

        /// <summary>
        /// Konwertuje kolekcjê domenowych <see cref="SystemRowDto"/> pobranych z DB
        /// na format <see cref="SystemsResponse"/> kompatybilny z uk³adem ANPRS (ColumnsNames + Data).
        /// Kolumny: "Nazwa","Opis".
        /// </summary>
        /// <param name="rows">Wiersze domenowe z DB.</param>
        /// <returns>SystemsResponse z wype³nionym ColumnsNames i Data.</returns>
        public static SystemsResponse ToSystemsResponseFromDb(IEnumerable<SystemRowDto> rows)
        {
            var resp = new SystemsResponse();
            resp.ColumnsNames.Add("Nazwa");
            resp.ColumnsNames.Add("Opis");

            foreach (var r in rows ?? Enumerable.Empty<SystemRowDto>())
            {
                var code = (r.SystemCode ?? string.Empty).Trim();
                var desc = (r.Description ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(code)) continue;

                resp.Data.Add(new List<string> { code, desc });
            }
            return resp;
        }
    }
}