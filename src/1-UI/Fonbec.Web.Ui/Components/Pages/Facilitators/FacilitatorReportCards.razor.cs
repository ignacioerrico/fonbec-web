using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Facilitators;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Facilitators;

[PageMetadata(nameof(FacilitatorReportCards), "Calificaciones de un becario", [FonbecRole.Uploader])]
public partial class FacilitatorReportCards : AuthenticationRequiredComponentBase
{
    private static readonly Dictionary<string, object> NoEnhance = new()
    {
        ["data-enhance-nav"] = "false",
    };

    private readonly HashSet<long> _expandedDocumentIds = [];

    private ReportCardsHistoryViewModel? _history;

    [Parameter]
    public int StudentId { get; set; }

    [Inject]
    public IFacilitatorService FacilitatorService { get; set; } = null!;

    private int? _requestedStudentId;

    private int _historyRequest;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (_requestedStudentId == StudentId)
        {
            return;
        }

        _requestedStudentId = StudentId;
        var request = ++_historyRequest;

        Loading = true;
        _history = null;
        _expandedDocumentIds.Clear();

        var history = await FacilitatorService.GetReportCardsHistoryAsync(FonbecClaim.UserId, StudentId);
        if (request != _historyRequest)
        {
            return;
        }

        _history = history;
        if (_history is null)
        {
            Snackbar.Add("No se pueden ver las calificaciones de este becario.", Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.FacilitatorStudents);
            return;
        }

        Loading = false;
    }

    private bool IsUniversity => _history?.EducationLevel == EducationLevel.University;

    private string DocumentsNoun => IsUniversity ? "libretas universitarias" : "boletines";

    private string PageTitleText =>
        _history is null
            ? "Calificaciones"
            : $"{(IsUniversity ? "Libretas universitarias" : "Boletines")} — {_history.StudentName}";

    private string EmptyText => $"Aún no hay {DocumentsNoun}.";

    private string ViewUrl(ReportCardHistoryItemViewModel item, int pageNumber) =>
        NavRoutes.FacilitatorReportCardPage(StudentId, item.DocumentId, pageNumber);

    private string DownloadUrl(ReportCardHistoryItemViewModel item, int pageNumber) =>
        NavRoutes.FacilitatorReportCardPage(StudentId, item.DocumentId, pageNumber, download: true);

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

    private static string DescriptionText(ReportCardHistoryItemViewModel item) =>
        string.IsNullOrWhiteSpace(item.Description) ? "—" : item.Description;

    private static string StatusText(ReportCardHistoryItemViewModel item)
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
