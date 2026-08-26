using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class FacilitatorSelector
{
    private readonly List<SelectableModel<int>> _facilitators = [];

    private bool _loading;
    private int? _previousChapterId;

    [Parameter]
    public bool SelectFirstItemOnLoad { get; set; }

    [Parameter]
    public int SelectedFacilitatorId { get; set; }

    [Parameter]
    public EventCallback<int> SelectedFacilitatorIdChanged { get; set; }

    /// <summary>
    /// Callback invoked when facilitators are loaded. The int parameter indicates the number of facilitators loaded.
    /// </summary>
    [Parameter]
    public EventCallback<int> NumberOfFacilitatorsLoaded { get; set; }

    [Inject]
    public IUserService UserService { get; set; } = null!;

    [Parameter]
    public int? ChapterId { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // Reload only if ChapterId has changed or on first parameter load
        if (_previousChapterId != ChapterId)
        {
            _previousChapterId = ChapterId;
            await LoadFacilitatorsAsync();
        }
    }

    private async Task LoadFacilitatorsAsync()
    {
        _loading = true;
        _facilitators.Clear();

        var facilitators = await UserService.GetAllUsersInRoleForSelectionAsync(FonbecRole.Uploader);

        if (ChapterId.HasValue && ChapterId.Value > 0)
        {
            var chapterUsers = await UserService.GetAllUsersAsync(ChapterId.Value);
            var chapterUserIds = chapterUsers.Select(u => u.UserId).ToHashSet();
            facilitators = facilitators.Where(f => chapterUserIds.Contains(f.Key)).ToList();
        }

        _loading = false;
        _facilitators.AddRange(facilitators);

        await NumberOfFacilitatorsLoaded.InvokeAsync(_facilitators.Count);
    }
    protected override async Task OnInitializedAsync()
    {
        _loading = true;

        var facilitators = await UserService.GetAllUsersInRoleForSelectionAsync(FonbecRole.Uploader);

        _loading = false;

        _facilitators.AddRange(facilitators);

        await NumberOfFacilitatorsLoaded.InvokeAsync(facilitators.Count);

        if (SelectFirstItemOnLoad && facilitators.Count > 0)
        {
            if (SelectedFacilitatorId == 0)
            {
                SelectedFacilitatorId = facilitators.First().Key;
            }

            await OnSelectedValueChanged(SelectedFacilitatorId);
        }

        await base.OnInitializedAsync();
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