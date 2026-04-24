using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Companies;
using Fonbec.Web.Logic.Models.Companies.Input;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Companies;

[PageMetadata(nameof(CompaniesList), "Lista de empresas", [FonbecRole.Admin, FonbecRole.Manager])]
public partial class CompaniesList : AuthenticationRequiredComponentBase
{
    private List<CompaniesListViewModel> _viewModels = [];

    private CompaniesListViewModel _originalViewModel = new();

    private string _searchString = string.Empty;

    [Inject]
    public ICompanyService CompanyService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        _viewModels = await CompanyService.GetAllCompaniesAsync();

        Loading = false;
    }
    private bool Filter(CompaniesListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || viewModel.CompanyName.ContainsIgnoringAccents(_searchString);

    private void StartedEditingItem(CompaniesListViewModel originalViewModel) =>
        _originalViewModel = originalViewModel.DeepClone();

    private async Task CommittedItemChangesAsync(CompaniesListViewModel modifiedViewModel)
    {
        if (_originalViewModel.IsEqualTo(modifiedViewModel))
        {
            Snackbar.Add("No se realizaron cambios.", Severity.Info);
            return;
        }

        var updateCompanyInputModel = new UpdateCompanyInputModel(
            modifiedViewModel.CompanyId,
            modifiedViewModel.CompanyName,
            modifiedViewModel.CompanyPhoneNumber,
            modifiedViewModel.CompanyEmail,
            modifiedViewModel.CompanyNotes,
            FonbecClaim.UserId
        );

        Loading = true;

        var result = await CompanyService.UpdateCompanyAsync(updateCompanyInputModel);

        Loading = false;

        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo actualizar la filial.", Severity.Error);
        }

        _viewModels.Single(vm => vm.CompanyId == modifiedViewModel.CompanyId).LastUpdatedOnUtc = DateTime.Now;
    }
}