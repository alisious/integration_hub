using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using Trentum.Common.Csv;
using Trentum.Horkos;

namespace IntegrationHub.Api.Controllers;

[ApiController]
[Route("tools/horkos")]
[ApiExplorerSettings(GroupName = "v1")]
public class HorkosController : ControllerBase
{
    private readonly IHorkosDictionaryService _dict;

    public HorkosController(IHorkosDictionaryService dict)
    {
        _dict = dict;
    }

    /// <summary>
    /// Walidacja listy ZWOLNIONYCH z pliku CSV (separator ';').
    /// Wymagane nagłówki: Stopień, Imię pierwsze, Imię drugie (wartość może być pusta), Nazwisko, PESEL, Stanowisko, Nazwa jednostki wojskowej, Data zwolnienia.
    /// Opcjonalnie: <c>validateRank=true</c> — sprawdza kolumnę „Stopień” względem listy referencyjnej z bazy.
    /// Zwraca wynik walidacji jako plik CSV z dodatkową kolumną "STATUS WALIDACJI".
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
        Description = "Wynik: CSV z kolumną STATUS WALIDACJI. Ustaw validateRank=true aby zweryfikować wartości w kolumnie „Stopień” względem słownika.",
        OperationId = "Tools_ValidateDischargedList_Csv",
        Tags = new[] { "Horkos CSV" }
    )]
    public async Task<IActionResult> ValidateDischargedList([FromForm] ValidateCsvRequest request, CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest("Brak pliku lub plik pusty.");

        // Opcjonalnie pobierz listę dozwolonych stopni
        IReadOnlyList<string>? ranks = null;
        if (request.ValidateRank == true)
            ranks = await _dict.GetRankReferenceListAsync(ct); // pobiera listę stopni z bazy (distinct, trim) :contentReference[oaicite:1]{index=1}

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
            validRanks: ranks
        );

        var baseName = Path.GetFileNameWithoutExtension(request.File.FileName);
        if (string.IsNullOrWhiteSpace(baseName)) baseName = "lista_zwolnionych";

        return File(bytes, "text/csv; charset=utf-16", $"{baseName}_WYNIK.csv");
    }

    /// <summary>
    /// Walidacja ROCZNEJ listy z pliku CSV (separator ';').
    /// Wymagane nagłówki: Stopień, Imiona, Nazwisko, PESEL, Stanowisko, Nazwa jednostki wojskowej.
    /// Kolumna "Nr etatu" nie jest wymagana i nie pojawia się w wyniku.
    /// Opcjonalnie: <c>validateRank=true</c> — sprawdza kolumnę „Stopień” względem listy referencyjnej z bazy.
    /// Zwraca wynik walidacji jako plik CSV z dodatkową kolumną "STATUS WALIDACJI".
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
        Description = "Wynik: CSV z kolumną STATUS WALIDACJI. Ustaw validateRank=true aby zweryfikować wartości w kolumnie „Stopień” względem słownika.",
        OperationId = "Tools_ValidateAnnualList_Csv",
        Tags = new[] { "Horkos CSV" }
    )]
    public async Task<IActionResult> ValidateAnnualList([FromForm] ValidateCsvRequest request, CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest("Brak pliku lub plik pusty.");

        // Opcjonalnie pobierz listę dozwolonych stopni
        IReadOnlyList<string>? ranks = null;
        if (request.ValidateRank == true)
            ranks = await _dict.GetRankReferenceListAsync(ct); // słownik stopni HORKOS_STOPIEN :contentReference[oaicite:2]{index=2}

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
            validRanks: ranks
        );

        var baseName = Path.GetFileNameWithoutExtension(request.File.FileName);
        if (string.IsNullOrWhiteSpace(baseName)) baseName = "lista_roczna";

        return File(bytes, "text/csv; charset=utf-16", $"{baseName}_WYNIK.csv");
    }

    /// <summary>
    /// Dane wejściowe dla walidatorów CSV.
    /// </summary>
    public sealed class ValidateCsvRequest
    {
        /// <summary>Plik CSV z danymi (separator średnik ';').</summary>
        [Required]
        public IFormFile File { get; set; } = default!;

        /// <summary>Nr wiersza z nagłówkiem (1-indeksowany, domyślnie 1).</summary>
        public int? HeaderRow { get; set; }

        /// <summary>Gdy <c>true</c>, kolumna „Stopień” jest weryfikowana względem słownika.</summary>
        public bool? ValidateRank { get; set; }
    }
}
