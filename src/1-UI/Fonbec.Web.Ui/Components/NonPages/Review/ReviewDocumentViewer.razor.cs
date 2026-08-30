using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Review;

public partial class ReviewDocumentViewer
{
    private int _selectedPageNumber = 1;

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

    protected override void OnParametersSet()
    {
        // Keep the selection valid when the page set changes (or defaults to the single page).
        if (Pages.Count > 0 && Pages.All(p => p.PageNumber != _selectedPageNumber))
        {
            _selectedPageNumber = Pages.Min(p => p.PageNumber);
        }
    }

    private void SelectPage(int pageNumber) => _selectedPageNumber = pageNumber;

    private string PageUrl(int pageNumber) => NavRoutes.ReviewDocumentPage(DocumentId, pageNumber);

    private static bool IsImage(string? mimeType) =>
        !string.IsNullOrWhiteSpace(mimeType) && DocumentMimeTypes.IsImage(mimeType);
}