using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Import;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Import;

[PageMetadata(nameof(ChapterImport), "Importar becarios y padrinos", [FonbecRole.Admin, FonbecRole.Manager])]
public partial class ChapterImport : AuthenticationRequiredComponentBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private int _chapterId;
    private int _facilitatorId;
    private bool _anyChapters;
    private bool _anyFacilitators;
    private int _facilitatorsLoadedChapterId;
    private bool _working;
    private string _workingText = "Leyendo el archivo";
    private bool _previewIsCurrent;
    private bool _committed;

    private byte[]? _excel;
    private string? _excelName;
    private byte[]? _studentsCsv;
    private string? _studentsCsvName;
    private byte[]? _sponsorsCsv;
    private string? _sponsorsCsvName;

    private ChapterImportResult? _result;

    private int EffectiveChapterId => FonbecClaim.ChapterId ?? _chapterId;

    private bool IsChapterKnown => EffectiveChapterId > 0;

    private bool FacilitatorsLoadedForCurrentChapter =>
        _facilitatorsLoadedChapterId == EffectiveChapterId;

    private bool NoFacilitatorsForCurrentChapter =>
        IsChapterKnown && FacilitatorsLoadedForCurrentChapter && !_anyFacilitators;

    private bool HasFile => _excel is not null || _studentsCsv is not null || _sponsorsCsv is not null;

    private bool PreviewDisabled => Loading || _working || !IsChapterKnown || !HasFile;

    private bool CommitDisabled =>
        PreviewDisabled
        || !_previewIsCurrent
        || _committed
        || _result is null
        || _result.Rows.All(row => row.Status != ChapterImportRowStatus.Create);

    private string Summary
    {
        get
        {
            if (_result is null)
            {
                return string.Empty;
            }

            var created = _result.Rows.Count(row => row.Status == ChapterImportRowStatus.Created);
            var failed = _result.Rows.Count(row => row.Status == ChapterImportRowStatus.Failed);
            if (_committed || created > 0 || failed > 0)
            {
                var skipped = _result.Rows.Count(row => row.Status == ChapterImportRowStatus.Skip);
                var errors = _result.Rows.Count(row => row.Status == ChapterImportRowStatus.Error);
                return $"{created} creados · {skipped} omitidos · {errors + failed} con error";
            }

            var toCreate = _result.Rows.Count(row => row.Status == ChapterImportRowStatus.Create);
            var toSkip = _result.Rows.Count(row => row.Status == ChapterImportRowStatus.Skip);
            var withError = _result.Rows.Count(row => row.Status == ChapterImportRowStatus.Error);
            return $"{toCreate} a crear · {toSkip} a omitir · {withError} con error";
        }
    }

    [Inject]
    public IChapterImportService ChapterImportService { get; set; } = null!;

    [Inject]
    public IJSRuntime JsRuntime { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (FonbecClaim.ChapterId.HasValue)
        {
            _anyChapters = true;
            _chapterId = FonbecClaim.ChapterId.Value;
        }
    }

    private Task OnChaptersLoaded(int chaptersCount)
    {
        _anyChapters = chaptersCount > 0;
        return Task.CompletedTask;
    }

    private Task OnFacilitatorsLoaded(int totalFacilitators)
    {
        _anyFacilitators = totalFacilitators > 0;
        _facilitatorsLoadedChapterId = EffectiveChapterId;
        return Task.CompletedTask;
    }

    private Task OnChapterChanged(int chapterId)
    {
        _chapterId = chapterId;
        InvalidatePreview();
        return Task.CompletedTask;
    }

    private Task OnFacilitatorChanged(int facilitatorId)
    {
        if (_facilitatorId == facilitatorId)
        {
            return Task.CompletedTask;
        }

        _facilitatorId = facilitatorId;
        InvalidatePreview();
        return Task.CompletedTask;
    }

    private async Task OnExcelSelected(IBrowserFile file) =>
        (_excel, _excelName) = await ReadFileAsync(file, ".xlsx");

    private async Task OnStudentsCsvSelected(IBrowserFile file) =>
        (_studentsCsv, _studentsCsvName) = await ReadFileAsync(file, ".csv");

    private async Task OnSponsorsCsvSelected(IBrowserFile file) =>
        (_sponsorsCsv, _sponsorsCsvName) = await ReadFileAsync(file, ".csv");

    private void ClearExcel()
    {
        _excel = null;
        _excelName = null;
        InvalidatePreview();
    }

    private void ClearStudentsCsv()
    {
        _studentsCsv = null;
        _studentsCsvName = null;
        InvalidatePreview();
    }

    private void ClearSponsorsCsv()
    {
        _sponsorsCsv = null;
        _sponsorsCsvName = null;
        InvalidatePreview();
    }

    private async Task DownloadExcelAsync() =>
        await DownloadAsync("importar-filial.xlsx", ExcelContentType, ChapterImportService.BuildExcelTemplate());

    private async Task DownloadStudentsCsvAsync() =>
        await DownloadAsync("becarios.csv", "text/csv", ChapterImportService.BuildStudentsCsvTemplate());

    private async Task DownloadSponsorsCsvAsync() =>
        await DownloadAsync("padrinos.csv", "text/csv", ChapterImportService.BuildSponsorsCsvTemplate());

    private async Task PreviewAsync() =>
        await RunAsync(commit: false);

    private async Task CommitAsync() =>
        await RunAsync(commit: true);

    private async Task RunAsync(bool commit)
    {
        if (!IsChapterKnown || !HasFile)
        {
            return;
        }

        _working = true;
        _workingText = commit ? "Importando" : "Leyendo el archivo";
        try
        {
            var request = BuildRequest();
            _result = commit
                ? await ChapterImportService.CommitAsync(request)
                : await ChapterImportService.PreviewAsync(request);
            _previewIsCurrent = true;
            _committed = commit && _result.Rows.Any(row => row.Status == ChapterImportRowStatus.Created);
            if (commit)
            {
                var created = _result.Rows.Count(row => row.Status == ChapterImportRowStatus.Created);
                Snackbar.Add(
                    created > 0
                        ? created == 1 ? "Se creó 1 persona." : $"Se crearon {created} personas."
                        : "No se creó ninguna persona.",
                    created > 0 ? Severity.Success : Severity.Warning);
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Snackbar.Add("No se pudo leer el archivo.", Severity.Error);
        }
        finally
        {
            _working = false;
        }
    }

    private ChapterImportRequest BuildRequest()
    {
        var files = new List<ChapterImportUpload>();
        if (_excel is not null)
        {
            files.Add(new ChapterImportUpload(_excelName ?? "importar.xlsx", _excel, ChapterImportUploadKind.Excel));
        }

        if (_studentsCsv is not null)
        {
            files.Add(new ChapterImportUpload(_studentsCsvName ?? "becarios.csv", _studentsCsv, ChapterImportUploadKind.StudentsCsv));
        }

        if (_sponsorsCsv is not null)
        {
            files.Add(new ChapterImportUpload(_sponsorsCsvName ?? "padrinos.csv", _sponsorsCsv, ChapterImportUploadKind.SponsorsCsv));
        }

        return new ChapterImportRequest(
            EffectiveChapterId,
            FonbecClaim.ChapterId,
            FonbecClaim.UserId,
            _facilitatorId,
            files);
    }

    private async Task<(byte[]? Content, string? Name)> ReadFileAsync(IBrowserFile file, string extension)
    {
        InvalidatePreview();

        if (!file.Name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        {
            Snackbar.Add($"Elegí un archivo {extension}.", Severity.Error);
            return (null, null);
        }

        if (file.Size > ChapterImportLimits.MaxFileBytes)
        {
            Snackbar.Add("El archivo supera el tamaño máximo (2 MB).", Severity.Error);
            return (null, null);
        }

        try
        {
            await using var stream = file.OpenReadStream(ChapterImportLimits.MaxFileBytes);
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            return (memory.ToArray(), file.Name);
        }
        catch (IOException)
        {
            Snackbar.Add("El archivo supera el tamaño máximo (2 MB).", Severity.Error);
            return (null, null);
        }
    }

    private async Task DownloadAsync(string fileName, string contentType, byte[] content) =>
        await JsRuntime.InvokeVoidAsync(
            "fonbecDownloadFile",
            fileName,
            contentType,
            Convert.ToBase64String(content));

    private void InvalidatePreview()
    {
        _previewIsCurrent = false;
        _committed = false;
        _result = null;
    }

    private static string StatusText(ChapterImportRowStatus status) => status switch
    {
        ChapterImportRowStatus.Create => "Crear",
        ChapterImportRowStatus.Skip => "Omitir",
        ChapterImportRowStatus.Error => "Error",
        ChapterImportRowStatus.Created => "Creado",
        ChapterImportRowStatus.Failed => "Error",
        _ => status.ToString(),
    };

    private static Color StatusColor(ChapterImportRowStatus status) => status switch
    {
        ChapterImportRowStatus.Create => Color.Info,
        ChapterImportRowStatus.Skip => Color.Default,
        ChapterImportRowStatus.Error => Color.Error,
        ChapterImportRowStatus.Created => Color.Success,
        ChapterImportRowStatus.Failed => Color.Error,
        _ => Color.Default,
    };
}