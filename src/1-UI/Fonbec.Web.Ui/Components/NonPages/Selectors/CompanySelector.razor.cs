using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class CompanySelector
{
    private readonly List<SelectableModel<int>> _companies = [];

    private bool _loading;

    [Parameter]
    public int? SelectedCompanyId { get; set; }

    [Parameter]
    public EventCallback<int?> SelectedCompanyIdChanged { get; set; }

    /// <summary>
    /// Callback invoked when companies are loaded. The int parameter indicates the number of companies loaded.
    /// </summary>
    [Parameter]
    public EventCallback<int> NumberOfCompaniesLoaded { get; set; }

    [Inject]
    private ICompanyService CompanyService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;

        var companies = await CompanyService.GetAllCompaniesForSelectionAsync();

        _loading = false;

        _companies.AddRange(companies);

        await NumberOfCompaniesLoaded.InvokeAsync(companies.Count);

        await base.OnInitializedAsync();
    }

    private async Task<IEnumerable<int?>> Search(string value, CancellationToken token)
    {
        var result = string.IsNullOrEmpty(value)
            ? _companies.Select(c => (int?)c.Key)
            : _companies.Where(c => c.DisplayName.Contains(value, StringComparison.OrdinalIgnoreCase))
                        .Select(c => (int?)c.Key);

        return await Task.FromResult(result);
    }

    private async Task OnSelectedValueChanged(int? selectedCompanyId) =>
        await SelectedCompanyIdChanged.InvokeAsync(selectedCompanyId);

    private string? MapKeyToDisplayName(int? key) =>
        _companies.FirstOrDefault(s => s.Key == key)?.DisplayName;
}