using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.PlannedDeliveries.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.PlannedDelivery;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.PlannedDeliveries;

[PageMetadata(nameof(PlannedDeliveryCreate), "Crear planificación de envío", [FonbecRole.Manager])]
public partial class PlannedDeliveryCreate : AuthenticationRequiredComponentBase
{
    private readonly PlannedDeliveryCreateBindModel _bindModel = new();

    private readonly DateTime _minDate = new(DateTime.Now.Year, DateTime.Now.Month, 1);

    private bool _formValidationSucceeded;

    private bool _saving;

    private List<DateTime> _existingPlansDates = [];

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || !_formValidationSucceeded;

    [Inject]
    public IPlannedDeliveryService PlannedDeliveryService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (!FonbecClaim.ChapterId.HasValue)
        {
            Snackbar.Add("Esta página requiere un usuario que pertenezca a una filial.", Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.PlannedDeliveries);
        }

        var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        Loading = true;

        _existingPlansDates = await PlannedDeliveryService.GetPlannedDeliveryDatesAsync(FonbecClaim.ChapterId, from: currentMonth);

        Loading = false;
    }

    private string? ValidatePlanIsNotDuplicate(DateTime? selectedDate)
    {
        if (selectedDate is null)
        {
            return null;
        }

        var planDateExists = _existingPlansDates.Any(d =>
            d.Year == selectedDate.Value.Year
            && d.Month == selectedDate.Value.Month);

        return planDateExists
            ? "Ya existe una planificación para este mes y año."
            : null;
    }

    private async Task Save()
    {
        var chapterId = FonbecClaim.ChapterId
                        ?? throw new NullReferenceException(nameof(FonbecClaim.ChapterId));

        if (!_bindModel.PlanStartsOn.HasValue)
        {
            return;
        }

        var createPlannedDeliveryInputModel = new CreatePlannedDeliveryInputModel(
            chapterId,
            _bindModel.PlanStartsOn.Value,
            _bindModel.PlanNotes,
            FonbecClaim.UserId);

        _saving = true;

        var result = await PlannedDeliveryService.CreatePlannedDeliveryAsync(createPlannedDeliveryInputModel);

        _saving = false;

        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo crear la planificación.", Severity.Error);
            return;
        }

        NavigationManager.NavigateTo(NavRoutes.PlannedDeliveries);
    }
}