using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class CompanySelector
{
    private bool _dataLoaded;

    private readonly List<SelectableModel<int>> _companies = [];

    [Parameter]
    public int? SelectedCompanyId { get; set; }

    [Parameter]
    public EventCallback<int?> SelectedCompanyIdChanged { get; set; }

    [Inject]
    private ICompanyService CompanyService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        var companies = await CompanyService.GetAllCompaniesForSelectionAsync();

        _companies.AddRange(companies);

        _dataLoaded = true;

        await base.OnInitializedAsync();
    }

    private async Task OnSelectedValueChanged(int? selectedCompanyId)
    {
        await SelectedCompanyIdChanged.InvokeAsync(selectedCompanyId);
    }
}
