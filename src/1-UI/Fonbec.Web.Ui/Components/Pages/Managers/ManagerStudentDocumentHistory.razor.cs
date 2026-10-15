using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Managers;

[PageMetadata(nameof(ManagerStudentDocumentHistory), "Documentos de un becario", [FonbecRole.Manager])]
public partial class ManagerStudentDocumentHistory : AuthenticationRequiredComponentBase
{
    private static readonly Dictionary<string, object> NoEnhance = new()
    {
        ["data-enhance-nav"] = "false",
    };

    private readonly HashSet<long> _expandedDocumentIds = [];

    private StudentDocumentHistoryViewModel? _history;

    [Parameter]
    public int StudentId { get; set; }

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        if (FonbecClaim.ChapterId is not int chapterId)
        {
            Deny();
            return;
        }

        _history = await DocumentService.GetStudentDocumentHistoryAsync(chapterId, StudentId);
        if (_history is null)
        {
            Deny();
            return;
        }

        Loading = false;
    }

    private string PageTitleText =>
        _history is null
            ? "Documentos"
            : $"Documentos — {_history.StudentName}";

    private string ViewUrl(StudentDocumentHistoryItemViewModel item, int pageNumber) =>
        NavRoutes.ManagerStudentDocumentPage(StudentId, item.DocumentId, pageNumber);

    private string DownloadUrl(StudentDocumentHistoryItemViewModel item, int pageNumber) =>
        NavRoutes.ManagerStudentDocumentPage(StudentId, item.DocumentId, pageNumber, download: true);

    private void Deny()
    {
        Snackbar.Add("No se pueden ver los documentos de este becario.", Severity.Error);
        NavigationManager.NavigateTo(NavRoutes.Students);
    }

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

    private static string TypeLabel(DocumentType documentType) => documentType switch
    {
        DocumentType.Letter => "Carta",
        DocumentType.ReportCard => "Boletín",
        DocumentType.Other => "Otro documento",
        _ => documentType.ToString(),
    };

    private static string FileKindLabel(FileKind fileKind) => fileKind switch
    {
        FileKind.Blob => "Archivo",
        FileKind.Text => "Texto",
        FileKind.YouTube => "Video",
        _ => fileKind.ToString(),
    };

    private static string StatusText(StudentDocumentHistoryItemViewModel item)
    {
        if (item.Status == DocumentStatus.Approved && item.RejectionOverriddenOn is not null)
        {
            var reason = string.IsNullOrWhiteSpace(item.RejectionReason)
                ? "sin motivo indicado"
                : item.RejectionReason;
            var who = string.IsNullOrWhiteSpace(item.RejectionOverriddenByName)
                ? string.Empty
                : $" por {item.RejectionOverriddenByName}";
            return $"Aprobado (se compartió pese al rechazo{who}: {reason})";
        }

        if (item.Status == DocumentStatus.PendingManagerDecision)
        {
            return string.IsNullOrWhiteSpace(item.RejectionReason)
                ? "Pendiente del coordinador"
                : $"Pendiente del coordinador: {item.RejectionReason}";
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