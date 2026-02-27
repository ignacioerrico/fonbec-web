using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Chapters.Input;
using Fonbec.Web.Logic.Models.Companies.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.Company;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace Fonbec.Web.Ui.Components.Pages.Companies;

[PageMetadata(nameof(CompanyCreate), "Crear y actualizar empresas", [FonbecRole.Admin, FonbecRole.Manager])]
public partial class CompanyCreate : AuthenticationRequiredComponentBase
{
    private readonly CompanyCreateBindModel _bindModel = new();

    private bool _anySponsors;

    private bool _formValidationSucceeded;

    private bool _saving;

    [Inject]
    private ICompanyService CompanyService { get; set; } = null!;
    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || !_formValidationSucceeded;

    private async Task Save()
    {
        _saving = true;

        var createCompanyInputModel = new CreateCompanyInputModel(
            _bindModel.CompanyName,
            _bindModel.CompanyPhoneNumber,
            _bindModel.CompanyEmail,
            FonbecClaim.UserId);

        var duplicatedCompany = await CompanyService.GetCompanyByNameAsync(createCompanyInputModel.CompanyName);
        if (duplicatedCompany == null)
        {
            var result = await CompanyService.CreateCompanyAsync(createCompanyInputModel);
            if (!result.AnyAffectedRows)
            {
                Snackbar.Add("No se pudo crear la empresa.", Severity.Error);
            }

            _saving = false;

            NavigationManager.NavigateTo(NavRoutes.Companies);
        }
        else
        {
            Snackbar.Add("Ya existe una empresa con ese nombre.", Severity.Error);
        }
        _saving = false;

    }
}
