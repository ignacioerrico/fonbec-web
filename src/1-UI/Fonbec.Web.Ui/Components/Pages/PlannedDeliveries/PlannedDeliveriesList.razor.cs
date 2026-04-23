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
    private List<PlannedDeliveriesListViewModel> _viewModels = [];
    private PlannedDeliveriesListViewModel _originalViewModel = new();
    private string _searchString = string.Empty;
    private DateTime _minDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

    private List<DateTime> _viewDates = [];

    [Inject]
    public IPlannedDeliveryService PlannedDeliveryService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        _viewModels = await PlannedDeliveryService.GetAllPlannedDeliveriesAsync();
        _viewDates = await PlannedDeliveryService.GetPlannedDeliveryDatesAsync(FonbecClaim.ChapterId);

        Loading = false;
    }

    private bool Filter(PlannedDeliveriesListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || viewModel.StatusText.ContainsIgnoringAccents(_searchString);

    private void StartedEditingItem(PlannedDeliveriesListViewModel originalViewModel) =>
    _originalViewModel = originalViewModel.DeepClone();

    private async Task CommittedItemChangesAsync(PlannedDeliveriesListViewModel modifiedViewModel)
    {
        if (_originalViewModel.IsEqualTo(modifiedViewModel))
        {
            Snackbar.Add("No se realizaron cambios.", Severity.Info);
            return;
        }

        var updatePlannedDeliveryInputModel = new UpdatePlannedDeliveryInputModel(
            modifiedViewModel.PlannedDeliveryId,
            modifiedViewModel.PlanStartsOn,
            modifiedViewModel.IsPlannedDeliveryCompleted,
            modifiedViewModel.Notes,
            FonbecClaim.UserId
        );

        Loading = true;

        var result = await PlannedDeliveryService.UpdatePlannedDeliveryAsync(updatePlannedDeliveryInputModel);

        Loading = false;

        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo actualizar la filial.", Severity.Error);
        }

        _viewModels.Single(vm => vm.PlannedDeliveryId == modifiedViewModel.PlannedDeliveryId).LastUpdatedOnUtc = DateTime.Now;
    }

    private string? ValidateMonth(DateTime? selectedDate)
    {
        if (selectedDate == null) return null;

        bool alreadyExists = _viewDates.Any(d =>
            d.Year == selectedDate.Value.Year &&
            d.Month == selectedDate.Value.Month);

        return alreadyExists
            ? $"A delivery is already planned for {selectedDate.Value:MMMM yyyy}"
            : null;
    }
}