using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.LetterPlanProgress;
using Fonbec.Web.Logic.Models.PlannedDeliveries;
using Fonbec.Web.Logic.Models.PlannedDeliveries.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.PlannedDeliveries;

[PageMetadata(nameof(PlannedDeliveriesList), "Lista de planificaciones de envíos", [FonbecRole.Manager])]
public partial class PlannedDeliveriesList : AuthenticationRequiredComponentBase
{
    private readonly string CheckMarkEmoji = char.ConvertFromUtf32(0x2705);

    private CurrentPlannedDeliveryViewModel? _currentPlan;
    private LetterPlanProgressViewModel? _currentProgress;
    private CurrentPlannedDeliveryViewModel? _latestCompletedPlan;
    private LetterPlanProgressViewModel? _latestCompletedProgress;
    private List<PlannedDeliveriesListViewModel> _previousPlans = [];
    private bool _showPreviousPlans;
    private bool _loadingPreviousPlans;
    private bool _previousPlansLoaded;
    private bool _canCreatePlan;

    private PlannedDeliveriesListViewModel _originalViewModel = new();
    private List<DateTime> _existingPlannedDeliveryDates = [];
    private DateTime? _plannedDeliveryStartsOn;
    private readonly DateTime _minDate = new(DateTime.Now.Year, DateTime.Now.Month, 1);

    [Inject]
    public IPlannedDeliveryService PlannedDeliveryService { get; set; } = null!;

    [Inject]
    public ILetterPlanProgressService LetterPlanProgressService { get; set; } = null!;

    [Inject]
    public IPlanCompletionService PlanCompletionService { get; set; } = null!;

    private int LatestCompletedExemptStudents =>
        _latestCompletedProgress?.Rows
            .Where(r => r.IsStudentExempt)
            .Select(r => r.StudentId)
            .Distinct()
            .Count() ?? 0;

    private string PreviousPlanInfoMessage
    {
        get
        {
            if (_latestCompletedPlan is null || _latestCompletedProgress is null)
            {
                return string.Empty;
            }

            var planLabel = _latestCompletedPlan.PlannedDeliveryStartsOnText.CapitalizeFirstLetter();
            var delivered = _latestCompletedProgress.Summary.Approved;
            var lettersWord = delivered == 1 ? "carta" : "cartas";
            var message = $"En la campaña anterior ({planLabel}) se enviaron {delivered} {lettersWord}";

            if (LatestCompletedExemptStudents > 0)
            {
                var exemptWord = LatestCompletedExemptStudents == 1 ? "becario" : "becarios";
                message += $" y se eximieron {LatestCompletedExemptStudents} {exemptWord}";
            }

            return message + ".";
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (FonbecClaim.ChapterId is null)
        {
            Snackbar.Add("Esta página requiere un usuario que pertenezca a una filial.", Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.Home);
            return;
        }

        Loading = true;

        var chapterId = FonbecClaim.ChapterId.Value;
        _currentPlan = await PlannedDeliveryService.GetCurrentPlanAsync(chapterId);

        if (_currentPlan is not null)
        {
            await PlanCompletionService.EvaluateAndUpdateAsync(
                _currentPlan.PlannedDeliveryId, chapterId, FonbecClaim.UserId);

            // Reload in case auto-complete removed the current plan.
            _currentPlan = await PlannedDeliveryService.GetCurrentPlanAsync(chapterId);
        }

        // The current plan may have been auto-completed, so we need to check again.
        if (_currentPlan is not null)
        {
            _currentProgress = await LetterPlanProgressService.GetProgressAsync(
                _currentPlan.PlannedDeliveryId, chapterId);
        }

        _latestCompletedPlan = await PlannedDeliveryService.GetLatestCompletedPlanAsync(chapterId);
        if (_latestCompletedPlan is not null)
        {
            _latestCompletedProgress = await LetterPlanProgressService.GetProgressAsync(
                _latestCompletedPlan.PlannedDeliveryId, chapterId);
        }

        _canCreatePlan = _currentPlan is null;
        _existingPlannedDeliveryDates = await PlannedDeliveryService.GetPlannedDeliveryDatesAsync(chapterId);

        Loading = false;
    }

    private static string FormatLettersCell(PlannedDeliveriesListViewModel plan)
    {
        var lettersWord = plan.LettersDelivered == 1 ? "carta" : "cartas";
        var text = $"{plan.LettersDelivered} {lettersWord}";

        if (plan.ExemptStudents > 0)
        {
            var exemptWord = plan.ExemptStudents == 1 ? "eximido" : "eximidos";
            text += $" · {plan.ExemptStudents} {exemptWord}";
        }

        return text;
    }

    private async Task TogglePreviousPlansAsync()
    {
        _showPreviousPlans = !_showPreviousPlans;

        if (!_showPreviousPlans || _previousPlansLoaded || FonbecClaim.ChapterId is null)
        {
            return;
        }

        _loadingPreviousPlans = true;

        _previousPlans = await PlannedDeliveryService.GetCompletedPlansAsync(FonbecClaim.ChapterId.Value);
        _previousPlansLoaded = true;

        _loadingPreviousPlans = false;
    }

    private void StartedEditingItem(PlannedDeliveriesListViewModel originalViewModel)
    {
        _plannedDeliveryStartsOn = originalViewModel.PlannedDeliveryStartsOn;
        _originalViewModel = originalViewModel.DeepClone();
    }

    private string ValidatePlanIsNotDuplicate(DateTime? selectedDate)
    {
        if (selectedDate is null)
        {
            return string.Empty;
        }

        var isSameDateAsOriginal =
            selectedDate.Value.Year == _originalViewModel.PlannedDeliveryStartsOn.Year
            && selectedDate.Value.Month == _originalViewModel.PlannedDeliveryStartsOn.Month;

        bool dateAlreadyTaken = _existingPlannedDeliveryDates.Any(date =>
            date.Year == selectedDate.Value.Year
            && date.Month == selectedDate.Value.Month);

        return isSameDateAsOriginal || !dateAlreadyTaken
            ? string.Empty
            : "Ya existe una planificación para este mes y año.";
    }

    private async Task CommittedItemChangesAsync(PlannedDeliveriesListViewModel modifiedViewModel)
    {
        if (_plannedDeliveryStartsOn is null)
        {
            Snackbar.Add("Se debe seleccionar una fecha.", Severity.Warning);
            RevertItemChanges(modifiedViewModel.PlannedDeliveryId);
            return;
        }

        modifiedViewModel.PlannedDeliveryStartsOn = _plannedDeliveryStartsOn.Value;

        if (_originalViewModel.IsEqualTo(modifiedViewModel))
        {
            Snackbar.Add("No se realizaron cambios.", Severity.Info);
            return;
        }

        var updatePlannedDeliveryInputModel = new UpdatePlannedDeliveryInputModel(
            modifiedViewModel.PlannedDeliveryId,
            modifiedViewModel.PlannedDeliveryStartsOn,
            modifiedViewModel.Notes,
            FonbecClaim.UserId
        );

        Loading = true;

        var result = await PlannedDeliveryService.UpdatePlannedDeliveryAsync(updatePlannedDeliveryInputModel);

        Loading = false;

        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo actualizar la planificación de envíos.", Severity.Error);
            RevertItemChanges(modifiedViewModel.PlannedDeliveryId);
            return;
        }

        _previousPlans.Single(vm => vm.PlannedDeliveryId == modifiedViewModel.PlannedDeliveryId).LastUpdatedOnUtc = DateTime.Now;
        _existingPlannedDeliveryDates = await PlannedDeliveryService.GetPlannedDeliveryDatesAsync(FonbecClaim.ChapterId);
    }

    private void RevertItemChanges(int plannedDeliveryId)
    {
        var index = _previousPlans.FindIndex(vm => vm.PlannedDeliveryId == plannedDeliveryId);
        if (index >= 0)
        {
            _previousPlans[index] = _originalViewModel.DeepClone();
            _plannedDeliveryStartsOn = _originalViewModel.PlannedDeliveryStartsOn;
        }
    }
}