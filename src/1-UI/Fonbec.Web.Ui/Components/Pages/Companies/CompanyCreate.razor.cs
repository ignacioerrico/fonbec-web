using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models;
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

    private bool _addPointsOfContact;

    private bool _linkSponsors;

    [Inject]
    private ICompanyService CompanyService { get; set; } = null!;

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || (_linkSponsors && _bindModel.Sponsors.Count == 0)
                                       || !_formValidationSucceeded;

    private bool CanAddPointOfContact =>
        _bindModel.PointsOfContact.All(poc =>
            !string.IsNullOrWhiteSpace(poc.PocFirstName));

    private void AddPointOfContact() =>
        _bindModel.PointsOfContact.Add(new());

    private void RemovePointOfContact(Guid tempId) =>
        _bindModel.PointsOfContact.RemoveAll(poc => poc.TempId == tempId);

    private string QtySponsorsInfo => _bindModel.Sponsors.Count switch
    {
        0 => "No hay padrinos vinculados.",
        1 => "<strong>Un</strong> padrino vinculado.",
        _ => $"<strong>{_bindModel.Sponsors.Count}</strong> padrinos vinculados."
    };

    private void RemoveSponsor(SelectableModel<int> sponsor) =>
        _bindModel.Sponsors.Remove(sponsor);

    private void RemoveAllSponsors() =>
        _bindModel.Sponsors.Clear();

    private async Task Save()
    {
        _saving = true;

        var companyNameExists = await CompanyService.CompanyNameExistsAsync(_bindModel.CompanyName);
        if (companyNameExists)
        {
            _saving = false;

            Snackbar.Add("Ya existe una empresa con ese nombre.", Severity.Error);
            return;
        }

        var pointsOfContact = _addPointsOfContact
            ? _bindModel.PointsOfContact
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
                .ToList()
            : [];

        var sponsors = _linkSponsors
            ? _bindModel.Sponsors
            : [];

        var createCompanyInputModel = new CreateCompanyInputModel(
            _bindModel.CompanyName,
            _bindModel.CompanyEmail,
            _bindModel.CompanyPhoneNumber,
            _bindModel.CompanyNotes,
            pointsOfContact,
            sponsors,
            FonbecClaim.UserId
        );

        var result = await CompanyService.CreateCompanyAsync(createCompanyInputModel);

        _saving = false;

        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo crear la empresa.", Severity.Error);
            return;
        }

        NavigationManager.NavigateTo(NavRoutes.Companies);
    }

    private void OnSelectedSponsorChanged(SelectableModel<int> sponsor)
    {
        if (sponsor is null || sponsor.Key == 0)
        {
            return;
        }

        if (_bindModel.Sponsors.Contains(sponsor))
        {
            Snackbar.Add("El padrino ya fue agregado.", Severity.Warning);
            return;
        }

        _bindModel.Sponsors.Add(sponsor);
    }
}