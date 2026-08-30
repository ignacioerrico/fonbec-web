using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class FacilitatorSelector
{
    private readonly List<SelectableModel<int>> _facilitators = [];
    private int _loadId;
    private bool _loading;
    private int? _lastChapterId;

    [Parameter]
    public bool SelectFirstItemOnLoad { get; set; }

    [Parameter]
    public int SelectedFacilitatorId { get; set; }

    [Parameter]
    public EventCallback<int> SelectedFacilitatorIdChanged { get; set; }

    [Parameter]
    public int? ChapterId { get; set; }

    /// <summary>
    /// Callback invoked when facilitators are loaded. The int parameter indicates the number of facilitators loaded.
    /// </summary>
    [Parameter]
    public EventCallback<int> NumberOfFacilitatorsLoaded { get; set; }

    [Inject]
    public IUserService UserService { get; set; } = null!;

    protected override async Task OnParametersSetAsync()
    {
        if (ChapterId != _lastChapterId)
        {
            _lastChapterId = ChapterId;
            await LoadFacilitatorsAsync();
        }
        await base.OnParametersSetAsync();
    }

    private async Task LoadFacilitatorsAsync()
    {
        var loadId = ++_loadId;
        _loading = true;
        _facilitators.Clear();

        List<SelectableModel<int>> facilitators = [];
        if (ChapterId is > 0)
        {
            facilitators = await UserService.GetAllUsersInRoleForSelectionAsync(FonbecRole.Uploader, ChapterId);
        }

        if (loadId != _loadId)
        {
            return;
        }

        _facilitators.AddRange(facilitators);

        // Clear selection if current facilitator is not in the new chapter's list
        if (SelectedFacilitatorId != 0 && !_facilitators.Any(f => f.Key == SelectedFacilitatorId))
        {
            SelectedFacilitatorId = 0;
            await SelectedFacilitatorIdChanged.InvokeAsync(0);
        }

        if (SelectFirstItemOnLoad && _facilitators.Count > 0 && SelectedFacilitatorId == 0)
        {
            SelectedFacilitatorId = _facilitators.First().Key;
            await OnSelectedValueChanged(SelectedFacilitatorId);
        }

        _loading = false;
        await NumberOfFacilitatorsLoaded.InvokeAsync(_facilitators.Count);
    }

    private async Task<IEnumerable<int>> Search(string value, CancellationToken token)
    {
        var result = string.IsNullOrEmpty(value)
            ? _facilitators.Select(c => c.Key)
            : _facilitators.Where(c => c.DisplayName.Contains(value, StringComparison.OrdinalIgnoreCase))
                           .Select(c => c.Key);

        return await Task.FromResult(result);
    }

    private async Task OnSelectedValueChanged(int selectedFacilitatorId) =>
        await SelectedFacilitatorIdChanged.InvokeAsync(selectedFacilitatorId);

    private string? MapKeyToDisplayName(int key) =>
        _facilitators.FirstOrDefault(s => s.Key == key)?.DisplayName;
}