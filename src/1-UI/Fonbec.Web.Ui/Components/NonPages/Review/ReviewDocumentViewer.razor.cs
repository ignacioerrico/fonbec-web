using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Fonbec.Web.Ui.Components.NonPages.Review;

public partial class ReviewDocumentViewer
{
    private int _selectedPageNumber = 1;
    private ElementReference _imageElement;
    private int? _pixelWidth;
    private int? _pixelHeight;
    private bool _pixelSizeUnavailable;

    [Parameter, EditorRequired]
    public long DocumentId { get; set; }

    [Parameter, EditorRequired]
    public FileKind FileKind { get; set; }

    [Parameter]
    public string? TextContent { get; set; }

    [Parameter]
    public string? YouTubeVideoId { get; set; }

    [Parameter]
    public IReadOnlyList<ReviewWorkspacePageViewModel> Pages { get; set; } = [];

    [Parameter]
    public string? UploaderNotes { get; set; }

    /// <summary>
    /// Optional page-blob URL factory. Defaults to the review (active blob) route.
    /// </summary>
    [Parameter]
    public Func<int, string>? PageUrlFactory { get; set; }

    /// <summary>
    /// When true, show the selected image's intrinsic pixel size (naturalWidth × naturalHeight).
    /// Used on the digital-improvement workspace as a resize hint.
    /// </summary>
    [Parameter]
    public bool ShowPixelDimensions { get; set; }

    [Inject]
    public IJSRuntime JsRuntime { get; set; } = null!;

    private IReadOnlyList<ReviewWorkspacePageViewModel> OrderedPages =>
        Pages.OrderBy(p => p.PageNumber).ToList();

    private bool HasPages => FileKind == FileKind.Blob && Pages.Count > 0;

    private bool HasMultiplePages => FileKind == FileKind.Blob && Pages.Count > 1;

    private int TotalPages => Pages.Count;

    private ReviewWorkspacePageViewModel? SelectedPage =>
        Pages.FirstOrDefault(p => p.PageNumber == _selectedPageNumber);

    private bool SelectedPageIsImage => IsImage(SelectedPage?.MimeType);

    private bool SelectedPageIsPdf =>
        string.Equals(SelectedPage?.MimeType, DocumentMimeTypes.Pdf, StringComparison.OrdinalIgnoreCase);

    private string? PixelDimensionsLabel =>
        _pixelWidth is { } width && _pixelHeight is { } height
            ? $"{width} × {height} px"
            : null;

    protected override void OnParametersSet()
    {
        // Keep the selection valid when the page set changes (or defaults to the single page).
        if (Pages.Count > 0 && Pages.All(p => p.PageNumber != _selectedPageNumber))
        {
            _selectedPageNumber = Pages.Min(p => p.PageNumber);
            ClearPixelDimensions();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (ShowPixelDimensions && SelectedPageIsImage && PixelDimensionsLabel is null && !_pixelSizeUnavailable)
        {
            await ReadNaturalSizeAsync();
        }
    }

    private void SelectPage(int pageNumber)
    {
        if (_selectedPageNumber == pageNumber)
        {
            return;
        }

        _selectedPageNumber = pageNumber;
        ClearPixelDimensions();
    }

    private Task OnImageLoadedAsync() => ReadNaturalSizeAsync();

    private async Task ReadNaturalSizeAsync()
    {
        if (!ShowPixelDimensions || !SelectedPageIsImage)
        {
            return;
        }

        try
        {
            var size = await JsRuntime.InvokeAsync<NaturalSize?>("fonbecImageNaturalSize", _imageElement);
            if (size is not { Width: > 0, Height: > 0 })
            {
                return;
            }

            if (_pixelWidth == size.Width && _pixelHeight == size.Height)
            {
                return;
            }

            _pixelWidth = size.Width;
            _pixelHeight = size.Height;
            await InvokeAsync(StateHasChanged);
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone; nothing to show.
        }
        catch (JSException)
        {
            _pixelSizeUnavailable = true;
        }
    }

    private void ClearPixelDimensions()
    {
        _pixelWidth = null;
        _pixelHeight = null;
        _pixelSizeUnavailable = false;
    }

    private string PageUrl(int pageNumber) =>
        PageUrlFactory?.Invoke(pageNumber) ?? NavRoutes.ReviewDocumentPage(DocumentId, pageNumber);

    private static bool IsImage(string? mimeType) =>
        !string.IsNullOrWhiteSpace(mimeType) && DocumentMimeTypes.IsImage(mimeType);

    private sealed class NaturalSize
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
