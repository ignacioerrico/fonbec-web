using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.PlannedDeliveries;
using Fonbec.Web.Logic.Models.PlannedDeliveries.Input;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.PlannedDeliveries;

[PageMetadata(nameof(PlannedDeliveriesList), "Lista de planificaciones de envíos", [FonbecRole.Manager])]
public partial class PlannedDeliveriesList : AuthenticationRequiredComponentBase
{
    // Emojis
    // Yellow circle = Planned Delivery created but not started
    // None O'Clock = Planned Delivery in progress (started)
    // CheckMark = Planned Delivery completed
    private readonly string YellowCircleEmoji = char.ConvertFromUtf32(0x1F7E1);
    private readonly string NineOClockEmoji = char.ConvertFromUtf32(0x1F558);
    private readonly string CheckMarkEmoji = char.ConvertFromUtf32(0x2705);

    private List<PlannedDeliveriesListViewModel> _viewModels = [];
    
    // Used to tell whether any changes have been made
    private PlannedDeliveriesListViewModel _originalViewModel = new();

    private List<DateTime> _existingPlannedDeliveryDates = [];

    // This field exists because the PlannedDeliveryStartsOn of the View Model is not nullable (otherwise, that property should be used).
    // (And of course that property must not be nullable, because a Planned Delivery must have a starting date.)
    private DateTime? _plannedDeliveryStartsOn;
    
    // It's not possible to choose a month before the current one.
    private readonly DateTime _minDate = new(DateTime.Now.Year, DateTime.Now.Month, 1);

    [Inject]
    public IPlannedDeliveryService PlannedDeliveryService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        _viewModels = await PlannedDeliveryService.GetAllPlannedDeliveriesAsync();
        
        // When editing a Planned Delivery, to avoid duplicating dates.
        _existingPlannedDeliveryDates = await PlannedDeliveryService.GetPlannedDeliveryDatesAsync(FonbecClaim.ChapterId);

        Loading = false;
    }

    private void StartedEditingItem(PlannedDeliveriesListViewModel originalViewModel)
    {
        // Set the date of the date picker
        _plannedDeliveryStartsOn = originalViewModel.PlannedDeliveryStartsOn;
        
        // Save original state to tell later if changes have been made
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

        // It is valid to not change the StartsOn date (necessary if you only want to change the notes)
        return isSameDateAsOriginal || !dateAlreadyTaken
            ? string.Empty
            : "Ya existe una planificación para este mes y año.";
    }

    private async Task CommittedItemChangesAsync(PlannedDeliveriesListViewModel modifiedViewModel)
    {
        if (_plannedDeliveryStartsOn is null)
        {
            Snackbar.Add("Se debe seleccionar una fecha.", Severity.Warning);
            return;
        }

        // Update the view model to the selected date
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
        }

        _viewModels.Single(vm => vm.PlannedDeliveryId == modifiedViewModel.PlannedDeliveryId).LastUpdatedOnUtc = DateTime.Now;
    }
}