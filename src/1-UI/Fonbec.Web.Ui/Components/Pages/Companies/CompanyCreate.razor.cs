using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Companies;
using Fonbec.Web.Logic.Models.Companies.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Util;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.Company;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Companies;

[PageMetadata(nameof(CompanyCreate), "Crear y actualizar empresas", [FonbecRole.Manager])]
public partial class CompanyCreate : AuthenticationRequiredComponentBase
{
    private readonly CompanyCreateBindModel _bindModel = new();

    private readonly List<SelectableModel<int>> _sponsorPool = [];

    private MudForm _form = null!;

    private MudAutocomplete<SelectableModel<int>> _sponsorAutocomplete = null!;

    private bool _saving;

    private bool _sponsorsLoading;

    [Inject]
    private ICompanyService CompanyService { get; set; } = null!;

    private bool SaveButtonDisabled =>
        Loading || _saving || _sponsorsLoading || !CompanyFieldsAreValid;

    private bool CompanyFieldsAreValid =>
        CompanyFieldValidator.IsValidName(_bindModel.CompanyName)
        && ContactFieldValidator.ValidateEmail(_bindModel.CompanyEmail) is null
        && ContactFieldValidator.ValidatePhone(_bindModel.CompanyPhoneNumber) is null;

    private bool ContactsAreValid =>
        _bindModel.PointsOfContact.All(contact =>
            !string.IsNullOrWhiteSpace(contact.PocFirstName)
            && ContactFieldValidator.ValidateEmail(contact.PocEmail) is null
            && ContactFieldValidator.ValidatePhone(contact.PocPhoneNumber) is null);

    private bool CanAddPointOfContact =>
        _bindModel.PointsOfContact.All(contact => !string.IsNullOrWhiteSpace(contact.PocFirstName));

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        _sponsorsLoading = true;
        _sponsorPool.AddRange(await CompanyService.GetSponsorsAvailableToLinkAsync());
        _sponsorsLoading = false;
    }

    private void AddPointOfContact() =>
        _bindModel.PointsOfContact.Add(new());

    private void SetContactFirstName(CompanyCreatePointOfContactBindModel contact, string? value) =>
        contact.PocFirstName = value ?? string.Empty;

    private void RemovePointOfContact(CompanyCreatePointOfContactBindModel contact) =>
        _bindModel.PointsOfContact.Remove(contact);

    private void RemoveSponsor(SelectableModel<int> sponsor) =>
        _bindModel.Sponsors.Remove(sponsor);

    private Task<IEnumerable<SelectableModel<int>?>> SearchSponsors(string value, CancellationToken token)
    {
        var linkedIds = _bindModel.Sponsors.Select(sponsor => sponsor.Key).ToHashSet();
        IEnumerable<SelectableModel<int>?> available = _sponsorPool.Where(sponsor => !linkedIds.Contains(sponsor.Key));
        if (!string.IsNullOrWhiteSpace(value))
        {
            available = available.Where(sponsor =>
                sponsor!.DisplayName.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(available);
    }

    private async Task AddSponsor(SelectableModel<int>? sponsor)
    {
        if (sponsor is null || sponsor.Key == 0)
        {
            return;
        }

        if (_bindModel.Sponsors.All(existing => existing.Key != sponsor.Key))
        {
            _bindModel.Sponsors.Add(sponsor);
        }

        await _sponsorAutocomplete.ClearAsync();
    }

    private async Task Save()
    {
        await _form.Validate();
        if (!CompanyFieldsAreValid || !ContactsAreValid)
        {
            return;
        }

        _saving = true;
        var companyNameExists = await CompanyService.CompanyNameExistsAsync(_bindModel.CompanyName);
        if (companyNameExists)
        {
            _saving = false;

            Snackbar.Add("Ya existe una empresa con ese nombre.", Severity.Error);
            return;
        }

        var pointsOfContact = _bindModel.PointsOfContact
            .Where(contact => !string.IsNullOrWhiteSpace(contact.PocFirstName))
            .Select(contact =>
                new CreateCompanyPointOfContactInputModel(
                    contact.PocFirstName,
                    contact.PocLastName,
                    contact.PocNickName,
                    contact.PocEmail,
                    contact.PocPhoneNumber,
                    contact.PocNotes
                ))
            .ToList();

        var createCompanyInputModel = new CreateCompanyInputModel(
            _bindModel.CompanyName,
            _bindModel.CompanyEmail,
            _bindModel.CompanyPhoneNumber,
            _bindModel.CompanyNotes,
            pointsOfContact,
            _bindModel.Sponsors,
            FonbecClaim.UserId
        );

        var result = await CompanyService.CreateCompanyAsync(createCompanyInputModel);

        _saving = false;

        if (result.HasMissingSponsors)
        {
            var missingSponsorDetails = string.Join(
                ", ",
                result.MissingSponsors!.Select(s => $"{s.SponsorName} (ID {s.SponsorId})"));

            Snackbar.Add(
                $"No se pudo crear la empresa. Los siguientes padrinos no están disponibles para vincular: {missingSponsorDetails}.",
                Severity.Error);
            return;
        }

        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo crear la empresa.", Severity.Error);
            return;
        }

        NavigationManager.NavigateTo(NavRoutes.Companies);
    }

    private static string? ValidateNameFormat(string? name) =>
        CompanyFieldValidator.IsValidName(name) ? null : "Nombre inválido.";

    private static string ContactHeading(CompanyCreatePointOfContactBindModel contact)
    {
        var name = $"{contact.PocFirstName} {contact.PocLastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? "Nuevo contacto" : name;
    }
}