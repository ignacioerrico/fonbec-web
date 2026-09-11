using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.LetterFollowUp;
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
    public IPlanCompletionService PlanCompletionService { get; set; } = null!;

    [Inject]
    public ILetterFollowUpService LetterFollowUpService { get; set; } = null!;

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
               && !_viewModel.IsPlanCompleted
               && studentRows.All(LetterPendingUpload);
    }

    private string UploadUrl(LetterPlanProgressRowViewModel row) =>
        NavRoutes.ManagerUploadLetter(
            row.StudentId,
            PlanId,
            row.SponsorId,
            row.CompanyId,
            NavRoutes.LetterPlanProgress(PlanId));

    private async Task ShowFlagAsync(LetterFollowUpTaskViewModel? task)
    {
        if (task is null)
        {
            return;
        }

        var parameters = new DialogParameters<LetterFlagDialog>
        {
            { dialog => dialog.Task, task },
        };
        var dialog = await DialogService.ShowAsync<LetterFlagDialog>(
            task.Kind == LetterFollowUpTaskKind.RedFlag ? "Bandera roja" : "Bandera verde",
            parameters);
        var dialogResult = await dialog.Result;

        if (dialogResult is null
            || dialogResult.Canceled
            || dialogResult.Data is not LetterFlagDialogResult result)
        {
            return;
        }

        var priorityChanged = result.Priority.HasValue && task.Priority != result.Priority;
        if (priorityChanged)
        {
            Loading = true;
            var updated = await LetterFollowUpService.SetRedFlagPriorityAsync(
                task.AssessmentId,
                FonbecClaim.ChapterId!.Value,
                result.Priority!.Value);

            if (!updated)
            {
                Loading = false;
                Snackbar.Add("No se pudo cambiar la prioridad.", Severity.Error);
                await ReloadAsync();
                return;
            }
        }

        if (result.Resolve)
        {
            Loading = true;
            var resolved = await LetterFollowUpService.MarkTaskResolvedAsync(
                task.AssessmentId,
                task.Kind,
                FonbecClaim.ChapterId!.Value,
                FonbecClaim.UserId);

            if (!resolved)
            {
                Loading = false;
                Snackbar.Add("No se pudo resolver la tarea.", Severity.Error);
                await ReloadAsync();
                return;
            }

            Snackbar.Add("Tarea resuelta.", Severity.Success);
            await ReloadAsync();
            return;
        }

        if (priorityChanged)
        {
            Snackbar.Add("Prioridad actualizada.", Severity.Success);
            await ReloadAsync();
        }
    }

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

        if (_viewModel is { IsReadyToComplete: true })
        {
            await PromptCompletePlanAsync();
        }
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

    private async Task PromptCompletePlanAsync()
    {
        var dialogResult = await DialogService.ShowMessageBox(
            "Completar campaña",
            "Todas las cartas de esta campaña están cubiertas. ¿Querés marcarla como completada?",
            yesText: "Completar campaña",
            cancelText: "Ahora no");

        if (dialogResult == true)
        {
            await CompletePlanAsync(confirmed: true);
        }
    }

    private Task HandleCompletePlanClicked() => CompletePlanAsync(confirmed: false);

    private async Task CompletePlanAsync(bool confirmed)
    {
        if (!confirmed)
        {
            var dialogResult = await DialogService.ShowMessageBox(
                "Completar campaña",
                "¿Confirmás que querés marcar esta campaña como completada?",
                yesText: "Completar campaña",
                cancelText: "Cancelar");

            if (dialogResult != true)
            {
                return;
            }
        }

        if (FonbecClaim.ChapterId is null)
        {
            return;
        }

        Loading = true;

        var result = await PlanCompletionService.CompletePlanAsync(
            PlanId,
            FonbecClaim.ChapterId.Value,
            FonbecClaim.UserId);

        Loading = false;

        if (!result.Success)
        {
            var message = result.Errors.Count > 0
                ? result.Errors[0]
                : "No se pudo completar la campaña.";
            Snackbar.Add(message, Severity.Error);
            return;
        }

        Snackbar.Add("La campaña fue marcada como completada.", Severity.Success);
        await ReloadAsync();
    }
}