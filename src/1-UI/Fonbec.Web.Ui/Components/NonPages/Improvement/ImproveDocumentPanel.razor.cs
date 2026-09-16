using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Options;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Improvement;

public partial class ImproveDocumentPanel : ComponentBase
{
    private static readonly HashSet<string> JpegExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
    };

    private IBrowserFile?[] _files = [];
    private long _maxFileSizeBytes;
    private bool _saving;

    [Parameter, EditorRequired]
    public long DocumentId { get; set; }

    [Parameter, EditorRequired]
    public int PageCount { get; set; }

    [Parameter, EditorRequired]
    public byte[] RowVersion { get; set; } = null!;

    [Parameter, EditorRequired]
    public int ReviewerId { get; set; }

    [Parameter, EditorRequired]
    public string ReviewerRole { get; set; } = string.Empty;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback OnCompleted { get; set; }

    [Parameter]
    public EventCallback OnRelease { get; set; }

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    public IOptions<BlobStorageOptions> BlobStorageOptions { get; set; } = null!;

    private bool ActionsDisabled => Disabled || _saving;

    private bool SubmitDisabled => ActionsDisabled || FirstValidationError() is not null;

    protected override void OnParametersSet()
    {
        _maxFileSizeBytes = BlobStorageOptions.Value.MaxFileSizeBytes;

        if (_files.Length != PageCount)
        {
            var previous = _files;
            _files = new IBrowserFile?[PageCount];
            for (var i = 0; i < Math.Min(previous.Length, PageCount); i++)
            {
                _files[i] = previous[i];
            }
        }
    }

    private void OnPageFileSelected(int pageIndex, IBrowserFile? file)
    {
        if (file is null)
        {
            return;
        }

        var extension = Path.GetExtension(file.Name);
        if (!JpegExtensions.Contains(extension))
        {
            Snackbar.Add("La versión mejorada debe ser una imagen JPG.", Severity.Error);
            return;
        }

        _files[pageIndex] = file;
    }

    private void ClearPage(int pageIndex) => _files[pageIndex] = null;

    private string? FirstValidationError()
    {
        if (PageCount <= 0)
        {
            return "El documento no tiene páginas para mejorar.";
        }

        var selected = _files.Count(f => f is not null);
        if (selected != PageCount)
        {
            return $"Debés subir {PageCount} imágenes JPG (una por página).";
        }

        long totalSize = 0;
        foreach (var file in _files)
        {
            if (file is null)
            {
                continue;
            }

            var extension = Path.GetExtension(file.Name);
            if (!JpegExtensions.Contains(extension))
            {
                return "La versión mejorada debe ser una imagen JPG.";
            }

            totalSize += file.Size;
        }

        if (totalSize > _maxFileSizeBytes)
        {
            return $"El tamaño total de los archivos supera el máximo permitido ({FormatBytes(_maxFileSizeBytes)}).";
        }

        return null;
    }

    private async Task ReleaseClickedAsync() => await OnRelease.InvokeAsync();

    private async Task SubmitAsync()
    {
        var validationError = FirstValidationError();
        if (validationError is not null)
        {
            Snackbar.Add(validationError, Severity.Error);
            return;
        }

        _saving = true;

        var uploads = _files
            .Select(f => new UploadFileInputModel(f!.OpenReadStream(_maxFileSizeBytes), DocumentMimeTypes.Jpeg))
            .ToList();

        try
        {
            var result = await DocumentService.SubmitDigitalImprovementWithBlobAsync(
                new SubmitDigitalImprovementWithBlobInputModel(
                    DocumentId,
                    ReviewerId,
                    ReviewerRole,
                    null,
                    uploads,
                    RowVersion));

            if (!result.IsSuccess)
            {
                foreach (var error in result.Errors ?? [])
                {
                    Snackbar.Add(error, Severity.Error);
                }

                return;
            }

            Snackbar.Add("Imágenes mejoradas subidas.", Severity.Success);
            await OnCompleted.InvokeAsync();
        }
        finally
        {
            foreach (var upload in uploads)
            {
                await upload.Content.DisposeAsync();
            }

            _saving = false;
        }
    }

    private static string FormatBytes(long bytes)
    {
        const long megabyte = 1024 * 1024;
        const long kilobyte = 1024;
        return bytes >= megabyte
            ? $"{bytes / (double)megabyte:0.#} MB"
            : $"{bytes / (double)kilobyte:0.#} KB";
    }
}