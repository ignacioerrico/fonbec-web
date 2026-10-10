using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Companies;
using Fonbec.Web.Logic.Models.Companies.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Util;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Companies;

[PageMetadata(nameof(CompanyRelations), "Contactos y padrinos de una empresa", [FonbecRole.Manager])]
public partial class CompanyRelations : AuthenticationRequiredComponentBase
{
    private readonly List<CompanyContactEditModel> _contacts = [];

    private readonly List<SelectableModel<int>> _sponsors = [];

    private readonly List<SelectableModel<int>> _sponsorPool = [];

    private List<CompanyContactEditModel> _originalContacts = [];

    private List<int> _originalSponsorIds = [];

    private MudForm _form = null!;

    private MudAutocomplete<SelectableModel<int>> _sponsorAutocomplete = null!;

    private bool _pendingValidation;

    private bool _saving;

    private bool _notFound;

    private string? _companyName;

    [Parameter]
    public int CompanyId { get; set; }

    [Inject]
    private ICompanyService CompanyService { get; set; } = null!;

    private bool SaveButtonDisabled => Loading || _saving || !ContactsAreValid;

    private bool ContactsAreValid =>
        _contacts.All(contact =>
            !string.IsNullOrWhiteSpace(contact.FirstName)
            && ContactFieldValidator.ValidateEmail(contact.Email) is null
            && ContactFieldValidator.ValidatePhone(contact.PhoneNumber) is null);

    private bool CanAddContact =>
        _contacts.All(contact => !string.IsNullOrWhiteSpace(contact.FirstName));

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        var relations = await CompanyService.GetCompanyRelationsAsync(CompanyId);
        if (relations is null)
        {
            _notFound = true;
            Loading = false;
            return;
        }

        _companyName = relations.CompanyName;
        _contacts.AddRange(relations.Contacts.Select(CompanyContactEditModel.From));
        _sponsors.AddRange(relations.Sponsors);
        _sponsorPool.AddRange(relations.Sponsors);

        var available = await CompanyService.GetSponsorsAvailableToLinkAsync();
        foreach (var sponsor in available)
        {
            if (_sponsorPool.All(existing => existing.Key != sponsor.Key))
            {
                _sponsorPool.Add(sponsor);
            }
        }

        RememberOriginal();
        _pendingValidation = true;
        Loading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_pendingValidation)
        {
            return;
        }

        _pendingValidation = false;
        await _form.Validate();
    }

    private void AddContact() =>
        _contacts.Add(new CompanyContactEditModel());

    private void RemoveContact(CompanyContactEditModel contact) =>
        _contacts.Remove(contact);

    private void RemoveSponsor(SelectableModel<int> sponsor) =>
        _sponsors.Remove(sponsor);

    private Task<IEnumerable<SelectableModel<int>?>> SearchSponsors(string value, CancellationToken token)
    {
        var linkedIds = _sponsors.Select(sponsor => sponsor.Key).ToHashSet();
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

        if (_sponsors.All(existing => existing.Key != sponsor.Key))
        {
            _sponsors.Add(sponsor);
        }

        await _sponsorAutocomplete.ClearAsync();
    }

    private async Task Save()
    {
        await _form.Validate();
        if (!ContactsAreValid)
        {
            return;
        }

        if (!HasChanges())
        {
            Snackbar.Add("No se realizaron cambios.", Severity.Info);
            return;
        }

        _saving = true;

        var input = new UpdateCompanyRelationsInputModel(
            CompanyId,
            _contacts
                .Where(contact => !string.IsNullOrWhiteSpace(contact.FirstName))
                .Select(contact => contact.ToInput())
                .ToList(),
            _sponsors.ToList(),
            FonbecClaim.UserId);

        var result = await CompanyService.UpdateCompanyRelationsAsync(input);

        _saving = false;

        if (!result.CompanyFound)
        {
            Snackbar.Add("No se encontró la empresa.", Severity.Error);
            return;
        }

        if (result.HasUnknownContacts)
        {
            Snackbar.Add("Hay contactos que ya no pertenecen a la empresa. Volvé a abrir la página.", Severity.Error);
            return;
        }

        if (result.HasMissingSponsors)
        {
            var names = string.Join(", ", result.MissingSponsors!.Select(sponsor => sponsor.SponsorName));
            Snackbar.Add($"No se pudo vincular: {names}. Ya pertenecen a otra empresa o no están activos.", Severity.Error);
            return;
        }

        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo guardar.", Severity.Error);
            return;
        }

        Snackbar.Add("Se guardaron los contactos y padrinos.", Severity.Success);
        NavigationManager.NavigateTo(NavRoutes.Companies);
    }

    private bool HasChanges()
    {
        var sponsorIds = _sponsors.Select(sponsor => sponsor.Key).OrderBy(id => id);
        if (!sponsorIds.SequenceEqual(_originalSponsorIds))
        {
            return true;
        }

        if (_contacts.Count != _originalContacts.Count)
        {
            return true;
        }

        return _contacts
            .OrderBy(contact => contact.Id ?? int.MaxValue)
            .ThenBy(contact => contact.FirstName)
            .Zip(_originalContacts.OrderBy(contact => contact.Id ?? int.MaxValue).ThenBy(contact => contact.FirstName))
            .Any(pair => !pair.First.SameContent(pair.Second));
    }

    private void RememberOriginal()
    {
        _originalContacts = _contacts.Select(contact => contact.Copy()).ToList();
        _originalSponsorIds = _sponsors.Select(sponsor => sponsor.Key).OrderBy(id => id).ToList();
    }

    private static string ContactHeading(CompanyContactEditModel contact)
    {
        var name = $"{contact.FirstName} {contact.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? "Nuevo contacto" : name;
    }

    private sealed class CompanyContactEditModel
    {
        public Guid Key { get; } = Guid.NewGuid();

        public int? Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string NickName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public static CompanyContactEditModel From(CompanyContactViewModel contact) =>
            new()
            {
                Id = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                NickName = contact.NickName,
                Email = contact.Email,
                PhoneNumber = contact.PhoneNumber,
                Notes = contact.Notes,
            };

        public CompanyContactEditModel Copy() =>
            new()
            {
                Id = Id,
                FirstName = FirstName,
                LastName = LastName,
                NickName = NickName,
                Email = Email,
                PhoneNumber = PhoneNumber,
                Notes = Notes,
            };

        public UpdateCompanyContactInputModel ToInput() =>
            new(Id, FirstName, LastName, NickName, Email, PhoneNumber, Notes);

        public bool SameContent(CompanyContactEditModel other) =>
            Id == other.Id
            && Same(FirstName, other.FirstName)
            && Same(LastName, other.LastName)
            && Same(NickName, other.NickName)
            && Same(Email, other.Email)
            && Same(PhoneNumber, other.PhoneNumber)
            && Same(Notes, other.Notes);

        private static bool Same(string left, string right) =>
            string.Equals(left.Trim(), right.Trim(), StringComparison.Ordinal);
    }
}