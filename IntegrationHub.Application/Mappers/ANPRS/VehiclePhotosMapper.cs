using System.Collections.Generic;
using IntegrationHub.Sources.ANPRS.Contracts;

namespace IntegrationHub.Application.Mappers.ANPRS
{
    /// <summary>
    /// Mapuje odpowiedź ANPRS typu PhotoResponse (grid: columnsNames + data)
    /// na kolekcję VehiclePhotoRowDto.
    /// Używa wyłącznie kolumn "Zdjęcia" i opcjonalnie "Położenie numeru".
    /// </summary>
    internal static class VehiclePhotosMapper
    {
        internal static IEnumerable<VehiclePhotoRowDto> Map(PhotoResponse? response)
        {
            if (response is null)
                yield break;

            var index = MapperHelpers.Index(response.ColumnsNames);
            var iPhoto = MapperHelpers.FirstIndex(index, "Zdjęcia");
            var iPos = MapperHelpers.FirstIndex(index, "Położenie numeru");

            if (iPhoto < 0)
                yield break;

            foreach (var row in response.Data)
            {
                var photo = MapperHelpers.Get(row, iPhoto);
                if (string.IsNullOrWhiteSpace(photo))
                    continue;

                string? pos = iPos >= 0 ? MapperHelpers.Get(row, iPos)?.Trim() : null;

                yield return new VehiclePhotoRowDto
                {
                    Zdjecie = photo.Trim(),
                    PolozenieNumeru = string.IsNullOrWhiteSpace(pos) ? null : pos
                };
            }
        }
    }
}
