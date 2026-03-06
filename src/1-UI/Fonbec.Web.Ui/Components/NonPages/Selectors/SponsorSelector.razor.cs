using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class SponsorSelector
{
    private readonly List<SelectableModel<int>> _sponsors = [];

    private bool _dataLoaded;

    [Parameter]
    public int? ChapterId { get; set; }

    [Parameter]
    public int SelectedSponsorId { get; set; }

    [Parameter]
    public EventCallback<int> SelectedSponsorIdChanged { get; set; }

    /// <summary>
    /// Callback invoked when sponsors are loaded. The int parameter indicates the number of sponsors loaded.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnSponsorsLoaded { get; set; }

    [Inject]
    public ISponsorService SponsorService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _dataLoaded = false;

        var sponsors = await SponsorService.GetAllSponsorsForSelectionAsync(ChapterId);

        _dataLoaded = true;

        _sponsors.AddRange(sponsors);

        await OnSponsorsLoaded.InvokeAsync(sponsors.Count);

        if (sponsors.Count > 0)
        {
            if (SelectedSponsorId == 0)
            {
                SelectedSponsorId = sponsors.First().Key;
            }

            await OnSelectedValueChanged(SelectedSponsorId);
        }

        await base.OnInitializedAsync();
    }

    private async Task OnSelectedValueChanged(int selectedSponsorId) =>
        await SelectedSponsorIdChanged.InvokeAsync(selectedSponsorId);

    private string? MapKeyToDisplayName(int key) =>
        _sponsors.FirstOrDefault(s => s.Key == key)?.DisplayName;
}