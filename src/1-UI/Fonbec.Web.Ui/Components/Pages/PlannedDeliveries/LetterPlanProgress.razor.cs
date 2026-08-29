using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.LetterPlanProgress;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Components.NonPages.Dialogs;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.PlannedDeliveries;

[PageMetadata(nameof(LetterPlanProgress), "Detalle de cartas del plan", [FonbecRole.Manager])]
public partial class LetterPlanProgress
{
    private const string EmptyGridMessage = "No hay cartas requeridas para este plan.";

    private LetterPlanProgressViewModel? _viewModel = new();
    private bool _accessDenied;
    private string _pageTitle = "Avance de la campaña";
    private string _searchString = string.Empty;
    private bool _sortByLastName;
    private IEnumerable<string> _allFacilitators = [];
    private IEnumerable<string> _allStatuses = [];

    [Parameter]
    public int PlanId { get; set; }

    [Inject]
    public ILetterPlanProgressService LetterPlanProgressService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (FonbecClaim.ChapterId is null)
        {
            _accessDenied = true;
            return;
        }

        Loading = true;

        _viewModel = await LetterPlanProgressService.GetProgressAsync(PlanId, FonbecClaim.ChapterId.Value);

        Loading = false;

        if (_viewModel is null)
        {
            _accessDenied = true;
            Snackbar.Add("No tenés acceso a esta planificación.", Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.PlannedDeliveries);
            return;
        }

        _pageTitle = $"Avance de la campaña de {_viewModel.PlanLabel}";
        RefreshFilterOptions();
    }

    private void RefreshFilterOptions()
    {
        if (_viewModel is null)
        {
            _allFacilitators = [];
            _allStatuses = [];
            return;
        }

        _allFacilitators = _viewModel.Rows
            .Select(r => r.FacilitatorFullName)
            .Distinct()
            .OrderBy(n => n);

        _allStatuses = _viewModel.Rows
            .Select(r => r.StatusLabel)
            .Distinct()
            .OrderBy(n => n);
    }

    private bool FilterRows(LetterPlanProgressRowViewModel row)
    {
        if (string.IsNullOrWhiteSpace(_searchString))
        {
            return true;
        }

        return $"{row.StudentFirstName} {row.StudentLastName}".ContainsIgnoringAccents(_searchString)
               || (!string.IsNullOrEmpty(row.StudentNickName)
                   && $"{row.StudentNickName} {row.StudentLastName}".ContainsIgnoringAccents(_searchString));
    }

    private string StudentFullName(LetterPlanProgressRowViewModel row) =>
        _sortByLastName
            ? $"{row.StudentLastName}, {row.StudentFirstName}"
            : $"{row.StudentFirstName} {row.StudentLastName}";

    private static Color ChipColorForStatus(LetterPlanDisplayStatus status) => status switch
    {
        LetterPlanDisplayStatus.Missing => Color.Error,
        LetterPlanDisplayStatus.PendingImprovement => Color.Info,
        LetterPlanDisplayStatus.ProcessingImprovement => Color.Warning,
        LetterPlanDisplayStatus.PendingReview => Color.Info,
        LetterPlanDisplayStatus.ProcessingReview => Color.Warning,
        LetterPlanDisplayStatus.Approved => Color.Success,
        LetterPlanDisplayStatus.Rejected => Color.Error,
        LetterPlanDisplayStatus.Exempt => Color.Default,
        _ => Color.Default,
    };

    private static string? StatusTooltip(LetterPlanProgressRowViewModel row) =>
        row.Status switch
        {
            LetterPlanDisplayStatus.Exempt when !string.IsNullOrWhiteSpace(row.ExemptionReason) => row.ExemptionReason,
            LetterPlanDisplayStatus.Rejected when !string.IsNullOrWhiteSpace(row.RejectionReason) => row.RejectionReason,
            _ => null
        };

    private static bool LetterPendingUpload(LetterPlanProgressRowViewModel row) =>
        row.Status is LetterPlanDisplayStatus.Missing or LetterPlanDisplayStatus.Rejected;

    // Exemption replaces the student's entire letter obligation for the plan: it is all or nothing.
    // Once a letter stands for any sponsor, the student must provide letters for all remaining
    // sponsors. A rejected letter still has to be provided, so it leaves the student exemptable.
    private bool StudentCanBeExempted(int studentId)
    {
        if (_viewModel is null)
        {
            return false;
        }

        var studentRows = _viewModel.Rows
            .Where(row => row.StudentId == studentId)
            .ToList();

        return studentRows.Count > 0
               && studentRows.All(LetterPendingUpload);
    }

    private string UploadUrl(LetterPlanProgressRowViewModel row) =>
        NavRoutes.ManagerUploadLetter(
            row.StudentId,
            PlanId,
            row.SponsorId,
            row.CompanyId,
            NavRoutes.LetterPlanProgress(PlanId));

    private async Task ExemptStudentAsync(LetterPlanProgressRowViewModel row)
    {
        var studentName = $"{row.StudentFirstName} {row.StudentLastName}".Trim();
        var title = string.IsNullOrWhiteSpace(studentName)
            ? "Eximir de carta"
            : $"Eximir de carta a {studentName}";

        var parameters = new DialogParameters<LetterExemptionReasonDialog>
        {
            { x => x.Title, title },
            { x => x.Prompt, "Motivo de la exención (obligatorio)" },
            { x => x.PlanLabel, _viewModel?.PlanLabel },
        };

        var dialog = await DialogService.ShowAsync<LetterExemptionReasonDialog>(title, parameters);
        var result = await dialog.Result;

        if (result is null || result.Canceled || result.Data is not string reason)
        {
            return;
        }

        Loading = true;

        var success = await LetterPlanProgressService.ExemptStudentAsync(
            PlanId,
            row.StudentId,
            FonbecClaim.ChapterId!.Value,
            FonbecClaim.UserId,
            reason);

        Loading = false;

        if (!success)
        {
            Snackbar.Add("No se pudo registrar la exención.", Severity.Error);
            return;
        }

        Snackbar.Add("Exención registrada.", Severity.Success);
        await ReloadAsync();
    }

    private async Task RevokeExemptionAsync(int studentId)
    {
        var dialogResult = await DialogService.ShowMessageBox(
            "Quitar exención",
            "¿Confirmás que querés quitar la exención de carta para este becario?",
            yesText: "Quitar exención",
            cancelText: "Cancelar");

        if (dialogResult != true)
        {
            return;
        }

        Loading = true;

        var success = await LetterPlanProgressService.RevokeExemptionAsync(
            PlanId,
            studentId,
            FonbecClaim.ChapterId!.Value,
            FonbecClaim.UserId);

        Loading = false;

        if (!success)
        {
            Snackbar.Add("No se pudo quitar la exención.", Severity.Error);
            return;
        }

        Snackbar.Add("Exención revocada.", Severity.Success);
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (FonbecClaim.ChapterId is null)
        {
            return;
        }

        Loading = true;

        _viewModel = await LetterPlanProgressService.GetProgressAsync(PlanId, FonbecClaim.ChapterId.Value);

        Loading = false;

        if (_viewModel is null)
        {
            NavigationManager.NavigateTo(NavRoutes.PlannedDeliveries);
            return;
        }

        RefreshFilterOptions();
    }
}