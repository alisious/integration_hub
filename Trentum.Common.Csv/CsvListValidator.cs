using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Trentum.Common.Csv;


/// <summary>
/// Podsumowanie walidacji CSV dla listy rocznej lub listy zwolnionych.
/// </summary>
/// <param name="HeaderRow">1-indeksowany numer wiersza nagłówka w pliku wejściowym.</param>
/// <param name="DataRowCount">Liczba wierszy danych (z pominięciem pustych) pod nagłówkiem.</param>
/// <param name="ValidRows">Liczba wierszy poprawnych.</param>
/// <param name="ErrorRows">Liczba wierszy z błędami.</param>
/// <param name="MissingHeaders">Lista brakujących nagłówków wymaganych do walidacji.</param>
/// <param name="PeselDuplicatesCount">Słownik: PESEL → liczba wystąpień, tylko dla zduplikowanych.</param>
public sealed record CsvValidationSummary(
    int HeaderRow,
    int DataRowCount,
    int ValidRows,
    int ErrorRows,
    IReadOnlyList<string> MissingHeaders,
    IReadOnlyDictionary<string, int> PeselDuplicatesCount
);

/// <summary>
/// Walidator plików CSV (separator <c>;</c>) dla:
/// <list type="bullet">
/// <item><description>listy rocznej (bez kolumny <c>Data zwolnienia</c>),</description></item>
/// <item><description>listy zwolnionych (z kolumną <c>Data zwolnienia</c> wymaganą).</description></item>
/// </list>
/// Zasady:
/// <list type="bullet">
/// <item><description>Kolumna <c>Nr etatu</c> nie jest wymagana i nie trafia do pliku wynikowego.</description></item>
/// <item><description>Wynik walidacji zwracany jest jako CSV z dodatkową kolumną <c>STATUS WALIDACJI</c>.</description></item>
/// <item><description>Dla listy zwolnionych nagłówki <c>Imię pierwsze</c> i <c>Imię drugie</c> są wymagane; wartość w <c>Imię drugie</c> może być pusta.</description></item>
/// <item><description>(Opcjonalnie) walidacja kolumny <c>Stopień</c> względem przekazanej listy stopni: <see cref="ValidateAnnualCsv"/> / <see cref="ValidateDischargedCsv"/> → <c>validateRank</c> + <c>validRanks</c>.</description></item>
/// <item><description>(Opcjonalnie) walidacja kolumny <c>Nazwa jednostki wojskowej</c> względem listy referencyjnej: <see cref="ValidateAnnualCsv"/> / <see cref="ValidateDischargedCsv"/> → <c>validateUnit</c> + <c>validUnits</c>.</description></item>
/// </list>
/// </summary>
public static class CsvListValidator
{
    private const string StatusHeader = "STATUS WALIDACJI";
    private static readonly CultureInfo Pl = CultureInfo.GetCultureInfo("pl-PL");

    // Aliasy nagłówków
    private static readonly Dictionary<string, string[]> HeaderAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Stopień"] = new[] { "Stopień", "Stopien" },
            ["Imiona"] = new[] { "Imiona" },

            ["Imię pierwsze"] = new[] { "Imię pierwsze", "Imie pierwsze", "Imie_pierwsze", "ImiePierwsze", "Imię" },
            ["Imię drugie"] = new[] { "Imię drugie", "Imie drugie", "Imie_drugie", "ImieDrugie", "Drugie imię" },

            ["Nazwisko"] = new[] { "Nazwisko" },
            ["PESEL"] = new[] { "PESEL" },
            ["Stanowisko"] = new[] { "Stanowisko" },
            ["Nr etatu"] = new[] { "Nr etatu", "Nr Etatu", "Nr_etatu", "NrEtatu" },
            ["Nazwa jednostki wojskowej"] = new[] { "Nazwa jednostki wojskowej", "Nazwa jednostki", "Jednostka" },
            ["Data zwolnienia"] = new[] { "Data zwolnienia", "Data Zwolnienia", "Zwolnienie", "DataZwolnienia" },
        };

    /// <summary>
    /// Wykonuje walidację listy rocznej (bez kolumny <c>Data zwolnienia</c>) i zwraca wynik jako CSV.
    /// </summary>
    public static byte[] ValidateAnnualCsv(
        Stream inputCsv,
        out CsvValidationSummary summary,
        int headerRow = 1,
        bool validatePesel = true,
        bool validatePeselDuplicates = true,
        bool validateRank = false,
        IEnumerable<string>? validRanks = null,
        bool validateUnit = true,
        IEnumerable<string>? validUnits = null)
    {
        var required = new[]
        {
            "Stopień", "Imiona", "Nazwisko", "PESEL", "Stanowisko", "Nazwa jednostki wojskowej"
        };

        var outputOrder = new[]
        {
            "Stopień", "Imiona", "Nazwisko", "PESEL", "Stanowisko", "Nazwa jednostki wojskowej", StatusHeader
        };

        return ValidateInternal(
            inputCsv,
            headerRow: headerRow,
            requiredHeaders: required,
            extraKeptHeaders: Array.Empty<string>(),
            dischargeDateRequired: false,
            validateDischargeDate: false,
            validatePesel: validatePesel,
            validatePeselDuplicates: validatePeselDuplicates,
            outputColumnOrder: outputOrder,
            out summary,
            valueOptionalColumns: null,
            validateRank: validateRank,
            validRanks: validRanks,
            validateUnit: validateUnit,
            validUnits: validUnits
        );
    }

    /// <summary>
    /// Waliduje listę <b>zwolnionych</b> w formacie CSV (separator <c>;</c>) i zwraca wynik jako CSV
    /// z dodatkową kolumną <c>STATUS WALIDACJI</c>.
    /// Kolumny wymagane: <c>Stopień</c>, <c>Imię pierwsze</c>, <c>Imię drugie</c> (nagłówek wymagany, wartość może być pusta),
    /// <c>Nazwisko</c>, <c>PESEL</c>, <c>Stanowisko</c>, <c>Nazwa jednostki wojskowej</c>, <c>Data zwolnienia</c>.
    /// Kolumna <c>Nr etatu</c> jest ignorowana.
    /// </summary>
    /// <param name="inputCsv">Strumień wejściowy CSV (BOM wykrywany automatycznie).</param>
    /// <param name="summary">Zwracane podsumowanie walidacji.</param>
    /// <param name="headerRow">1-indeksowany numer wiersza nagłówka (domyślnie 1).</param>
    /// <param name="validatePesel">Czy sprawdzać sumę kontrolną i datę w PESEL (domyślnie <c>true</c>).</param>
    /// <param name="validatePeselDuplicates">Czy raportować zduplikowane wartości PESEL (domyślnie <c>true</c>).</param>
    /// <param name="validateRank">
    /// Czy weryfikować kolumnę <c>Stopień</c> względem listy referencyjnej przekazanej w <paramref name="validRanks"/> (domyślnie <c>false</c>).
    /// Porównanie jest case-insensitive, z normalizacją spacji.
    /// </param>
    /// <param name="validRanks">Lista dozwolonych stopni (gdy <paramref name="validateRank"/> = <c>true</c>).</param>
    /// <param name="validateDischargeDate">
    /// Gdy <c>true</c> (domyślnie), kolumna <c>Data zwolnienia</c> musi zawierać <b>poprawną</b> datę w jednym z <b>dwóch</b> formatów:
    /// <list type="bullet">
    /// <item><description><c>rrrr-MM-dd</c> (np. <c>2025-09-30</c>)</description></item>
    /// <item><description><c>dd.MM.rrrr</c> (np. <c>30.09.2025</c>)</description></item>
    /// </list>
    /// Inne zapisy (np. <c>2025/09/30</c>, <c>30-09-2025</c>) są <b>niedozwolone</b>. Dodatkowo walidowana jest realność daty
    /// (np. <c>2024-02-29</c> ✔, <c>2023-02-29</c> ✖).
    /// </param>
    /// <param name="validateUnit">
    /// Czy weryfikować kolumnę <c>Nazwa jednostki wojskowej</c> względem listy referencyjnej przekazanej w
    /// <paramref name="validUnits"/> (domyślnie <c>true</c>). Porównanie jest case-insensitive, z normalizacją spacji.
    /// </param>
    /// <param name="validUnits">Lista dozwolonych nazw jednostek (gdy <paramref name="validateUnit"/> = <c>true</c>).</param>
    /// <returns>Bajty wynikowego pliku CSV (UTF-8 z BOM) z kolumną <c>STATUS WALIDACJI</c>.</returns>
    public static byte[] ValidateDischargedCsv(
        Stream inputCsv,
        out CsvValidationSummary summary,
        int headerRow = 1,
        bool validatePesel = true,
        bool validatePeselDuplicates = true,
        bool validateRank = false,
        IEnumerable<string>? validRanks = null,
        bool validateDischargeDate = true,
        bool validateUnit = true,
        IEnumerable<string>? validUnits = null)
    {
        var required = new[]
        {
            "Stopień",
            "Imię pierwsze",
            "Imię drugie", // nagłówek wymagany, wartość może być pusta
            "Nazwisko",
            "PESEL",
            "Stanowisko",
            "Nazwa jednostki wojskowej",
            "Data zwolnienia"
        };

        var outputOrder = new[]
        {
            "Stopień",
            "Imię pierwsze",
            "Imię drugie",
            "Nazwisko",
            "PESEL",
            "Stanowisko",
            "Nazwa jednostki wojskowej",
            "Data zwolnienia",
            StatusHeader
        };

        var optionalValueCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Imię drugie"
        };

        return ValidateInternal(
            inputCsv,
            headerRow: headerRow,
            requiredHeaders: required,
            extraKeptHeaders: new[] { "Data zwolnienia", "Imię pierwsze", "Imię drugie" },
            dischargeDateRequired: true,
            validateDischargeDate: validateDischargeDate,
            validatePesel: validatePesel,
            validatePeselDuplicates: validatePeselDuplicates,
            outputColumnOrder: outputOrder,
            out summary,
            valueOptionalColumns: optionalValueCols,
            validateRank: validateRank,
            validRanks: validRanks,
            validateUnit: validateUnit,
            validUnits: validUnits
        );
    }

    // ================= Silnik walidacji =================

    private static byte[] ValidateInternal(
        Stream inputCsv,
        int headerRow,
        string[] requiredHeaders,
        string[] extraKeptHeaders,
        bool dischargeDateRequired,
        bool validateDischargeDate,
        bool validatePesel,
        bool validatePeselDuplicates,
        string[] outputColumnOrder,
        out CsvValidationSummary summary,
        ISet<string>? valueOptionalColumns,
        bool validateRank,
        IEnumerable<string>? validRanks,
        bool validateUnit,
        IEnumerable<string>? validUnits)
    {
        valueOptionalColumns ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var rows = ReadCsv(inputCsv, ';');

        if (rows.Count == 0)
        {
            summary = new CsvValidationSummary(headerRow, 0, 0, 0, requiredHeaders, new Dictionary<string, int>());
            return EmitCsv(new[] { outputColumnOrder }, ';');
        }

        int headerIndex = headerRow - 1;
        if (headerIndex < 0 || headerIndex >= rows.Count)
            throw new InvalidDataException($"Nieprawidłowy wiersz nagłówka: {headerRow}");

        var header = rows[headerIndex];
        var headerMap = BuildHeaderMap(header);

        // Brakujące nagłówki
        var missing = new List<string>();
        foreach (var name in requiredHeaders)
        {
            if (!TryResolveColumn(headerMap, name, out _))
                missing.Add(name);
        }

        if (missing.Count > 0)
        {
            summary = new CsvValidationSummary(headerRow, 0, 0, 0, missing, new Dictionary<string, int>());
            var errRow = outputColumnOrder.Select(col =>
                col.Equals(StatusHeader, StringComparison.OrdinalIgnoreCase)
                    ? $"Brak nagłówków: {string.Join(", ", missing)}"
                    : "").ToArray();

            return EmitCsv(new[] { outputColumnOrder, errRow }, ';');
        }

        // Kolumny do wypisania (bez "Nr etatu")
        var printCols = new List<(string canonical, int index)>();
        foreach (var canonical in outputColumnOrder)
        {
            if (canonical.Equals(StatusHeader, StringComparison.OrdinalIgnoreCase))
                continue;

            if (TryResolveColumn(headerMap, canonical, out var idx))
            {
                if (canonical.Equals("Nr etatu", StringComparison.OrdinalIgnoreCase))
                    continue;
                printCols.Add((canonical, idx));
            }
        }

        // Zbiory dozwolonych wartości (stopnie / jednostki)
        HashSet<string>? allowedRanks = null;
        if (validateRank)
        {
            if (validRanks is not null)
                allowedRanks = new HashSet<string>(validRanks.Select(NormalizeKey), StringComparer.OrdinalIgnoreCase);
            else
                allowedRanks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        HashSet<string>? allowedUnits = null;
        if (validateUnit)
        {
            if (validUnits is not null)
                allowedUnits = new HashSet<string>(validUnits.Select(NormalizeKey), StringComparer.OrdinalIgnoreCase);
            else
                allowedUnits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        // Duplikaty PESEL
        var peselCounts = validatePeselDuplicates ? new Dictionary<string, int>(StringComparer.Ordinal) : null;

        int dataRowCount = 0;
        for (int r = headerIndex + 1; r < rows.Count; r++)
        {
            var row = rows[r];
            if (IsRowEmpty(row)) continue;
            dataRowCount++;

            if (validatePeselDuplicates)
            {
                var pesel = GetPesel(GetValue(row, headerMap, "PESEL"));
                if (pesel.Length > 0)
                    peselCounts![pesel] = peselCounts.TryGetValue(pesel, out var cnt) ? cnt + 1 : 1;
            }
        }

        var duplicatedPesels = (validatePeselDuplicates && peselCounts!.Count > 0)
            ? peselCounts.Where(kv => kv.Value > 1).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal)
            : new Dictionary<string, int>(StringComparer.Ordinal);

        // Budowa wyjścia
        var outRows = new List<string[]>();
        outRows.Add(outputColumnOrder);

        int valid = 0, errors = 0;

        for (int r = headerIndex + 1; r < rows.Count; r++)
        {
            var row = rows[r];
            if (IsRowEmpty(row)) continue;

            var rowErrors = new List<string>();

            // Wymagane wartości (z wyłączeniem kolumn, których wartości są opcjonalne)
            foreach (var name in requiredHeaders)
            {
                if (valueOptionalColumns.Contains(name))
                    continue;

                var val = GetValue(row, headerMap, name);
                if (string.IsNullOrWhiteSpace(val))
                    rowErrors.Add($"Brak wartości w kolumnie '{name}'");
            }

            // PESEL – walidacja
            var peselVal = GetPesel(GetValue(row, headerMap, "PESEL"));
            if (validatePesel && !string.IsNullOrWhiteSpace(peselVal))
            {
                if (!TryValidatePesel(peselVal, out var reason))
                    rowErrors.Add($"PESEL niepoprawny: {reason}");
            }

            if (validatePeselDuplicates && !string.IsNullOrWhiteSpace(peselVal))
            {
                if (duplicatedPesels.ContainsKey(peselVal))
                    rowErrors.Add("Zduplikowany PESEL");
            }

            // Data zwolnienia – ścisła walidacja formatów jeśli włączona
            if (validateDischargeDate)
            {
                var dz = GetValue(row, headerMap, "Data zwolnienia");
                if (!string.IsNullOrWhiteSpace(dz))
                {
                    if (!TryParseAllowedDateExact(dz, out _))
                        rowErrors.Add("Nieprawidłowa wartość w 'Data zwolnienia' (dozwolone formaty: rrrr-MM-dd lub dd.MM.rrrr).");
                }
                else if (dischargeDateRequired)
                {
                    // brak wartości – błąd zgłoszony już w sekcji „wymagane wartości”
                }
            }

            // Stopień – walidacja względem listy
            if (validateRank)
            {
                var rankRaw = GetValue(row, headerMap, "Stopień");
                if (!string.IsNullOrWhiteSpace(rankRaw))
                {
                    if (allowedRanks is null || allowedRanks.Count == 0)
                    {
                        rowErrors.Add("Brak listy dozwolonych stopni dla walidacji 'Stopień'.");
                    }
                    else
                    {
                        var normalized = NormalizeKey(rankRaw);
                        if (!allowedRanks.Contains(normalized))
                            rowErrors.Add($"Nieprawidłowa wartość w 'Stopień' (\"{rankRaw}\") – spoza listy dozwolonych.");
                    }
                }
            }

            // Nazwa jednostki wojskowej – walidacja względem listy
            if (validateUnit)
            {
                var unitRaw = GetValue(row, headerMap, "Nazwa jednostki wojskowej");
                if (!string.IsNullOrWhiteSpace(unitRaw))
                {
                    if (allowedUnits is null || allowedUnits.Count == 0)
                    {
                        rowErrors.Add("Brak listy dozwolonych jednostek dla walidacji 'Nazwa jednostki wojskowej'.");
                    }
                    else
                    {
                        var normalizedUnit = NormalizeKey(unitRaw);
                        if (!allowedUnits.Contains(normalizedUnit))
                            rowErrors.Add($"Nieprawidłowa wartość w 'Nazwa jednostki wojskowej' (\"{unitRaw}\") – spoza listy dozwolonych.");
                    }
                }
            }

            // Wiersz wynikowy
            var outRow = new List<string>(outputColumnOrder.Length);
            foreach (var colName in outputColumnOrder)
            {
                if (colName.Equals(StatusHeader, StringComparison.OrdinalIgnoreCase))
                {
                    outRow.Add(rowErrors.Count == 0 ? "POPRAWNY" : string.Join("; ", rowErrors));
                    continue;
                }

                if (TryResolveColumn(headerMap, colName, out var idx))
                {
                    if (colName.Equals("Nr etatu", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var raw = idx < row.Count ? row[idx] : "";
                    if (colName.Equals("PESEL", StringComparison.OrdinalIgnoreCase))
                        outRow.Add(GetPesel(raw));
                    else
                        outRow.Add(raw?.Trim() ?? "");
                }
                else
                {
                    outRow.Add("");
                }
            }

            if (rowErrors.Count == 0) valid++; else errors++;
            outRows.Add(outRow.ToArray());
        }

        summary = new CsvValidationSummary(headerRow, dataRowCount, valid, errors, Array.Empty<string>(), duplicatedPesels);
        return EmitCsv(outRows, ';');
    }

    // ================= Helpers: CSV, nagłówki, PESEL, daty, normalizacja =================

    private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> header)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Count; i++)
        {
            var name = (header[i] ?? "").Trim();
            if (name.Length > 0 && !map.ContainsKey(name))
                map[name] = i;
        }
        return map;
    }

    private static bool TryResolveColumn(Dictionary<string, int> headerMap, string canonicalName, out int index)
    {
        if (headerMap.TryGetValue(canonicalName, out index))
            return true;

        if (HeaderAliases.TryGetValue(canonicalName, out var aliases))
        {
            foreach (var a in aliases)
                if (headerMap.TryGetValue(a, out index))
                    return true;
        }

        index = -1;
        return false;
    }

    private static string GetValue(IReadOnlyList<string> row, Dictionary<string, int> headerMap, string canonicalName)
    {
        if (TryResolveColumn(headerMap, canonicalName, out var idx) && idx >= 0 && idx < row.Count)
            return row[idx]?.Trim() ?? "";
        return "";
    }

    private static bool IsRowEmpty(IReadOnlyList<string> row)
        => row.All(s => string.IsNullOrWhiteSpace(s));

    // Prosty parser CSV z obsługą cudzysłowów i separatora ';'
    private static List<List<string>> ReadCsv(Stream input, char sep)
    {
        using var sr = new StreamReader(
            input,
            DetectEncodingFromBOM(input) ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1 << 15,
            leaveOpen: true);

        var rows = new List<List<string>>();
        string? line;
        var sb = new StringBuilder();
        var fields = new List<string>();
        bool inQuotes = false;

        while ((line = sr.ReadLine()) != null)
        {
            int i = 0;
            while (i < line.Length)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i += 2; }
                        else { inQuotes = false; i++; }
                    }
                    else
                    {
                        sb.Append(c); i++;
                    }
                }
                else
                {
                    if (c == '"') { inQuotes = true; i++; }
                    else if (c == sep) { fields.Add(sb.ToString()); sb.Clear(); i++; }
                    else { sb.Append(c); i++; }
                }
            }

            if (inQuotes) { sb.AppendLine(); continue; }

            fields.Add(sb.ToString()); sb.Clear();
            rows.Add(new List<string>(fields));
            fields.Clear();
        }

        if (inQuotes)
        {
            fields.Add(sb.ToString()); sb.Clear();
            rows.Add(new List<string>(fields));
            fields.Clear();
        }

        try { input.Position = 0; } catch { }
        return rows;
    }

    private static Encoding? DetectEncodingFromBOM(Stream s)
    {
        long pos = s.CanSeek ? s.Position : 0;
        var preamble = new byte[4];
        int read = s.Read(preamble, 0, 4);
        if (s.CanSeek) s.Position = pos;

        if (read >= 3 && preamble[0] == 0xEF && preamble[1] == 0xBB && preamble[2] == 0xBF)
            return new UTF8Encoding(true);
        if (read >= 2 && preamble[0] == 0xFF && preamble[1] == 0xFE)
            return Encoding.Unicode; // UTF-16 LE
        if (read >= 2 && preamble[0] == 0xFE && preamble[1] == 0xFF)
            return Encoding.BigEndianUnicode; // UTF-16 BE
        return null;
    }

    private static readonly Encoding CsvUtf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private static byte[] EmitCsv(IEnumerable<string[]> rows, char sep)
    {
        using var ms = new MemoryStream(capacity: 1 << 16);
        // UTF-8 + BOM (EF BB BF)
        using (var writer = new StreamWriter(ms, CsvUtf8Bom, bufferSize: 1 << 12, leaveOpen: true))
        {
            foreach (var r in rows)
            {
                for (int i = 0; i < r.Length; i++)
                {
                    if (i > 0) writer.Write(sep);
                    writer.Write(QuoteIfNeeded(r[i] ?? "", sep));
                }
                writer.Write("\r\n");
            }
            writer.Flush();
        }
        return ms.ToArray();
    }

    private static string QuoteIfNeeded(string s, char sep)
    {
        bool needQuotes = s.Contains(sep) || s.Contains('"') || s.Contains('\r') || s.Contains('\n');
        if (!needQuotes) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    private static string GetPesel(string? raw)
    {
        var txt = (raw ?? "").Trim();
        if (txt.Length > 0 && txt.All(char.IsDigit) && txt.Length < 11)
            txt = txt.PadLeft(11, '0'); // zachowaj zera wiodące
        return txt;
    }

    private static bool TryValidatePesel(string pesel, out string reason)
    {
        reason = string.Empty;
        if (pesel.Length != 11 || !pesel.All(char.IsDigit)) { reason = "musi mieć 11 cyfr"; return false; }

        int[] w = { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };
        int sum = 0; for (int i = 0; i < 10; i++) sum += (pesel[i] - '0') * w[i];
        int check = (10 - (sum % 10)) % 10;
        if (check != (pesel[10] - '0')) { reason = "błędna suma kontrolna"; return false; }

        int year = (pesel[0] - '0') * 10 + (pesel[1] - '0');
        int month = (pesel[2] - '0') * 10 + (pesel[3] - '0');
        int day = (pesel[4] - '0') * 10 + (pesel[5] - '0');

        int century;
        if (month is >= 1 and <= 12) { century = 1900; }
        else if (month is >= 21 and <= 32) { century = 2000; month -= 20; }
        else if (month is >= 41 and <= 52) { century = 2100; month -= 40; }
        else if (month is >= 61 and <= 72) { century = 2200; month -= 60; }
        else if (month is >= 81 and <= 92) { century = 1800; month -= 80; }
        else { reason = "nieprawidłowy miesiąc w dacie"; return false; }

        try { _ = new DateTime(century + year, month, day); }
        catch { reason = "nieprawidłowa data urodzenia"; return false; }

        return true;
    }

    /// <summary>
    /// Ścisłe parsowanie daty – dozwolone tylko: "yyyy-MM-dd" lub "dd.MM.yyyy".
    /// </summary>
    private static bool TryParseAllowedDateExact(string? s, out DateTime dt)
    {
        dt = default;
        if (string.IsNullOrWhiteSpace(s)) return false;

        // usuń NBSP i przytnij
        var txt = s.Replace('\u00A0', ' ').Trim();

        var formats = new[] { "yyyy-MM-dd", "dd.MM.yyyy" };
        return DateTime.TryParseExact(
            txt,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out dt);
    }

    /// <summary>
    /// Normalizuje zapis kluczy tekstowych (stopnie/jednostki): przycina oraz redukuje wielokrotne spacje do pojedynczej.
    /// Nie zmienia diakrytyków ani znaków interpunkcyjnych/skrótów.
    /// </summary>
    private static string NormalizeKey(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var trimmed = s.Trim();
        return Regex.Replace(trimmed, @"\s+", " ");
    }

    /// <summary>Alias dla wstecznej kompatybilności — normalizacja stopnia.</summary>
    private static string NormalizeRank(string rank) => NormalizeKey(rank);
}
