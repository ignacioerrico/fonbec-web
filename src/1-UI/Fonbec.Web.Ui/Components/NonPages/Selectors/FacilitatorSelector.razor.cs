using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class FacilitatorSelector
{
    private bool _dataLoaded;

    private readonly List<SelectableModel<int>> _facilitators = [];

    [Parameter]
    public int SelectedFacilitatorId { get; set; }

    [Parameter]
    public EventCallback<int> SelectedFacilitatorIdChanged { get; set; }

    /// <summary>
    /// Callback invoked when facilitators are loaded. The int parameter indicates the number of facilitators loaded.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnFacilitatorsLoaded { get; set; }

    [Inject]
    public IUserService UserService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        var facilitators = await UserService.GetAllUsersInRoleForSelectionAsync(FonbecRole.Uploader);

        _facilitators.AddRange(facilitators);

        _dataLoaded = true;

        if (SelectedFacilitatorId == 0 && _facilitators.Count > 0)
        {
            SelectedFacilitatorId = _facilitators[0].Key;
        }

        await OnSelectedValueChanged(SelectedFacilitatorId);
        
        await OnFacilitatorsLoaded.InvokeAsync(facilitators.Count);
    }

    private async Task OnSelectedValueChanged(int selectedFacilitatorId)
    {
        await SelectedFacilitatorIdChanged.InvokeAsync(selectedFacilitatorId);
    }
}