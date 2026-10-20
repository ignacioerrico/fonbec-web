using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Options;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Improvement;

public partial class ImproveDocumentPanel : ComponentBase
{
    private static readonly HashSet<string> JpegExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
    };

    private PageReplacement?[] _pages = [];
    private long _maxFileSizeBytes;
    private bool _saving;
    private bool _editing;

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

    [Inject]
    public IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    private bool ActionsDisabled => Disabled || _saving || _editing;

    private bool HasAnyUpload => _pages.Any(page => page is not null);

    private bool SubmitDisabled => ActionsDisabled || FirstValidationError() is not null;

    protected override void OnParametersSet()
    {
        _maxFileSizeBytes = BlobStorageOptions.Value.MaxFileSizeBytes;

        if (_pages.Length != PageCount)
        {
            var previous = _pages;
            _pages = new PageReplacement?[PageCount];
            for (var i = 0; i < Math.Min(previous.Length, PageCount); i++)
            {
                _pages[i] = previous[i];
            }
        }
    }

    private Task FocusPageAsync(int pageNumber) => OnPageFocused.InvokeAsync(pageNumber);

    private async Task OnPageFileSelected(int pageIndex, IBrowserFile? file)
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

        await ClearEditedImageAsync(pageIndex);
        _pages[pageIndex] = new PageReplacement(file.Name, file.Size, file);
    }

    private async Task EditPageAsync(int pageIndex)
    {
        if (_editing)
        {
            return;
        }

        _editing = true;
        try
        {
            await FocusPageAsync(pageIndex + 1);

            if (_pages[pageIndex]?.File is not null)
            {
                var replaceUpload = await DialogService.ShowMessageBox(
                    "¿Reemplazar el JPG?",
                    "La imagen editada va a reemplazar el JPG que subiste para esta página.",
                    yesText: "Editar",
                    cancelText: "Cancelar");
                if (replaceUpload != true)
                {
                    return;
                }
            }

            var parameters = new DialogParameters<ImageEditorDialog>
            {
                { dialog => dialog.DocumentId, DocumentId },
                { dialog => dialog.PageIndex, pageIndex },
                { dialog => dialog.SourceUrl, PageSourceUrl(pageIndex) },
            };

            var dialog = await DialogService.ShowAsync<ImageEditorDialog>(
                string.Empty,
                parameters,
                new DialogOptions
                {
                    FullScreen = true,
                    CloseButton = false,
                    NoHeader = true,
                    BackdropClick = false,
                    CloseOnEscapeKey = false,
                });
            var result = await dialog.Result;
            if (result is { Canceled: false, Data: ImageEditorResult saved })
            {
                _pages[pageIndex] = new PageReplacement(saved.Name, saved.Size, file: null);
            }
        }
        finally
        {
            _editing = false;
        }
    }

    private async Task ClearPageAsync(int pageIndex)
    {
        _pages[pageIndex] = null;
        await ClearEditedImageAsync(pageIndex);
    }

    private async Task ClearEditedImageAsync(int pageIndex)
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("fonbecImageEditor.clear", DocumentId, pageIndex);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private string PageSourceUrl(int pageIndex) =>
        NavigationManager.ToAbsoluteUri(NavRoutes.ImproveDocumentPage(DocumentId, pageIndex + 1)).AbsoluteUri;

    private string? FirstValidationError()
    {
        if (PageCount <= 0)
        {
            return "El documento no tiene páginas para mejorar.";
        }

        long totalSize = 0;
        foreach (var page in _pages)
        {
            if (page is null)
            {
                continue;
            }

            var extension = Path.GetExtension(page.Name);
            if (!JpegExtensions.Contains(extension))
            {
                return "La versión mejorada debe ser una imagen JPG.";
            }

            totalSize += page.Size;
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
            .Where(n => _pages[n - 1] is null)
            .ToList();

        if (originalPages.Count > 0 && !await ConfirmKeepOriginalsAsync(originalPages))
        {
            return;
        }

        _saving = true;

        var uploads = new List<UploadFileInputModel?>(PageCount);
        var streamRefs = new List<IJSStreamReference>();

        try
        {
            for (var i = 0; i < PageCount; i++)
            {
                var page = _pages[i];
                if (page is null)
                {
                    uploads.Add(null);
                    continue;
                }

                Stream content;
                if (page.File is not null)
                {
                    content = page.File.OpenReadStream(_maxFileSizeBytes);
                }
                else
                {
                    var streamRef = await JsRuntime.InvokeAsync<IJSStreamReference>(
                        "fonbecImageEditor.take", DocumentId, i);
                    streamRefs.Add(streamRef);
                    content = await streamRef.OpenReadStreamAsync(_maxFileSizeBytes);
                }

                uploads.Add(new UploadFileInputModel(content, DocumentMimeTypes.Jpeg));
            }

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

            try
            {
                await JsRuntime.InvokeVoidAsync("fonbecImageEditor.clearDocument", DocumentId);
            }
            catch (JSDisconnectedException)
            {
            }
            catch (JSException)
            {
            }

            Snackbar.Add(
                HasAnyUpload
                    ? "Imágenes mejoradas subidas."
                    : "El documento pasó a revisión con las fotos originales.",
                Severity.Success);
            await OnCompleted.InvokeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException)
        {
            Snackbar.Add("No se pudo leer una imagen editada. Volvé a editarla.", Severity.Error);
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

            foreach (var streamRef in streamRefs)
            {
                await streamRef.DisposeAsync();
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

    private sealed class PageReplacement(string name, long size, IBrowserFile? file)
    {
        public string Name { get; } = name;

        public long Size { get; } = size;

        public IBrowserFile? File { get; } = file;
    }
}
