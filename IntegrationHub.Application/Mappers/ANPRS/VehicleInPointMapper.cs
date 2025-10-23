using System;
using System.Collections.Generic;
using System.Globalization;
using IntegrationHub.Domain.Contracts.ANPRS;
using IntegrationHub.Sources.ANPRS.Contracts;

namespace IntegrationHub.Application.Mappers.ANPRS
{
    /// <summary>
    /// Mapuje wiersz/odpowiedź typu <see cref="VehiclesInPointResponse"/> na <see cref="VehicleInPointDto"/>.
    /// Używa pomocniczych metod z <c>MapperHelpers</c> (Index, FirstIndex, Get, ParseDecimalNullable, Normalize).
    /// </summary>
    internal static class VehicleInPointMapper
    {
        /// <summary>
        /// Mapuje całą odpowiedź gridową na kolekcję DTO.
        /// Zwraca pustą kolekcję, gdy dane są puste.
        /// </summary>
        internal static IEnumerable<VehicleInPointDto> Map(VehiclesInPointResponse? response)
        {
            if (response is null) yield break;
            var index = MapperHelpers.Index(response.ColumnsNames);

            foreach (var row in response.Data)
            {
                var dto = MapRow(row, index);
                if (dto is not null) yield return dto;
            }
        }

        /// <summary>
        /// Mapuje pojedynczy wiersz (lista stringów) do DTO. Zwraca null gdy wiersz jest null/za krótki.
        /// </summary>
        private static VehicleInPointDto? MapRow(IReadOnlyList<string>? row, Dictionary<string, int> index)
        {
            if (row is null) return null;

            // Candidate keys - uwzględniamy polskie i angielskie warianty oraz skróty
            var idIdx = MapperHelpers.FirstIndex(index, "Identyfikator", "Id", "Identifier", "identyfikator");
            var numberIdx = MapperHelpers.FirstIndex(index, "Numer", "Number", "Nr", "nr");
            var dateIdx = MapperHelpers.FirstIndex(index, "Data zdarzenia", "Data zdarzenia", "Date", "EventDate", "Data");
            var typeIdx = MapperHelpers.FirstIndex(index, "Typ pojazdu", "Typ", "Vehicle type", "VehicleType");
            var locationIdx = MapperHelpers.FirstIndex(index, "Lokalizacja nr.", "Lokalizacja", "Location", "Position");
            var countryIdx = MapperHelpers.FirstIndex(index, "Kraj poj.", "Kraj", "Country", "Country code");
            var adrIdx = MapperHelpers.FirstIndex(index, "ADR", "Adr");
            var bcpIdx = MapperHelpers.FirstIndex(index, "BCP", "Bcp");
            var latIdx = MapperHelpers.FirstIndex(index, "Latitude", "Lat", "latitude", "lat");
            var lonIdx = MapperHelpers.FirstIndex(index, "Longitude", "Lon", "longitude", "lon");

            // Safe getters
            string? Get(int i) => MapperHelpers.Get(row, i);

            var dto = new VehicleInPointDto();

            // Id
            var idStr = Get(idIdx);
            if (!string.IsNullOrWhiteSpace(idStr) && Guid.TryParse(idStr, out var guid)) dto.Id = guid;
            else dto.Id = Guid.Empty;

            // Number
            dto.Number = Get(numberIdx) ?? string.Empty;

            // EventDate parsing: prefer exact "yyyy-MM-dd HH:mm:ss" then fallback to general parse (Invariant + current culture)
            var dateStr = Get(dateIdx) ?? string.Empty;
            if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt)
                && !DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt)
                && !DateTime.TryParse(dateStr, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dt))
            {
                dt = DateTime.MinValue;
            }
            dto.EventDate = dt;

            // Other textual fields
            dto.VehicleType = Get(typeIdx) ?? string.Empty;
            dto.Location = Get(locationIdx) ?? string.Empty;
            dto.Country = Get(countryIdx) ?? string.Empty;
            dto.Adr = Get(adrIdx) ?? string.Empty;
            dto.Bcp = Get(bcpIdx) ?? string.Empty;

            // Latitude / Longitude: użyj ParseDecimalNullable z MapperHelpers, konwertuj do double
            var latDec = MapperHelpers.ParseDecimalNullable(Get(latIdx));
            var lonDec = MapperHelpers.ParseDecimalNullable(Get(lonIdx));
            dto.Latitude = latDec.HasValue ? (double)latDec.Value : 0.0;
            dto.Longitude = lonDec.HasValue ? (double)lonDec.Value : 0.0;

            return dto;
        }
    }
}