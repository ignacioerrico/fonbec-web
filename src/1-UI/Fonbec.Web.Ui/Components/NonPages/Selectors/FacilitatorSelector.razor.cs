using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class FacilitatorSelector
{
    private bool _dataLoaded;

    private readonly List<SelectableModel<int>> _facilitators = [];

    private SelectableModel<int> _selectedFacilitator = null!;

    [Parameter]
    public int SelectedFacilitatorId { get; set; }

    [Parameter]
    public EventCallback<int> SelectedFacilitatorIdChanged { get; set; }

    [Parameter]
    public EventCallback<int> OnFacilitatorsLoaded { get; set; }

    [Inject]
    public IUserService UserService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        var facilitators = await UserService.GetAllUsersInRoleForSelectionAsync(FonbecRole.Uploader);

        _dataLoaded = true;

        _facilitators.AddRange(facilitators);

        if (facilitators.Count > 0)
        {
            _selectedFacilitator = facilitators.First();
            await OnSelectedValueChanged(_selectedFacilitator);
        }

        await OnFacilitatorsLoaded.InvokeAsync(facilitators.Count);

        await base.OnInitializedAsync();
    }

    private async Task OnSelectedValueChanged(SelectableModel<int> selectedFacilitator)
    {
        SelectedFacilitatorId = selectedFacilitator.Key;
        await SelectedFacilitatorIdChanged.InvokeAsync(SelectedFacilitatorId);
    }
}