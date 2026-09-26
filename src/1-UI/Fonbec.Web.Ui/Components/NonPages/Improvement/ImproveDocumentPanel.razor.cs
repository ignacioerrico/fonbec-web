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

    [Parameter]
    public EventCallback<int> OnPageFocused { get; set; }

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Inject]
    public IOptions<BlobStorageOptions> BlobStorageOptions { get; set; } = null!;

    private bool ActionsDisabled => Disabled || _saving;

    private bool HasAnyUpload => _files.Any(f => f is not null);

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

    private Task FocusPageAsync(int pageNumber) => OnPageFocused.InvokeAsync(pageNumber);

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

        var originalPages = Enumerable.Range(1, PageCount)
            .Where(n => _files[n - 1] is null)
            .ToList();

        if (originalPages.Count > 0 && !await ConfirmKeepOriginalsAsync(originalPages))
        {
            return;
        }

        _saving = true;

        var uploads = _files
            .Select(f => f is null
                ? null
                : new UploadFileInputModel(f.OpenReadStream(_maxFileSizeBytes), DocumentMimeTypes.Jpeg))
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

            Snackbar.Add(
                HasAnyUpload
                    ? "Imágenes mejoradas subidas."
                    : "El documento pasó a revisión con las fotos originales.",
                Severity.Success);
            await OnCompleted.InvokeAsync();
        }
        finally
        {
            foreach (var upload in uploads)
            {
                if (upload is not null)
                {
                    await upload.Content.DisposeAsync();
                }
            }

            _saving = false;
        }
    }

    private async Task<bool> ConfirmKeepOriginalsAsync(IReadOnlyList<int> originalPages)
    {
        var allOriginal = originalPages.Count == PageCount;
        var message = allOriginal
            ? "Ninguna página se va a mejorar. El documento pasa a revisión con las fotos originales."
            : $"{FormatOriginalPages(originalPages)} se van a revisar con la foto original. ¿Confirmás que se ven bien?";

        var confirmed = await DialogService.ShowMessageBox(
            allOriginal ? "¿Usar las fotos originales?" : "¿Confirmar páginas sin mejora?",
            message,
            yesText: allOriginal ? "Está bien así" : "Confirmar",
            cancelText: "Cancelar");

        return confirmed == true;
    }

    private static string FormatOriginalPages(IReadOnlyList<int> pages)
    {
        if (pages.Count == 1)
        {
            return $"La página {pages[0]}";
        }

        if (pages.Count == 2)
        {
            return $"Las páginas {pages[0]} y {pages[1]}";
        }

        return $"Las páginas {string.Join(", ", pages.Take(pages.Count - 1))} y {pages[^1]}";
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