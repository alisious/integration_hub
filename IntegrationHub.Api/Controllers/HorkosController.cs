using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using Trentum.Common.Csv;
using Trentum.Horkos;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using System.Text.RegularExpressions;

namespace IntegrationHub.Api.Controllers;

[ApiController]
[Route("tools/horkos")]
[ApiExplorerSettings(GroupName = "v1")]
public class HorkosController : ControllerBase
{
    private readonly IHorkosDictionaryService _dictService;
    private readonly ILogger<HorkosController> _log;
    private readonly IObligationsService _obligationsService;
    private readonly string _exportDir;

    public HorkosController(
        IHorkosDictionaryService dictService,
        ILogger<HorkosController> log,
        IObligationsService obligationsService,
        IWebHostEnvironment env)
    {
        _dictService = dictService;
        _log = log;
        _obligationsService = obligationsService;

        // Katalog docelowy: <ContentRoot>/App_Data/Horkos/Exports/
        _exportDir = Path.Combine(env.ContentRootPath, "App_Data", "Horkos", "Exports");
        Directory.CreateDirectory(_exportDir);
    }

    /// <summary>
    /// Walidacja listy ZWOLNIONYCH z pliku CSV (separator ';').
    /// Opcjonalnie: validateRank=true — weryfikuje „Stopień”; validateUnit=true — weryfikuje „Nazwa jednostki wojskowej”
    /// względem słowników referencyjnych. Zwraca CSV (UTF-8 z BOM) i zapisuje kopię pliku na dysk.
    /// </summary>
    [HttpPost("validate-discharged-list")]
    [Consumes("multipart/form-data")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 50_000_000)]
    [SwaggerOperation(
        Summary = "Walidacja listy zwolnionych (CSV).",
        Description = "Wynik: CSV z kolumną STATUS WALIDACJI. Przełączniki: validateRank (Stopień), validateUnit (Nazwa jednostki wojskowej).",
        OperationId = "Tools_ValidateDischargedList_Csv",
        Tags = new[] { "Horkos CSV" }
    )]
    public async Task<IActionResult> ValidateDischargedList([FromForm] ValidateCsvRequest request, CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest("Brak pliku lub plik pusty.");

        // (opcjonalnie) słowniki referencyjne
        IReadOnlyList<string>? ranks = null;
        if (request.ValidateRank == true)
            ranks = await _dictService.GetRankReferenceListAsync(ct); // stopnie

        IReadOnlyList<string>? units = null;
        if (request.ValidateUnit == true)
            units = await _dictService.GetUnitNameReferenceListAsync(ct); // nazwy jednostek :contentReference[oaicite:2]{index=2}

        await using var inMs = new MemoryStream();
        await request.File.CopyToAsync(inMs, ct);
        inMs.Position = 0;

        var bytes = CsvListValidator.ValidateDischargedCsv(
            inputCsv: inMs,
            summary: out var _,
            headerRow: request.HeaderRow ?? 1,
            validatePesel: true,
            validatePeselDuplicates: true,
            validateRank: request.ValidateRank == true,
            validRanks: ranks,
            validateDischargeDate: true,
            validateUnit: request.ValidateUnit == true,
            validUnits: units
        );

        // dopnij BOM UTF-8 jeśli z jakiegoś powodu go nie ma
        bytes = EnsureUtf8Bom(bytes);

        var baseName = SafeBaseName(null, "lista_zwolnionych");
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        // Zapis na dysk
        var savedPath = await SaveCsvToDiskAsync(bytes, baseName, ct);
        Response.Headers["X-Saved-File"] = savedPath;

        // anty-cache:
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
        // nagłówek diagnostyczny (pierwsze 3 bajty)
        Response.Headers["X-Debug-First3"] = Convert.ToHexString(bytes.AsSpan(0, Math.Min(bytes.Length, 3)));

        return File(bytes, "application/octet-stream", $"{baseName}_{stamp}_WYNIK.csv");
    }

    /// <summary>
    /// Walidacja ROCZNEJ listy z pliku CSV (separator ';').
    /// Opcjonalnie: validateRank=true — weryfikuje „Stopień”; validateUnit=true — weryfikuje „Nazwa jednostki wojskowej”
    /// względem słowników referencyjnych. Zwraca CSV (UTF-8 z BOM) i zapisuje kopię pliku na dysk.
    /// </summary>
    [HttpPost("validate-annual-list")]
    [Consumes("multipart/form-data")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 50_000_000)]
    [SwaggerOperation(
        Summary = "Walidacja rocznej listy (CSV).",
        Description = "Wynik: CSV z kolumną STATUS WALIDACJI. Przełączniki: validateRank (Stopień), validateUnit (Nazwa jednostki wojskowej).",
        OperationId = "Tools_ValidateAnnualList_Csv",
        Tags = new[] { "Horkos CSV" }
    )]
    public async Task<IActionResult> ValidateAnnualList([FromForm] ValidateCsvRequest request, CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest("Brak pliku lub plik pusty.");

        // (opcjonalnie) słowniki referencyjne
        IReadOnlyList<string>? ranks = null;
        if (request.ValidateRank == true)
            ranks = await _dictService.GetRankReferenceListAsync(ct); // stopnie

        IReadOnlyList<string>? units = null;
        if (request.ValidateUnit == true)
            units = await _dictService.GetUnitNameReferenceListAsync(ct); // nazwy jednostek :contentReference[oaicite:3]{index=3}

        await using var inMs = new MemoryStream();
        await request.File.CopyToAsync(inMs, ct);
        inMs.Position = 0;

        var bytes = CsvListValidator.ValidateAnnualCsv(
            inputCsv: inMs,
            summary: out var _,
            headerRow: request.HeaderRow ?? 1,
            validatePesel: true,
            validatePeselDuplicates: true,
            validateRank: request.ValidateRank == true,
            validRanks: ranks,
            validateUnit: request.ValidateUnit == true,
            validUnits: units
        );

        // dopnij BOM UTF-8 jeśli z jakiegoś powodu go nie ma
        bytes = EnsureUtf8Bom(bytes);

        var baseName = SafeBaseName(null, "lista_roczna");
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        // Zapis na dysk
        var savedPath = await SaveCsvToDiskAsync(bytes, baseName, ct);
        Response.Headers["X-Saved-File"] = savedPath;

        // anty-cache:
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
        // nagłówek diagnostyczny (pierwsze 3 bajty)
        Response.Headers["X-Debug-First3"] = Convert.ToHexString(bytes.AsSpan(0, Math.Min(bytes.Length, 3)));

        return File(bytes, "application/octet-stream", $"{baseName}_{stamp}_WYNIK.csv");
    }

    [HttpPost("import-annual-list")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(200_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
    [SwaggerOperation(
    Summary = "Import rocznej listy (CSV) do dbo.ZobowiazaniaRoczne",
    Description = "Czyści dane dla wskazanej listy (HorkosListId), następnie ładuje CSV bulkiem. Kolumny zgodne z parserem Annual.",
    OperationId = "Tools_ImportAnnualList_Csv",
    Tags = new[] { "Horkos CSV" }
)]
    public async Task<IActionResult> ImportAnnualList([FromForm] ImportAnnualCsvRequest request, CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest("Brak pliku lub plik pusty.");

        await using var ms = new MemoryStream();
        await request.File.CopyToAsync(ms, ct);
        ms.Position = 0;

        var inserted = await _obligationsService.ImportAnnualList(ms, request.HorkosListId, request.Rok, ct);

        _log.LogInformation("Annual import done. HorkosListId={Id}, Rok={Rok}, Inserted={Rows}",
            request.HorkosListId, request.Rok, inserted);

        return Ok(new { inserted });
    }

    [HttpPost("import-discharged-list")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(200_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
    [SwaggerOperation(
    Summary = "Import listy zwolnionych (CSV) do dbo.ZobowiazaniaPoZwolnieniu",
    Description = "Czyści dane dla wskazanej listy (HorkosListId), następnie ładuje CSV bulkiem. Kolumny zgodne z parserem Discharged.",
    OperationId = "Tools_ImportDischargedList_Csv",
    Tags = new[] { "Horkos CSV" }
)]
    public async Task<IActionResult> ImportDischargedList([FromForm] ImportDischargedCsvRequest request, CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest("Brak pliku lub plik pusty.");

        await using var ms = new MemoryStream();
        await request.File.CopyToAsync(ms, ct);
        ms.Position = 0;

        var inserted = await _obligationsService.ImportDischargedList(ms, request.HorkosListId, request.Rok, request.Miesiac, ct);

        _log.LogInformation("Discharged import done. HorkosListId={Id}, Rok={Rok}, Miesiac={M}, Inserted={Rows}",
            request.HorkosListId, request.Rok, request.Miesiac, inserted);

        return Ok(new { inserted });
    }


    /// <summary>Wejściowe parametry walidacji CSV.</summary>
    public sealed class ValidateCsvRequest
    {
        [Required] public IFormFile File { get; set; } = default!;
        /// <summary>Nr wiersza nagłówka (1-indeksowany), domyślnie 1.</summary>
        public int? HeaderRow { get; set; }
        /// <summary>Gdy true – weryfikuje kolumnę „Stopień” względem słownika.</summary>
        public bool? ValidateRank { get; set; }
        /// <summary>Gdy true – weryfikuje kolumnę „Nazwa jednostki wojskowej” względem słownika.</summary>
        public bool? ValidateUnit { get; set; }
    }

    public sealed class ImportAnnualCsvRequest
    {
        [Required] public IFormFile File { get; set; } = default!;
        [Required] public int HorkosListId { get; set; }
        [Required] public int Rok { get; set; }
    }

    public sealed class ImportDischargedCsvRequest
    {
        [Required] public IFormFile File { get; set; } = default!;
        [Required] public int HorkosListId { get; set; }
        [Required] public int Rok { get; set; }
        [Required][RegularExpression(@"^\d{1,2}$")] public string Miesiac { get; set; } = default!;
    }


    // ===== helpers =====

    private static string SafeBaseName(string? name, string fallback)
    {
        var n = string.IsNullOrWhiteSpace(name) ? fallback : name!;
        var safe = string.Join("_", n.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim('_');
        return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
    }

    private async Task<string> SaveCsvToDiskAsync(byte[] data, string baseName, CancellationToken ct)
    {
        var fileName = $"{baseName}_{DateTime.Now:yyyyMMdd_HHmmss}_WYNIK.csv";
        var fullPath = Path.Combine(_exportDir, fileName);

        try
        {
            await System.IO.File.WriteAllBytesAsync(fullPath, data, ct);
            _log.LogInformation("CSV saved to disk: {Path}", fullPath);
            return fullPath;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to save CSV to disk at {Path}", fullPath);
            Response.Headers["X-Saved-File"] = "ERROR:" + ex.GetType().Name;
            return fullPath;
        }
    }

    private static byte[] EnsureUtf8Bom(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return bytes; // already has BOM

        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var fixedBytes = new byte[bom.Length + bytes.Length];
        Buffer.BlockCopy(bom, 0, fixedBytes, 0, bom.Length);
        Buffer.BlockCopy(bytes, 0, fixedBytes, bom.Length, bytes.Length);
        return fixedBytes;
    }
}
