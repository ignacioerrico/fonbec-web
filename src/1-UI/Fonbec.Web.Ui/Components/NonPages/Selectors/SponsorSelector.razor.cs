using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class SponsorSelector
{
    private readonly List<SelectableModel<int>> _sponsors = [];

    private bool _loading;

    private MudAutocomplete<SelectableModel<int>> _autocomplete = null!;

    [Parameter]
    public int? ChapterId { get; set; }

    [Parameter]
    public bool IsRequired { get; set; }

    [Parameter]
    public bool SelectFirstItemOnLoad { get; set; }

    [Parameter]
    public bool ClearWhenValueChanged { get; set; }

    [Parameter]
    public SelectableModel<int>? SelectedSponsor { get; set; }

    [Parameter]
    public EventCallback<SelectableModel<int>?> SelectedSponsorChanged { get; set; }

    /// <summary>
    /// Callback invoked when sponsors are loaded. The int parameter indicates the number of sponsors loaded.
    /// </summary>
    [Parameter]
    public EventCallback<int> NumberOfSponsorsLoaded { get; set; }

    [Inject]
    public ISponsorService SponsorService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;

        var sponsors = await SponsorService.GetAllSponsorsForSelectionAsync(ChapterId);

        _loading = false;

        _sponsors.AddRange(sponsors);

        await NumberOfSponsorsLoaded.InvokeAsync(sponsors.Count);

        if (SelectFirstItemOnLoad && sponsors.Count > 0)
        {
            if (SelectedSponsor is null || SelectedSponsor.Key == 0)
            {
                SelectedSponsor = sponsors.First();
            }

            await OnSelectedValueChanged(SelectedSponsor);
        }

        await base.OnInitializedAsync();
    }

    private async Task<IEnumerable<SelectableModel<int>?>> Search(string value, CancellationToken token)
    {
        var result = string.IsNullOrEmpty(value)
            ? _sponsors
            : _sponsors.Where(sm => sm.DisplayName.Contains(value, StringComparison.OrdinalIgnoreCase));

        return await Task.FromResult(result);
    }

    private async Task OnSelectedValueChanged(SelectableModel<int>? selectedSponsor)
    {
        await SelectedSponsorChanged.InvokeAsync(selectedSponsor);

        if (ClearWhenValueChanged)
        {
            await _autocomplete.ClearAsync();
        }
    }

    private string? MapKeyToDisplayName(SelectableModel<int> sponsor) =>
        _sponsors.FirstOrDefault(sm => sm == sponsor)?.DisplayName;
}