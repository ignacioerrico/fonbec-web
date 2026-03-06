using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class CompanySelector
{
    private readonly List<SelectableModel<int>> _companies = [];

    private bool _dataLoaded;

    [Parameter]
    public int? SelectedCompanyId { get; set; }

    [Parameter]
    public EventCallback<int?> SelectedCompanyIdChanged { get; set; }

    /// <summary>
    /// Callback invoked when companies are loaded. The int parameter indicates the number of companies loaded.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnCompaniesLoaded { get; set; }

    [Inject]
    private ICompanyService CompanyService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _dataLoaded = false;

        var companies = await CompanyService.GetAllCompaniesForSelectionAsync();

        _dataLoaded = true;

        _companies.AddRange(companies);

        await OnCompaniesLoaded.InvokeAsync(companies.Count);

        await base.OnInitializedAsync();
    }

    private async Task OnSelectedValueChanged(int? selectedCompanyId) =>
        await SelectedCompanyIdChanged.InvokeAsync(selectedCompanyId);

    private string? MapKeyToDisplayName(int? key) =>
        _companies.FirstOrDefault(s => s.Key == key)?.DisplayName ?? string.Empty;
}