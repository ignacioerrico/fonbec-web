using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.PlannedDeliveries.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.PlannedDelivery;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.PlannedDeliveries;

[PageMetadata(nameof(PlannedDeliveryCreate), "Create planned delivery", [FonbecRole.Admin, FonbecRole.Manager])]
public partial class PlannedDeliveryCreate : AuthenticationRequiredComponentBase
{
    private readonly PlannedDeliveryCreateBindModel _bindModel = new();

    private bool _formValidationSucceeded;
    public bool Completed { get; set; } = false;

    private DateTime _minDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
    private List<DateTime> _viewDates = [];

    private bool _saving;
    private bool SaveButtonDisabled => Loading
                               || _saving
                               || !_formValidationSucceeded;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Loading = true;
        _viewDates = await PlannedDeliveryService.GetPlannedDeliveryDatesAsync(FonbecClaim.ChapterId);
        Loading = false;

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

    [Inject]
    public IPlannedDeliveryService PlannedDeliveryService { get; set; } = null!;

    private async Task Save()
    {
        _saving = true;

        var createPlannedDeliveryInputModel = new CreatePlannedDeliveryInputModel(
            _bindModel.StartsOn,
            _bindModel.Completed,
            FonbecClaim.ChapterId,
            FonbecClaim.UserId,
            _bindModel.PlannedDeliveryNotes
            );

        var result = await PlannedDeliveryService.CreatePlannedDeliveryAsync(createPlannedDeliveryInputModel);
        if (!result.AnyAffectedRows)
        {
            Snackbar.Add(result.Message ?? "Save failed", Severity.Error);
            _saving = false;
            return;
        }

        NavigationManager.NavigateTo(NavRoutes.PlannedDeliveries);
    }

}

