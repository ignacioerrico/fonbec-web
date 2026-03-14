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

    private bool _formValidationSucceeded;

    private bool _loadingTable;

    private int selectedSponsorId;

    private bool _saving;

    [Inject]
    private ICompanyService CompanyService { get; set; } = null!;

    [Inject]
    private ISponsorService SponsorService { get; set; } = null!;
    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || !_formValidationSucceeded;
    private bool AddSponsorButtonDisabled => _loadingTable
                                       || selectedSponsorId == 0;
    private async Task Save()
    {
        _saving = true;

        var createCompanyInputModel = new CreateCompanyInputModel(
            _bindModel.CompanyName,
            _bindModel.CompanyEmail,
            _bindModel.CompanyPhoneNumber,
            _bindModel.CompanySponsors,
            FonbecClaim.UserId);

        var companyNameExists = await CompanyService.CompanyNameExistsAsync(createCompanyInputModel.CompanyName);
        if (companyNameExists)
        {
            Snackbar.Add("Ya existe una empresa con ese nombre.", Severity.Error);
        }
        else
        {
            var result = await CompanyService.CreateCompanyAsync(createCompanyInputModel);
            if (!result.AnyAffectedRows)
            {
                Snackbar.Add("No se pudo crear la empresa.", Severity.Error);
            }

            _saving = false;

            NavigationManager.NavigateTo(NavRoutes.Companies);
        }
        _saving = false;

    }

    private async Task AddSponsor()
    {
        _loadingTable = true;

        var sponsorAdd = await SponsorService.GetSponsorByIdAsync(selectedSponsorId);
        if (sponsorAdd == null)
        {
            Snackbar.Add("No se pudo encontrar el padrino seleccionado.", Severity.Error);

        } else if (_bindModel.CompanySponsors.Any(s => s.IsIdenticalTo(sponsorAdd)))
        {
            Snackbar.Add("El padrino seleccionado ya está asociado a la empresa.", Severity.Warning);
        }
        else
        {
            _bindModel.CompanySponsors.Add(sponsorAdd);
        }

        _loadingTable = false;
    }

    private async Task RemoveSponsor(int sponsorId) { 
        _bindModel.CompanySponsors.RemoveAll(s => s.SponsorId == sponsorId);
    }
}
