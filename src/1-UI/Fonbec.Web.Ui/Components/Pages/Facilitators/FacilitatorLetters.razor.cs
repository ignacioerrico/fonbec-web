using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Facilitators;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Facilitators;

[PageMetadata(nameof(FacilitatorLetters), "Cartas de un becario", [FonbecRole.Uploader])]
public partial class FacilitatorLetters : AuthenticationRequiredComponentBase
{
    private static readonly Dictionary<string, object> NoEnhance = new()
    {
        ["data-enhance-nav"] = "false",
    };

    private readonly HashSet<long> _expandedDocumentIds = [];

    private LettersHistoryViewModel? _history;

    [Parameter]
    public int StudentId { get; set; }

    [Inject]
    public IFacilitatorService FacilitatorService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        _history = await FacilitatorService.GetLettersHistoryAsync(FonbecClaim.UserId, StudentId);
        if (_history is null)
        {
            Snackbar.Add("No se pueden ver las cartas de este becario.", Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.FacilitatorStudents);
            return;
        }

        Loading = false;
    }

    private string PageTitleText =>
        _history is null
            ? "Cartas"
            : $"Cartas — {_history.StudentName}";

    private string ViewUrl(LetterHistoryItemViewModel item, int pageNumber) =>
        NavRoutes.FacilitatorLetterPage(StudentId, item.DocumentId, pageNumber);

    private string DownloadUrl(LetterHistoryItemViewModel item, int pageNumber) =>
        NavRoutes.FacilitatorLetterPage(StudentId, item.DocumentId, pageNumber, download: true);

    private void ToggleExpanded(long documentId)
    {
        if (!_expandedDocumentIds.Add(documentId))
        {
            _expandedDocumentIds.Remove(documentId);
        }
    }

    private bool IsExpanded(long documentId) => _expandedDocumentIds.Contains(documentId);

    private static string FormatDate(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue).ToSpanishShortDate();

    private static string FileKindLabel(FileKind fileKind) => fileKind switch
    {
        FileKind.Blob => "Archivo",
        FileKind.Text => "Texto",
        FileKind.YouTube => "Video",
        _ => fileKind.ToString(),
    };

    private static string RecipientText(LetterHistoryItemViewModel item) =>
        string.IsNullOrWhiteSpace(item.RecipientName) ? "—" : item.RecipientName;

    private static string StatusText(LetterHistoryItemViewModel item)
    {
        if (item.Status == DocumentStatus.PendingManagerDecision)
        {
            return "Pendiente del coordinador";
        }

        if (item.Status == DocumentStatus.Rejected)
        {
            return string.IsNullOrWhiteSpace(item.RejectionReason)
                ? "Rechazado"
                : $"Rechazado: {item.RejectionReason}";
        }

        return item.Status == DocumentStatus.Approved
            ? "Aprobado"
            : "En revisión";
    }
}