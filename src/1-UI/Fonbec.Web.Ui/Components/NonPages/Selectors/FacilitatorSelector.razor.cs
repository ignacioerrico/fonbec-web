using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class FacilitatorSelector
{
    private readonly List<SelectableModel<int>> _facilitators = [];

    private bool _dataLoaded;

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
        _dataLoaded = false;

        var facilitators = await UserService.GetAllUsersInRoleForSelectionAsync(FonbecRole.Uploader);

        _dataLoaded = true;

        _facilitators.AddRange(facilitators);

        await OnFacilitatorsLoaded.InvokeAsync(facilitators.Count);

        if (facilitators.Count > 0)
        {
            if (SelectedFacilitatorId == 0)
            {
                SelectedFacilitatorId = facilitators.First().Key;
            }

            await OnSelectedValueChanged(SelectedFacilitatorId);
        }

        await base.OnInitializedAsync();
    }

    private async Task OnSelectedValueChanged(int selectedFacilitatorId) =>
        await SelectedFacilitatorIdChanged.InvokeAsync(selectedFacilitatorId);

    private string? MapKeyToDisplayName(int key) =>
        _facilitators.FirstOrDefault(s => s.Key == key)?.DisplayName;
}