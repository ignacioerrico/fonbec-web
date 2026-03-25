using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models;
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

    private bool _linkSponsors;

    private bool _addPointsOfContact;

    [Inject]
    private ICompanyService CompanyService { get; set; } = null!;

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || (_linkSponsors && _bindModel.Sponsors.Count == 0)
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
        var pointsOfContact = _bindModel.PointsOfContact.Select(poc => new CreatePointOfContactInputModel(
            poc.FirstName,
            poc.LastName,
            poc.NickName,
            poc.Email,
            poc.PhoneNumber,
            FonbecClaim.UserId
        )).ToList();

        _saving = true;

        var companyNameExists = await CompanyService.CompanyNameExistsAsync(_bindModel.CompanyName);

        if (companyNameExists)
        {
            _saving = false;

            Snackbar.Add("Ya existe una empresa con ese nombre.", Severity.Error);
            return;
        }

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
            var sponsors = _linkSponsors
                ? _bindModel.Sponsors
                : [];

            var createCompanyInputModel = new CreateCompanyInputModel(
                _bindModel.CompanyName,
                _bindModel.CompanyEmail,
                _bindModel.CompanyPhoneNumber,
                sponsors,
                FonbecClaim.UserId);

            result = await CompanyService.CreateCompanyAsync(createCompanyInputModel);

            _saving = false;

            if (!result.AnyAffectedRows)
            {
                Snackbar.Add("No se pudo crear la empresa.", Severity.Error);
                return;
            }
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
}