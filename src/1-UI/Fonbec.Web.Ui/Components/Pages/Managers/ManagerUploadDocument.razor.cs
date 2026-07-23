using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Models.Facilitators.Input;
using Fonbec.Web.Logic.Models.Managers;
using Fonbec.Web.Logic.Models.Managers.Input;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Options;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.Manager;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Managers;

[PageMetadata(nameof(ManagerUploadDocument), "Subir documento", [FonbecRole.Manager])]
public partial class ManagerUploadDocument : AuthenticationRequiredComponentBase
{
    private const string AcceptedExtensions = ".txt,.pdf,.jpg,.jpeg,.png";

    private static readonly Dictionary<string, string> MimeTypeByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".txt"] = "text/plain",
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
        };

    private readonly ManagerUploadDocumentBindModel _bindModel = new();

    private MudForm _form = null!;
    private bool _formValidationSucceeded = true;
    private bool _saving;

    private ManagerUploadContextViewModel? _context;
    private List<string> _descriptionOptions = [];

    private const string DropZoneBaseClass = "fonbec-dropzone";

    // Ordered list of selected files (pages). Multiple entries are only allowed when all are images.
    private readonly List<IBrowserFile> _selectedFiles = [];
    private MudDropContainer<IBrowserFile>? _dropContainer;
    private long _maxFileSizeBytes;
    private string _dropZoneClass = DropZoneBaseClass;

    private bool AnySelectedFileIsImage => _selectedFiles.Any(IsImage);
    private long TotalSelectedSize => _selectedFiles.Sum(f => f.Size);
    private bool TotalSizeExceeded => TotalSelectedSize > _maxFileSizeBytes;

    [Parameter]
    public int StudentId { get; set; }

    [SupplyParameterFromQuery(Name = "tipo")]
    public string? Tipo { get; set; }

    [SupplyParameterFromQuery(Name = "padrinoId")]
    public int? PadrinoId { get; set; }

    [SupplyParameterFromQuery(Name = "empresaId")]
    public int? EmpresaId { get; set; }

    [SupplyParameterFromQuery(Name = "planId")]
    public int? PlanId { get; set; }

    [Inject]
    public IManagerUploadService ManagerUploadService { get; set; } = null!;

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

    [Inject]
    public IOptions<BlobStorageOptions> BlobStorageOptions { get; set; } = null!;

    private bool SaveButtonDisabled =>
        Loading || _saving || _context is null || FirstValidationError() is not null;

    private string DocumentTypeLabel => _context?.DocumentType switch
    {
        DocumentType.Letter => "Carta",
        DocumentType.ReportCard => _context.EducationLevel == EducationLevel.University
            ? "Libreta universitaria"
            : "Boletín",
        DocumentType.Other => "Otro documento",
        _ => string.Empty,
    };

    private string DocumentTypeIcon => _context?.DocumentType switch
    {
        DocumentType.Letter => Icons.Material.Filled.Mail,
        DocumentType.ReportCard => Icons.Material.Filled.Description,
        DocumentType.Other => Icons.Material.Filled.Folder,
        _ => Icons.Material.Filled.InsertDriveFile,
    };

    private string RecipientIcon => _context?.CompanyId.HasValue == true
        ? Icons.Material.Filled.Factory
        : Icons.Material.Filled.Person;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;
        _maxFileSizeBytes = BlobStorageOptions.Value.MaxFileSizeBytes;

        // Managers with a null chapter claim should not occur, but cannot upload if it happens.
        if (FonbecClaim.ChapterId is not { } managerChapterId)
        {
            Snackbar.Add("No se puede subir el documento para este becario.", Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.Students);
            return;
        }

        _context = await ManagerUploadService.GetUploadContextAsync(
            FonbecClaim.UserId, managerChapterId, StudentId, Tipo ?? string.Empty, PadrinoId, EmpresaId, PlanId);

        if (_context is null)
        {
            Snackbar.Add("No se puede subir el documento para este becario.", Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.Students);
            return;
        }

        // Report cards may not use plain text; default to file upload.
        if (_context.DocumentType == DocumentType.ReportCard)
        {
            _bindModel.ContentMode = UploadContentMode.File;
        }

        if (_context.DocumentType is DocumentType.ReportCard or DocumentType.Other)
        {
            var options = await DocumentService.GetDescriptionOptionsAsync(
                _context.ChapterId, _context.DocumentType);
            _descriptionOptions = options.Select(o => o.Text).ToList();
        }

        Loading = false;
    }

    private void OnFilesSelected(IReadOnlyList<IBrowserFile> files)
    {
        foreach (var file in files)
        {
            TryAddFile(file);
        }

        _dropContainer?.Refresh();
    }

    private void TryAddFile(IBrowserFile file)
    {
        var extension = Path.GetExtension(file.Name);
        if (string.IsNullOrEmpty(extension) || !MimeTypeByExtension.ContainsKey(extension))
        {
            Snackbar.Add("El tipo de archivo no está permitido. Formatos: TXT, PDF, JPG, JPEG, PNG.", Severity.Error);
            return;
        }

        // Only image documents may consist of several files; a PDF/text document is always a single file.
        if (!IsImage(file))
        {
            if (_selectedFiles.Count > 0)
            {
                Snackbar.Add("Solo los documentos con imágenes (JPG/PNG) pueden tener varios archivos.", Severity.Error);
                return;
            }
        }
        else if (_selectedFiles.Any(f => !IsImage(f)))
        {
            Snackbar.Add("Solo los documentos con imágenes (JPG/PNG) pueden tener varios archivos.", Severity.Error);
            return;
        }

        _selectedFiles.Add(file);

        if (TotalSizeExceeded)
        {
            Snackbar.Add($"El tamaño total de los archivos supera el máximo permitido ({FormatBytes(_maxFileSizeBytes)}).", Severity.Warning);
        }
    }

    private void RemoveFile(IBrowserFile file)
    {
        _selectedFiles.Remove(file);
        _dropContainer?.Refresh();
    }

    private void OnFileDropped(MudItemDropInfo<IBrowserFile> dropInfo)
    {
        if (dropInfo.Item is null)
        {
            return;
        }

        _selectedFiles.Remove(dropInfo.Item);
        var index = Math.Clamp(dropInfo.IndexInZone, 0, _selectedFiles.Count);
        _selectedFiles.Insert(index, dropInfo.Item);
    }

    private static bool IsImage(IBrowserFile file) =>
        MimeTypeByExtension.TryGetValue(Path.GetExtension(file.Name), out var mimeType)
        && mimeType is "image/jpeg" or "image/png";

    private static string? MimeFor(IBrowserFile file) =>
        MimeTypeByExtension.GetValueOrDefault(Path.GetExtension(file.Name));

    private static string FileIcon(IBrowserFile file) =>
        IsImage(file) ? Icons.Material.Filled.Image : Icons.Material.Filled.InsertDriveFile;

    private void SetDragClass() => _dropZoneClass = $"{DropZoneBaseClass} {DropZoneBaseClass}--active";

    private void ClearDragClass() => _dropZoneClass = DropZoneBaseClass;

    private async Task Save()
    {
        if (_context is null || FonbecClaim.ChapterId is not { } managerChapterId)
        {
            return;
        }

        var validationError = FirstValidationError();
        if (validationError is not null)
        {
            Snackbar.Add(validationError, Severity.Error);
            return;
        }

        _saving = true;

        try
        {
            var result = _context.DocumentType switch
            {
                DocumentType.Letter => await UploadLetterAsync(managerChapterId),
                DocumentType.ReportCard => await UploadReportCardAsync(managerChapterId),
                _ => await UploadOtherAsync(managerChapterId),
            };

            if (!result.IsSuccess)
            {
                foreach (var error in result.Errors ?? [])
                {
                    Snackbar.Add(error, Severity.Error);
                }

                return;
            }

            Snackbar.Add("Documento subido correctamente.", Severity.Success);
            NavigationManager.NavigateTo(NavRoutes.Students);
        }
        finally
        {
            _saving = false;
        }
    }

    /// <summary>
    /// Returns the first required-field / validation error message, or <c>null</c> when the form is ready
    /// to submit. Used both to disable the upload button and as a defensive check on submit; it has no
    /// side effects so it is safe to evaluate on every render.
    /// </summary>
    private string? FirstValidationError()
    {
        if (_context!.DocumentType == DocumentType.ReportCard && _bindModel.Period is null)
        {
            return "Debe indicar el período (mes/año) del boletín.";
        }

        if (_context.DocumentType is DocumentType.ReportCard or DocumentType.Other
            && string.IsNullOrWhiteSpace(_bindModel.EffectiveDescription))
        {
            return "Debe indicar una descripción del documento.";
        }

        return _bindModel.ContentMode switch
        {
            UploadContentMode.File when _selectedFiles.Count == 0 =>
                "Debe seleccionar al menos un archivo.",
            UploadContentMode.File when TotalSizeExceeded =>
                $"El tamaño total de los archivos supera el máximo permitido ({FormatBytes(_maxFileSizeBytes)}).",
            UploadContentMode.Text when string.IsNullOrWhiteSpace(_bindModel.TextContent) =>
                "Debe ingresar el texto del documento.",
            UploadContentMode.YouTube when string.IsNullOrWhiteSpace(_bindModel.YouTubeUrlOrId) =>
                "Debe ingresar un enlace o ID de YouTube.",
            _ => null,
        };
    }

    private async Task<CrudResult<long>> UploadLetterAsync(int managerChapterId)
    {
        var files = OpenSelectedFiles();
        try
        {
            var input = new ManagerUploadLetterInputModel(
                _context!.StudentId,
                _context.PlanId!.Value,
                _context.SponsorId,
                _context.CompanyId,
                _bindModel.ContentMode,
                files,
                _bindModel.TextContent,
                _bindModel.YouTubeUrlOrId,
                NormalizedNotes());

            return await ManagerUploadService.UploadLetterAsync(input, FonbecClaim.UserId, managerChapterId);
        }
        finally
        {
            await DisposeFilesAsync(files);
        }
    }

    private async Task<CrudResult<long>> UploadReportCardAsync(int managerChapterId)
    {
        var files = OpenSelectedFiles();
        try
        {
            var input = new ManagerUploadReportCardInputModel(
                _context!.StudentId,
                DateOnly.FromDateTime(_bindModel.Period!.Value),
                _bindModel.EffectiveDescription,
                _bindModel.ContentMode,
                files,
                _bindModel.YouTubeUrlOrId,
                NormalizedNotes());

            return await ManagerUploadService.UploadReportCardAsync(input, FonbecClaim.UserId, managerChapterId);
        }
        finally
        {
            await DisposeFilesAsync(files);
        }
    }

    private async Task<CrudResult<long>> UploadOtherAsync(int managerChapterId)
    {
        var files = OpenSelectedFiles();
        try
        {
            var input = new ManagerUploadOtherInputModel(
                _context!.StudentId,
                _bindModel.EffectiveDescription,
                _bindModel.ContentMode,
                files,
                _bindModel.TextContent,
                _bindModel.YouTubeUrlOrId,
                NormalizedNotes());

            return await ManagerUploadService.UploadOtherDocumentAsync(input, FonbecClaim.UserId, managerChapterId);
        }
        finally
        {
            await DisposeFilesAsync(files);
        }
    }

    private List<UploadFileInputModel>? OpenSelectedFiles() =>
        _bindModel.ContentMode == UploadContentMode.File
            ? _selectedFiles
                .Select(f => new UploadFileInputModel(f.OpenReadStream(_maxFileSizeBytes), MimeFor(f)!))
                .ToList()
            : null;

    private static async Task DisposeFilesAsync(List<UploadFileInputModel>? files)
    {
        if (files is null)
        {
            return;
        }

        foreach (var file in files)
        {
            await file.Content.DisposeAsync();
        }
    }

    private string? NormalizedNotes() =>
        string.IsNullOrWhiteSpace(_bindModel.UploaderNotes) ? null : _bindModel.UploaderNotes.Trim();

    private static string FormatBytes(long bytes)
    {
        const long megabyte = 1024 * 1024;
        const long kilobyte = 1024;
        return bytes >= megabyte
            ? $"{bytes / (double)megabyte:0.#} MB"
            : $"{bytes / (double)kilobyte:0.#} KB";
    }
}
