using System;
using System.Collections.Generic;
using System.Globalization;
using IntegrationHub.Domain.Contracts.ANPRS;
using IntegrationHub.Sources.ANPRS.Contracts;

namespace IntegrationHub.Application.Mappers.ANPRS
{
    /// <summary>
    /// Mapuje odpowiedź <see cref="LicensePlateReportResponse"/> (grid: columnsNames + data)
    /// na kolekcję <see cref="LicenseplateDto"/>. Używa pomocniczych metod z MapperHelpers.
    /// </summary>
    internal static class LicenseplateMapper
    {
        internal static IEnumerable<LicenseplateDto> Map(LicensePlateReportResponse? response)
        {
            if (response is null) yield break;

            var index = MapperHelpers.Index(response.ColumnsNames);

            foreach (var row in response.Data)
            {
                var dto = MapRow(row, index);
                if (dto is not null) yield return dto;
            }
        }

        private static LicenseplateDto? MapRow(IReadOnlyList<string>? row, Dictionary<string, int> index)
        {
            if (row is null) return null;

            // candidate keys (polish / english variants)
            var idIdx = MapperHelpers.FirstIndex(index, "Identyfikator", "Id", "Identifier");
            var numberIdx = MapperHelpers.FirstIndex(index, "Numer", "Number", "Nr");
            var typeIdx = MapperHelpers.FirstIndex(index, "Typ pojazdu", "Typ", "Vehicle type", "VehicleType");
            var locationIdx = MapperHelpers.FirstIndex(index, "Lokalizacja nr.", "Lokalizacja", "Location", "Position");
            var countryIdx = MapperHelpers.FirstIndex(index, "Kraj poj.", "Kraj", "Country");
            var dateIdx = MapperHelpers.FirstIndex(index, "Data zdarzenia", "Data", "Date", "EventDate");
            var bcpCountryIdx = MapperHelpers.FirstIndex(index, "Kraj BCP", "KrajBcp", "Bcp Country", "bcpCountry");
            var bcpIdx = MapperHelpers.FirstIndex(index, "BCP", "Bcp");
            var latIdx = MapperHelpers.FirstIndex(index, "Latitude", "Lat", "latitude", "lat");
            var lonIdx = MapperHelpers.FirstIndex(index, "Longitude", "Lon", "longitude", "lon");

            static string? Get(IReadOnlyList<string> r, int i) => MapperHelpers.Get(r, i);

            var dto = new LicenseplateDto();

            // Id
            var idStr = Get(row, idIdx);
            if (!string.IsNullOrWhiteSpace(idStr) && Guid.TryParse(idStr, out var guid)) dto.Id = guid;
            else dto.Id = Guid.Empty;

            // Simple strings
            dto.Number = Get(row, numberIdx) ?? string.Empty;
            dto.VehicleType = Get(row, typeIdx) ?? string.Empty;
            dto.Location = Get(row, locationIdx) ?? string.Empty;
            dto.Country = Get(row, countryIdx) ?? string.Empty;

            // Date parsing
            var dateStr = Get(row, dateIdx) ?? string.Empty;
            if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt)
                && !DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt)
                && !DateTime.TryParse(dateStr, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dt))
            {
                dt = DateTime.MinValue;
            }
            dto.EventDate = dt;

            // BCP fields
            dto.BcpCountry = Get(row, bcpCountryIdx) ?? string.Empty;
            dto.Bcp = Get(row, bcpIdx) ?? string.Empty;

            // Latitude / Longitude (use ParseDecimalNullable from MapperHelpers)
            var latDec = MapperHelpers.ParseDecimalNullable(Get(row, latIdx));
            var lonDec = MapperHelpers.ParseDecimalNullable(Get(row, lonIdx));
            dto.Latitude = latDec.HasValue ? (double)latDec.Value : 0.0;
            dto.Longitude = lonDec.HasValue ? (double)lonDec.Value : 0.0;

            return dto;
        }
    }
}