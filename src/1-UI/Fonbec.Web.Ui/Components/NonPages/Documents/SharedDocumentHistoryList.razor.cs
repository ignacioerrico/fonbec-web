using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Documents;

public partial class SharedDocumentHistoryList
{
    private readonly HashSet<long> _expandedDocumentIds = [];
    private readonly List<SharedDocumentViewModel> _documents = [];

    private bool _loaded;
    private bool _hasMore;
    private bool _loadingMore;
    private bool _visitRecorded;

    /// <summary>Watermark from the first authorized load; kept for the whole session across paging.</summary>
    private DateTime? _previousLastVisitedOnUtc;

    [Parameter, EditorRequired]
    public Guid Token { get; set; }

    [Parameter, EditorRequired]
    public int StudentId { get; set; }

    [Parameter, EditorRequired]
    public bool IsCompany { get; set; }

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

    private bool IsAuthorized { get; set; }

    private string? StudentDisplayName { get; set; }

    private string? RecipientDisplayName { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var history = await LoadPageAsync(skip: 0);
        IsAuthorized = history.IsAuthorized;
        StudentDisplayName = history.StudentDisplayName;
        RecipientDisplayName = history.RecipientDisplayName;
        _hasMore = history.HasMore;
        if (history.IsAuthorized)
        {
            _previousLastVisitedOnUtc = history.PreviousLastVisitedOnUtc;
            _documents.AddRange(history.Documents);
        }

        _loaded = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // firstRender is the loading spinner, before authorization finishes.
        // Record once the list is on screen so a later refresh treats these as seen.
        if (_visitRecorded || !_loaded || !IsAuthorized)
        {
            return;
        }

        _visitRecorded = true;
        await DocumentService.RecordSharedDocumentHistoryVisitAsync(Token, StudentId, IsCompany);
    }

    private async Task LoadMoreAsync()
    {
        if (!_hasMore || _loadingMore)
        {
            return;
        }

        _loadingMore = true;
        try
        {
            var history = await LoadPageAsync(_documents.Count);
            if (!history.IsAuthorized)
            {
                IsAuthorized = false;
                _documents.Clear();
                return;
            }

            _documents.AddRange(history.Documents);
            _hasMore = history.HasMore;
        }
        finally
        {
            _loadingMore = false;
        }
    }

    private async Task<SponsorDocumentHistoryViewModel> LoadPageAsync(int skip) =>
        IsCompany
            ? await DocumentService.GetSharedDocumentsForCompanyAsync(
                Token, StudentId, skip, Logic.Services.DocumentService.SharedDocumentHistoryPageSize)
            : await DocumentService.GetSharedDocumentsAsync(
                Token, StudentId, skip, Logic.Services.DocumentService.SharedDocumentHistoryPageSize);

    private string DownloadUrl(long documentId, int pageNumber) =>
        IsCompany
            ? NavRoutes.CompanyHistoryDownload(Token, StudentId, documentId, pageNumber)
            : NavRoutes.SponsorHistoryDownload(Token, StudentId, documentId, pageNumber);

    private void ToggleExpanded(long documentId)
    {
        if (!_expandedDocumentIds.Add(documentId))
        {
            _expandedDocumentIds.Remove(documentId);
        }
    }

    /// <summary>
    /// Divider before the first document shared at or before the previous visit,
    /// and only when at least one newer document sits above it.
    /// </summary>
    private bool ShouldShowDividerBefore(SharedDocumentViewModel document)
    {
        if (_previousLastVisitedOnUtc is not { } watermark)
        {
            return false;
        }

        if (document.SharedOn > watermark)
        {
            return false;
        }

        var index = _documents.IndexOf(document);
        if (index <= 0)
        {
            return false;
        }

        return _documents.Take(index).All(d => d.SharedOn > watermark);
    }

    private bool IsNewSinceLastVisit(SharedDocumentViewModel document) =>
        _previousLastVisitedOnUtc is { } watermark && document.SharedOn > watermark;

    private bool IsLastNewDocument(SharedDocumentViewModel document)
    {
        if (!IsNewSinceLastVisit(document))
        {
            return false;
        }

        var index = _documents.IndexOf(document);
        return index >= 0
               && (index == _documents.Count - 1 || !IsNewSinceLastVisit(_documents[index + 1]));
    }

    /// <summary>Year headings only earn their space once the history spans more than one year.</summary>
    private bool ShowYearHeadings =>
        _documents.Select(d => d.SharedOn.ToLocalTime().Year).Distinct().Count() > 1;

    private IEnumerable<IGrouping<int, SharedDocumentViewModel>> DocumentsByYear() =>
        _documents.GroupBy(d => d.SharedOn.ToLocalTime().Year);

    private static bool IsDirectDownload(SharedDocumentViewModel document) =>
        document.FileKind == FileKind.Blob && document.PageCount <= 1;

    private static string ExpandIcon(SharedDocumentViewModel document, bool expanded) =>
        document.FileKind switch
        {
            FileKind.YouTube when !expanded => Icons.Material.Filled.PlayArrow,
            _ when expanded => Icons.Material.Filled.ExpandLess,
            _ => Icons.Material.Filled.ExpandMore,
        };

    private static string TypeLabel(DocumentType documentType) =>
        documentType switch
        {
            DocumentType.Letter => "Carta",
            DocumentType.ReportCard => "Boletín",
            _ => "Otro",
        };

    private static string FormatSharedOn(DateTime sharedOn) =>
        sharedOn.ToLocalTime().ToSpanishShortDate();
}