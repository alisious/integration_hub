//csharp IntegrationHub.Application\Mappers\MapperHelpers.cs
using System.Globalization;

namespace IntegrationHub.Application.Mappers;

/// <summary>
/// Ogólne metody pomocnicze dla mapperów (parsowanie siatek/kolumn oraz normalizacja).
/// Umieszczone w Application.Mappers, aby umo¿liwiæ ponowne u¿ycie we wszystkich mapperach aplikacji.
/// </summary>
internal static class MapperHelpers
{
    /// <summary>
    /// Buduje s³ownik mapuj¹cy znormalizowan¹ nazwê kolumny -> pierwszy indeks wyst¹pienia.
    /// Normalizacja: Trim() + ToLowerInvariant().
    /// Przydatne podczas parsowania odpowiedzi w uk³adzie grid (kolumny + wiersze), gdy nazwy kolumn mog¹ byæ zlokalizowane lub ró¿ne miêdzy Ÿród³ami.
    /// </summary>
    /// <param name="names">Sekwencja nazw kolumn w kolejnoœci (pole columnsNames z odpowiedzi zewnêtrznej).</param>
    /// <returns>S³ownik, gdzie klucz = znormalizowana nazwa kolumny, wartoœæ = pierwszy indeks tej kolumny.</returns>
    internal static Dictionary<string, int> Index(IEnumerable<string> names) =>
        names.Select((n, i) => new { n, i })
             .GroupBy(x => (x.n ?? string.Empty).Trim().ToLowerInvariant())
             .ToDictionary(g => g.Key, g => g.First().i);

    /// <summary>
    /// Zwraca pierwszy dopasowany indeks z s³ownika dla podanych kandydatów nazw kolumn.
    /// Klucze nale¿y podawaæ w preferowanej kolejnoœci (najbardziej prawdopodobny pierwszy).
    /// Zwraca -1, gdy ¿aden z kluczy nie zostanie znaleziony.
    /// </summary>
    /// <param name="dict">S³ownik znormalizowanych nazw kolumn na indeks (wynik <see cref="Index"/>).</param>
    /// <param name="keys">Kandydatów nazw kolumn (bez normalizacji) do wyszukania.</param>
    /// <returns>Indeks pierwszego dopasowanego klucza lub -1 jeœli brak.</returns>
    internal static int FirstIndex(Dictionary<string, int> dict, params string[] keys)
    {
        foreach (var k in keys)
            if (dict.TryGetValue((k ?? string.Empty).Trim().ToLowerInvariant(), out var i)) return i;
        return -1;
    }

    /// <summary>
    /// Bezpieczny dostêp do elementu w wierszu (IReadOnlyList&lt;string&gt;). Zwraca null, gdy indeks jest poza zakresem.
    /// </summary>
    /// <param name="row">Dane wiersza (lista wartoœci jako stringi).</param>
    /// <param name="i">Indeks do pobrania.</param>
    /// <returns>Wartoœæ pod indeksem lub null, jeœli indeks nieprawid³owy.</returns>
    internal static string? Get(IReadOnlyList<string> row, int i) =>
        i >= 0 && i < row.Count ? row[i] : null;

    /// <summary>
    /// Parsuje string do nullable decimal.
    /// Obs³uguje typowe formaty: najpierw próbuje invariant po zamianie przecinka na kropkê,
    /// nastêpnie próbuje kulturê "pl-PL" jeœli próba invariant zakoñczy siê niepowodzeniem.
    /// Zwraca null w przypadku nieudanego parsowania.
    /// </summary>
    /// <param name="s">Ciag wejœciowy (mo¿e zawieraæ przecinek lub kropkê).</param>
    /// <returns>Parsowany decimal lub null.</returns>
    internal static decimal? ParseDecimalNullable(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;

        var normalized = s.Replace(',', '.');
        if (decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            return v;

        if (decimal.TryParse(s, NumberStyles.Float, new CultureInfo("pl-PL"), out v))
            return v;

        return null;
    }

    /// <summary>
    /// Normalizuje tekstowe wartoœci przeznaczone do kodów domenowych:
    /// - przycina bia³e znaki,
    /// - konwertuje do wielkich liter (ToUpperInvariant),
    /// - zwraca null je¿eli wejœcie jest null/empty/whitespace.
    /// U¿ywaæ dla normalizacji kodów krajów/systemów itd., aby mieæ regu³ê w jednym miejscu.
    /// </summary>
    /// <param name="s">Wejœciowy string do normalizacji.</param>
    /// <returns>Znormalizowany string lub null.</returns>
    internal static string? Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return s.Trim().ToUpperInvariant();
    }
}