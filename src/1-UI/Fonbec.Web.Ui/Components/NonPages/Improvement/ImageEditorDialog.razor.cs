using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Improvement;

public partial class ImageEditorDialog : ComponentBase, IAsyncDisposable
{
    private readonly string _sessionId = Guid.NewGuid().ToString("N");
    private ElementReference _host;
    private bool _disposed;
    private bool _opened;

    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public long DocumentId { get; set; }

    [Parameter, EditorRequired]
    public int PageIndex { get; set; }

    [Parameter, EditorRequired]
    public string SourceUrl { get; set; } = string.Empty;

    [Inject]
    public IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _opened)
        {
            return;
        }

        _opened = true;

        try
        {
            var result = await JsRuntime.InvokeAsync<ImageEditorJsResult>(
                "fonbecImageEditor.open",
                _host,
                new ImageEditorOpenRequest(
                    _sessionId,
                    Assets["lib/filerobot-image-editor/filerobot-image-editor.min.js"],
                    DocumentId,
                    PageIndex,
                    SourceUrl,
                    $"pagina-{PageIndex + 1}.jpg"));

            if (_disposed)
            {
                return;
            }

            if (result is { Saved: true })
            {
                MudDialog.Close(DialogResult.Ok(new ImageEditorResult(result.Size, result.Name ?? string.Empty)));
                return;
            }

            if (!string.IsNullOrWhiteSpace(result?.Error))
            {
                Snackbar.Add(result.Error, Severity.Error);
            }

            CancelDialog();
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException ex) when (ex.Message.Contains("cargar el editor", StringComparison.Ordinal))
        {
            if (_disposed)
            {
                return;
            }

            Snackbar.Add("No se pudo abrir el editor de imágenes.", Severity.Error);
            CancelDialog();
        }
        catch (JSException)
        {
            CancelDialog();
        }
    }

    private void CancelDialog()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            MudDialog.Cancel();
        }
        catch (Exception)
        {
            // The dialog is already closing.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            await JsRuntime.InvokeVoidAsync("fonbecImageEditor.cancel", _sessionId);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException)
        {
        }
    }

    private sealed class ImageEditorOpenRequest(
        string sessionId,
        string scriptUrl,
        long documentId,
        int pageIndex,
        string sourceUrl,
        string fileName)
    {
        public string SessionId { get; } = sessionId;
        public string ScriptUrl { get; } = scriptUrl;
        public long DocumentId { get; } = documentId;
        public int PageIndex { get; } = pageIndex;
        public string SourceUrl { get; } = sourceUrl;
        public string FileName { get; } = fileName;
    }

    private sealed class ImageEditorJsResult
    {
        public bool Saved { get; set; }

        public long Size { get; set; }

        public string? Name { get; set; }

        public string? Error { get; set; }
    }
}

public sealed record ImageEditorResult(long Size, string Name);