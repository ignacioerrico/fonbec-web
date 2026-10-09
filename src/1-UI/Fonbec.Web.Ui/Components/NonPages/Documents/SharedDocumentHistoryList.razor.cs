using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Documents;

public partial class SharedDocumentHistoryList
{
    private readonly HashSet<long> _expandedDocumentIds = [];
    private readonly List<SharedDocumentViewModel> _documents = [];
    private readonly List<RecipientThreadItemViewModel> _threadItems = [];

    private bool _loaded;
    private bool _hasMore;
    private bool _loadingMore;
    private bool _hasOlder;
    private bool _loadingOlder;
    private bool _sending;
    private bool _visitRecorded;
    private string _draft = "";
    private string? _sendError;

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

    private Gender StudentGender { get; set; }

    private string MessageLabel =>
        $"Escribile un mensaje a tu {GenderWords.Ahijado(StudentGender)}";

    private string? RecipientDisplayName { get; set; }

    private string? CcRecipientLine { get; set; }

    private bool CanSend =>
        !_sending && !string.IsNullOrWhiteSpace(_draft);

    protected override async Task OnInitializedAsync()
    {
        var historyTask = LoadPageAsync(skip: 0);
        var threadTask = LoadThreadAsync(skipFromEnd: 0);
        await Task.WhenAll(historyTask, threadTask);

        var history = await historyTask;
        var thread = await threadTask;

        IsAuthorized = history.IsAuthorized && thread.IsAuthorized;
        StudentDisplayName = history.StudentDisplayName;
        StudentGender = history.StudentGender;
        RecipientDisplayName = history.RecipientDisplayName;
        CcRecipientLine = history.CcRecipientLine;
        _hasMore = history.HasMore;
        _hasOlder = thread.HasOlder;
        if (IsAuthorized)
        {
            _previousLastVisitedOnUtc = history.PreviousLastVisitedOnUtc;
            _documents.AddRange(history.Documents);
            _threadItems.AddRange(thread.Items);
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
                _threadItems.Clear();
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

    private async Task LoadOlderAsync()
    {
        if (!_hasOlder || _loadingOlder)
        {
            return;
        }

        _loadingOlder = true;
        try
        {
            var thread = await LoadThreadAsync(_threadItems.Count);
            if (!thread.IsAuthorized)
            {
                IsAuthorized = false;
                _documents.Clear();
                _threadItems.Clear();
                return;
            }

            _threadItems.InsertRange(0, thread.Items);
            _hasOlder = thread.HasOlder;
        }
        finally
        {
            _loadingOlder = false;
        }
    }

    private async Task SendAsync()
    {
        if (!CanSend)
        {
            return;
        }

        _sending = true;
        _sendError = null;
        var body = _draft;
        try
        {
            var result = await DocumentService.SendRecipientMessageAsync(Token, StudentId, IsCompany, body);
            if (!result.IsAuthorized)
            {
                IsAuthorized = false;
                _documents.Clear();
                _threadItems.Clear();
                return;
            }

            if (!result.IsSaved || result.Message is null)
            {
                _sendError = body.Trim().Length > MaxLength.RecipientMessage.Body
                    ? "El mensaje no puede superar los 2000 caracteres."
                    : "No se pudo enviar el mensaje.";
                return;
            }

            _draft = "";
            _threadItems.Add(result.Message);
        }
        finally
        {
            _sending = false;
        }
    }

    private async Task<SponsorDocumentHistoryViewModel> LoadPageAsync(int skip) =>
        IsCompany
            ? await DocumentService.GetSharedDocumentsForCompanyAsync(
                Token, StudentId, skip, Logic.Services.DocumentService.SharedDocumentHistoryPageSize)
            : await DocumentService.GetSharedDocumentsAsync(
                Token, StudentId, skip, Logic.Services.DocumentService.SharedDocumentHistoryPageSize);

    private Task<RecipientThreadViewModel> LoadThreadAsync(int skipFromEnd) =>
        DocumentService.GetRecipientThreadAsync(
            Token, StudentId, IsCompany, skipFromEnd, Logic.Services.DocumentService.RecipientThreadPageSize);

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
}