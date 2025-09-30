using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Trentum.Common.Csv;

/// <summary>
/// Parser CSV → listy obiektów domenowych.
/// <list type="bullet">
/// <item><description>Lista roczna: wejście zawiera kolumnę <c>Imiona</c>, dzieloną na <c>Imię pierwsze</c>/<c>Imię drugie</c>.</description></item>
/// <item><description>Lista zwolnionych: wejście zawiera odrębne kolumny <c>Imię pierwsze</c> i <c>Imię drugie</c> (drugie może być puste).</description></item>
/// </list>
/// Kolumna <c>Nr etatu</c> jest ignorowana.
/// </summary>
public static class CsvPeopleParser
{
    /// <summary>Reprezentuje osobę z listy rocznej wczytaną z CSV.</summary>
    /// <param name="Stopien">Stopień wojskowy.</param>
    /// <param name="ImiePierwsze">Pierwsze imię.</param>
    /// <param name="ImieDrugie">Drugie imię (może być puste).</param>
    /// <param name="Nazwisko">Nazwisko.</param>
    /// <param name="PESEL">PESEL (z zachowaniem zer wiodących).</param>
    /// <param name="Stanowisko">Stanowisko służbowe.</param>
    /// <param name="NazwaJednostkiWojskowej">Nazwa jednostki wojskowej.</param>
    public sealed record Osoba(
        string Stopien,
        string ImiePierwsze,
        string ImieDrugie,
        string Nazwisko,
        string PESEL,
        string Stanowisko,
        string NazwaJednostkiWojskowej
    );

    /// <summary>Reprezentuje osobę z listy zwolnionych wczytaną z CSV.</summary>
    /// <param name="Stopien">Stopień wojskowy.</param>
    /// <param name="ImiePierwsze">Pierwsze imię.</param>
    /// <param name="ImieDrugie">Drugie imię (może być puste).</param>
    /// <param name="Nazwisko">Nazwisko.</param>
    /// <param name="PESEL">PESEL (z zachowaniem zer wiodących).</param>
    /// <param name="Stanowisko">Stanowisko służbowe.</param>
    /// <param name="NazwaJednostkiWojskowej">Nazwa jednostki wojskowej.</param>
    /// <param name="DataZwolnienia">Data zwolnienia (jeśli możliwa do zparsowania).</param>
    public sealed record OsobaZwolniona(
        string Stopien,
        string ImiePierwsze,
        string ImieDrugie,
        string Nazwisko,
        string PESEL,
        string Stanowisko,
        string NazwaJednostkiWojskowej,
        DateTime? DataZwolnienia
    );

    private static readonly CultureInfo Pl = CultureInfo.GetCultureInfo("pl-PL");

    // Aliasy nagłówków
    private static readonly Dictionary<string, string[]> HeaderAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Stopień"] = new[] { "Stopień", "Stopien" },
            ["Imiona"] = new[] { "Imiona" },

            ["Imię pierwsze"] = new[] { "Imię pierwsze", "Imie pierwsze", "Imie_pierwsze", "ImiePierwsze" },
            ["Imię drugie"] = new[] { "Imię drugie", "Imie drugie", "Imie_drugie", "ImieDrugie" },

            ["Nazwisko"] = new[] { "Nazwisko" },
            ["PESEL"] = new[] { "PESEL" },
            ["Stanowisko"] = new[] { "Stanowisko" },
            ["Nazwa jednostki wojskowej"] = new[]
            {
                "Nazwa jednostki wojskowej", "Nazwa jednostki", "Jednostka"
            },
            ["Data zwolnienia"] = new[] { "Data zwolnienia", "Data Zwolnienia", "Zwolnienie", "DataZwolnienia" },
            // "Nr etatu" – pomijamy
        };

    /// <summary>
    /// Parsuje listę roczną z CSV (separator <c>;</c>).
    /// Wymagane kolumny: <c>Stopień</c>, <c>Imiona</c>, <c>Nazwisko</c>, <c>PESEL</c>, <c>Stanowisko</c>, <c>Nazwa jednostki wojskowej</c>.
    /// Kolumna <c>Imiona</c> jest dzielona na <c>Imię pierwsze</c> i <c>Imię drugie</c>.
    /// </summary>
    /// <param name="csvStream">Strumień CSV (BOM wykrywany automatycznie).</param>
    /// <param name="headerRow">1-indeksowany numer wiersza nagłówka (domyślnie 1).</param>
    /// <returns>Lista obiektów <see cref="Osoba"/> z listy rocznej.</returns>
    /// <exception cref="InvalidDataException">Gdy brakuje wymaganych nagłówków.</exception>
    public static List<Osoba> ParseAnnualCsv(Stream csvStream, int headerRow = 1)
    {
        var rows = ReadCsv(csvStream, ';');
        if (rows.Count == 0) return new();

        int h = headerRow - 1;
        var header = rows[h];
        var map = BuildHeaderMap(header);

        int cStopien = Require(map, "Stopień");
        int cImiona = Require(map, "Imiona");
        int cNazw = Require(map, "Nazwisko");
        int cPesel = Require(map, "PESEL");
        int cStan = Require(map, "Stanowisko");
        int cNJW = Require(map, "Nazwa jednostki wojskowej");

        var list = new List<Osoba>();
        for (int r = h + 1; r < rows.Count; r++)
        {
            var row = rows[r];
            if (IsRowEmpty(row)) continue;

            string stopien = Get(row, cStopien);
            string imionaRaw = Get(row, cImiona);
            string nazwisko = Get(row, cNazw);
            string pesel = GetPesel(Get(row, cPesel));
            string stanowisko = Get(row, cStan);
            string njw = Get(row, cNJW);

            var (first, second) = SplitImiona(imionaRaw);

            // Pomiń całkiem puste osoby (np. brak nazwiska i peselu)
            if (string.IsNullOrWhiteSpace(nazwisko) && string.IsNullOrWhiteSpace(pesel))
                continue;

            list.Add(new Osoba(
                Stopien: stopien,
                ImiePierwsze: first,
                ImieDrugie: second,
                Nazwisko: nazwisko,
                PESEL: pesel,
                Stanowisko: stanowisko,
                NazwaJednostkiWojskowej: njw
            ));
        }

        return list;
    }

    /// <summary>
    /// Parsuje listę zwolnionych z CSV (separator <c>;</c>).
    /// Wymagane kolumny: <c>Stopień</c>, <c>Imię pierwsze</c>, <c>Imię drugie</c> (nagłówek), <c>Nazwisko</c>, <c>PESEL</c>, <c>Stanowisko</c>, <c>Nazwa jednostki wojskowej</c>, <c>Data zwolnienia</c>.
    /// Wartość w kolumnie <c>Imię drugie</c> może być pusta.
    /// </summary>
    /// <param name="csvStream">Strumień CSV (BOM wykrywany automatycznie).</param>
    /// <param name="headerRow">1-indeksowany numer wiersza nagłówka (domyślnie 1).</param>
    /// <returns>Lista obiektów <see cref="OsobaZwolniona"/> z listy zwolnionych.</returns>
    /// <exception cref="InvalidDataException">Gdy brakuje wymaganych nagłówków.</exception>
    public static List<OsobaZwolniona> ParseDischargedCsv(Stream csvStream, int headerRow = 1)
    {
        var rows = ReadCsv(csvStream, ';');
        if (rows.Count == 0) return new();

        int h = headerRow - 1;
        var header = rows[h];
        var map = BuildHeaderMap(header);

        int cStopien = Require(map, "Stopień");
        int cImie1 = Require(map, "Imię pierwsze");
        int cImie2 = Require(map, "Imię drugie"); // wartość może być pusta
        int cNazw = Require(map, "Nazwisko");
        int cPesel = Require(map, "PESEL");
        int cStan = Require(map, "Stanowisko");
        int cNJW = Require(map, "Nazwa jednostki wojskowej");
        int cDataZw = Require(map, "Data zwolnienia");

        var list = new List<OsobaZwolniona>();
        for (int r = h + 1; r < rows.Count; r++)
        {
            var row = rows[r];
            if (IsRowEmpty(row)) continue;

            string stopien = Get(row, cStopien);
            string imie1 = Get(row, cImie1);
            string imie2 = Get(row, cImie2); // może być puste
            string nazwisko = Get(row, cNazw);
            string pesel = GetPesel(Get(row, cPesel));
            string stanowisko = Get(row, cStan);
            string njw = Get(row, cNJW);
            string dataRaw = Get(row, cDataZw);

            DateTime? dataZwolnienia = TryParseDate(dataRaw, out var dz) ? dz : null;

            if (string.IsNullOrWhiteSpace(nazwisko) && string.IsNullOrWhiteSpace(pesel))
                continue;

            list.Add(new OsobaZwolniona(
                Stopien: stopien,
                ImiePierwsze: imie1,
                ImieDrugie: imie2 ?? "",
                Nazwisko: nazwisko,
                PESEL: pesel,
                Stanowisko: stanowisko,
                NazwaJednostkiWojskowej: njw,
                DataZwolnienia: dataZwolnienia
            ));
        }

        return list;
    }

    // ======= Wspólne helpers (niewidoczne publicznie) =======

    private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> header)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Count; i++)
        {
            var n = (header[i] ?? "").Trim();
            if (n.Length > 0 && !map.ContainsKey(n)) map[n] = i;
        }
        return map;
    }

    private static int Require(Dictionary<string, int> map, string canonical)
    {
        if (TryResolveColumn(map, canonical, out var idx)) return idx;
        throw new InvalidDataException($"Brakuje wymaganej kolumny: {canonical}");
    }

    private static bool TryResolveColumn(Dictionary<string, int> map, string canonical, out int index)
    {
        if (map.TryGetValue(canonical, out index)) return true;
        if (HeaderAliases.TryGetValue(canonical, out var aliases))
        {
            foreach (var a in aliases)
                if (map.TryGetValue(a, out index)) return true;
        }
        index = -1; return false;
    }

    private static List<List<string>> ReadCsv(Stream input, char sep)
    {
        using var sr = new StreamReader(
            input,
            DetectEncodingFromBOM(input) ?? new UTF8Encoding(false),
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
                    else { sb.Append(c); i++; }
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
            rows.Add(new List<string>(fields)); fields.Clear();
        }

        if (inQuotes)
        {
            fields.Add(sb.ToString()); sb.Clear();
            rows.Add(new List<string>(fields)); fields.Clear();
        }

        try { input.Position = 0; } catch { }
        return rows;
    }

    private static Encoding? DetectEncodingFromBOM(Stream s)
    {
        long pos = s.CanSeek ? s.Position : 0;
        var bom = new byte[4];
        int read = s.Read(bom, 0, 4);
        if (s.CanSeek) s.Position = pos;

        if (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) return new UTF8Encoding(true);
        if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE) return Encoding.Unicode; // UTF-16 LE
        if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF) return Encoding.BigEndianUnicode; // UTF-16 BE
        return null;
    }

    private static string Get(IReadOnlyList<string> row, int idx)
        => (idx >= 0 && idx < row.Count ? row[idx] : string.Empty)?.Trim() ?? string.Empty;

    private static bool IsRowEmpty(IReadOnlyList<string> row)
        => row.All(s => string.IsNullOrWhiteSpace(s));

    private static (string first, string second) SplitImiona(string imiona)
    {
        if (string.IsNullOrWhiteSpace(imiona)) return ("", "");
        var parts = imiona.Trim().Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return (parts[0], "");
        return (parts[0], parts[1]);
    }

    private static string GetPesel(string? raw)
    {
        var txt = (raw ?? "").Trim();
        if (txt.Length > 0 && txt.All(char.IsDigit) && txt.Length < 11)
            txt = txt.PadLeft(11, '0');
        return txt;
    }

    private static bool TryParseDate(string s, out DateTime dt)
    {
        if (DateTime.TryParse(s, Pl, DateTimeStyles.None, out dt)) return true;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)) return true;

        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
        {
            try { dt = DateTime.FromOADate(d); return true; } catch { }
        }

        dt = default;
        return false;
    }
}
