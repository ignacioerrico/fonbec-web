using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class SponsorSelector
{
    private readonly List<SelectableModel<int>> _sponsors = [];

    private bool _loading;

    [Parameter]
    public int? ChapterId { get; set; }

    [Parameter]
    public bool SelectFirstItemOnLoad { get; set; }

    // voy a cambiar esto...
    [Parameter]
    public int? SelectedSponsorId { get; set; }

    [Parameter]
    public EventCallback<int?> SelectedSponsorIdChanged { get; set; }

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
            if (SelectedSponsorId == 0)
            {
                SelectedSponsorId = sponsors.First().Key;
            }

            await OnSelectedValueChanged(SelectedSponsorId);
        }

        await base.OnInitializedAsync();
    }

    // CAMBIOS de tipo
    private async Task<IEnumerable<int?>> Search(string value, CancellationToken token)
    {
        var result = string.IsNullOrEmpty(value)
            ? _sponsors.Select(c => (int?)c.Key)
            : _sponsors.Where(c => c.DisplayName.Contains(value, StringComparison.OrdinalIgnoreCase))
                       .Select(c => (int?)c.Key);

        return await Task.FromResult(result);
    }

    private async Task OnSelectedValueChanged(int? selectedSponsorId) =>
        await SelectedSponsorIdChanged.InvokeAsync(selectedSponsorId);

    private string? MapKeyToDisplayName(int? key) =>
        _sponsors.FirstOrDefault(s => s.Key == key)?.DisplayName;
}