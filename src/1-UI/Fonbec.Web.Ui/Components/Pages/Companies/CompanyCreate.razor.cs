using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Companies.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.Company;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Companies;

[PageMetadata(nameof(CompanyCreate), "Crear y actualizar empresas", [FonbecRole.Manager])]
public partial class CompanyCreate : AuthenticationRequiredComponentBase
{
    private readonly CompanyCreateBindModel _bindModel = new();

    private bool _formValidationSucceeded;

    private bool _saving;

    private bool _addPointOfContact;

    [Inject]
    private ICompanyService CompanyService { get; set; } = null!;

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || !_formValidationSucceeded;

    private bool CanAddPointOfContact =>
        _bindModel.PointsOfContact.All(poc =>
            !string.IsNullOrWhiteSpace(poc.PocFirstName));

    private void AddPointOfContact() =>
        _bindModel.PointsOfContact.Add(new());

    private void RemovePointOfContact(Guid tempId) =>
        _bindModel.PointsOfContact.RemoveAll(poc => poc.TempId == tempId);

    private async Task Save()
    {
        var pointsOfContact = _bindModel.PointsOfContact
            .Where(poc => !string.IsNullOrWhiteSpace(poc.PocFirstName))
            .Select(poc =>
                new CreateCompanyPointOfContactInputModel(
                    poc.PocFirstName,
                    poc.PocLastName,
                    poc.PocNickName,
                    poc.PocEmail,
                    poc.PocPhoneNumber,
                    poc.PocNotes
                ))
            .ToList();

        var createCompanyInputModel = new CreateCompanyInputModel(
            _bindModel.CompanyName,
            _bindModel.CompanyEmail,
            _bindModel.CompanyPhoneNumber,
            _bindModel.CompanyNotes,
            pointsOfContact,
            FonbecClaim.UserId
        );

        _saving = true;

        var result = await CompanyService.CreateCompanyAsync(createCompanyInputModel);

        _saving = false;

        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo crear la empresa.", Severity.Error);
            return;
        }

        Snackbar.Add("Empresa creada exitosamente.", Severity.Success);
        NavigationManager.NavigateTo(NavRoutes.Companies);
    }
}