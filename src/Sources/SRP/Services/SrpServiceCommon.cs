using System.Net;
using System.Text.Json;
using IntegrationHub.Common.Helpers;
using IntegrationHub.Common.Primitives;
using IntegrationHub.SRP.Contracts;

namespace IntegrationHub.SRP.Services;

internal static class SrpServiceCommon
{
    // Jedne opcje JSON dla całego projektu (deserializacja plików testowych)
    internal static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    // Normalizacja do porównań tekstowych
    internal static string N(string? s) => (s ?? string.Empty).Trim().ToUpperInvariant();

    /// <summary>
    /// Sprawdza: PESEL lub (Nazwisko+Imię). Normalizuje daty do yyyyMMdd.
    /// Jeśli <paramref name="allowRange"/> = true, normalizuje też DataUrodzeniaOd/Do.
    /// W razie błędu ustawia <paramref name="error"/> i zwraca false.
    /// </summary>
    internal static bool TryValidateAndNormalize(
        SearchPersonRequest body,
        string _,
        bool allowRange,
        out Error? error)
    {
        error = null;

        var hasPesel = !string.IsNullOrWhiteSpace(body.Pesel);
        var hasNamePair = !string.IsNullOrWhiteSpace(body.Nazwisko)
                       && !string.IsNullOrWhiteSpace(body.ImiePierwsze);

        if (!hasPesel && !hasNamePair)
        {
            error = ErrorFactory.BusinessError(ErrorCodeEnum.ValidationError, "Obowiazkowo podaj PESEL albo zestaw: nazwisko i imie.", (int)HttpStatusCode.BadRequest);
            return false;
        }

        if (!string.IsNullOrWhiteSpace(body.DataUrodzenia))
        {
            var formatted = DateStringFormatHelper.FormatYyyyMmDd(body.DataUrodzenia);
            if (formatted is null)
            {
                error = ErrorFactory.BusinessError(ErrorCodeEnum.ValidationError, "Niepoprawny format parametru dataUrodzenia. Wymagany format: yyyyMMdd lub yyyy-MM-dd.", (int)HttpStatusCode.BadRequest);
                return false;
            }
            body.DataUrodzenia = formatted;
        }

        if (allowRange)
        {
            if (!string.IsNullOrWhiteSpace(body.DataUrodzeniaOd))
            {
                var formatted = DateStringFormatHelper.FormatYyyyMmDd(body.DataUrodzeniaOd);
                if (formatted is null)
                {
                    error = ErrorFactory.BusinessError(ErrorCodeEnum.ValidationError, "Niepoprawny format parametru dataUrodzeniaOd. Wymagany format: yyyyMMdd lub yyyy-MM-dd.", (int)HttpStatusCode.BadRequest);
                    return false;
                }
                body.DataUrodzeniaOd = formatted;
            }

            if (!string.IsNullOrWhiteSpace(body.DataUrodzeniaDo))
            {
                var formatted = DateStringFormatHelper.FormatYyyyMmDd(body.DataUrodzeniaDo);
                if (formatted is null)
                {
                    error = ErrorFactory.BusinessError(ErrorCodeEnum.ValidationError, "Niepoprawny format parametru dataUrodzeniaDo. Wymagany format: yyyyMMdd lub yyyy-MM-dd.", (int)HttpStatusCode.BadRequest);
                    return false;
                }
                body.DataUrodzeniaDo = formatted;
            }
        }

        return true;
    }
}
