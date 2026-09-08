using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Companies;
using Fonbec.Web.Logic.Models.Companies.Input;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Companies;

[PageMetadata(nameof(CompaniesList), "Lista de empresas", [FonbecRole.Manager])]
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

        if (modifiedViewModel.CompanyName != _originalViewModel.CompanyName
            && await CompanyService.CompanyNameExistsAsync(modifiedViewModel.CompanyName, modifiedViewModel.CompanyId))
        {
            Snackbar.Add("Ya existe una empresa con ese nombre.", Severity.Error);
            RevertItemChanges(modifiedViewModel.CompanyId);
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
            Snackbar.Add("No se pudo actualizar la empresa.", Severity.Error);
            RevertItemChanges(modifiedViewModel.CompanyId);
            return;
        }

        _viewModels.Single(vm => vm.CompanyId == modifiedViewModel.CompanyId).LastUpdatedOnUtc = DateTime.Now;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial System.Text.RegularExpressions.Regex EmailRegex();

    private static string? ValidateEmailFormat(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return EmailRegex().IsMatch(email) ? null : "Correo inválido.";
    }

    private static string? ValidatePhoneFormat(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        ReadOnlySpan<char> trimmedPhone = phone.AsSpan().Trim();

        if (trimmedPhone.Length < 7 || !trimmedPhone.ContainsAnyExcept("+0123456789"))
            return "Número de teléfono inválido.";
     
        if (trimmedPhone.Contains('+') && trimmedPhone[0] != '+')
            return "Número de teléfono inválido.";

        return null;
    }

    private void RevertItemChanges(int companyId)
    {
        var index = _viewModels.FindIndex(vm => vm.CompanyId == companyId);
        if (index >= 0)
        {
            _viewModels[index] = _originalViewModel.DeepClone();
        }
    }
}