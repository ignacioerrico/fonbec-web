using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Companies.Input;
using Fonbec.Web.Logic.Models.Results;
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

    private bool _saving;
    [Inject]
    private ICompanyService CompanyService { get; set; } = null!;
    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || !_formValidationSucceeded;


    private void AddPointOfContact()
    {
        _bindModel.PointsOfContact.Add(new PointOfContactBindModel());
    }
    private void RemovePointOfContact(Guid tempId)
    {
        var poc = _bindModel.PointsOfContact.FirstOrDefault(p => p.TempId == tempId);

        if (poc != null)
            _bindModel.PointsOfContact.Remove(poc);
    }

    private async Task Save()
    {
        _saving = true;

        var pointsOfContact = _bindModel.PointsOfContact.Select(poc => new CreatePointOfContactInputModel(
            poc.FirstName,
            poc.LastName,
            poc.NickName,
            poc.Email,
            poc.PhoneNumber,
            FonbecClaim.UserId
        )).ToList();

        CrudResult result;

        if (pointsOfContact.Any())
        {
            var createCompanyInputModel = new CreateCompanyWithPointsOfContactInputModel(
                _bindModel.CompanyName,
                _bindModel.CompanyEmail ?? string.Empty,
                _bindModel.CompanyPhoneNumber ?? string.Empty,
                FonbecClaim.UserId,
                pointsOfContact
            );
            result = await CompanyService.CreateCompanyWithPointsOfContactAsync(createCompanyInputModel);
        }
        else
        {
            var createCompanyInputModel = new CreateCompanyInputModel(
                _bindModel.CompanyName,
                _bindModel.CompanyEmail ?? string.Empty,
                _bindModel.CompanyPhoneNumber ?? string.Empty,
                FonbecClaim.UserId
            );
            result = await CompanyService.CreateCompanyAsync(createCompanyInputModel);
        }

        if (result.AnyAffectedRows)
        {
            Snackbar.Add("Empresa creada exitosamente.", Severity.Success);
            NavigationManager.NavigateTo(NavRoutes.Companies);
        }
        else
        {
            Snackbar.Add("No se pudo crear la empresa.", Severity.Error);
            _saving = false;
        }
    }
}
