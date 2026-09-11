using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Sponsorships;
using Fonbec.Web.Logic.Models.Sponsorships.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.Sponsorship;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Sponsorships;

[PageMetadata(nameof(SponsorshipCreate), "Asignar padrino a becario", [FonbecRole.Manager])]
public partial class SponsorshipCreate : AuthenticationRequiredComponentBase
{
    private readonly SponsorshipCreateBindModel _bindModel = new();

    private bool _saving;
    private bool _anySponsors;
    private bool _anyCompanies;
    private bool _formValidationSucceeded;
    private bool _isEndDateKnown;
    private bool _studentNotFound;
    private bool _checkingPeriod;
    private int _periodCheckVersion;
    private string _studentDisplayName = string.Empty;
    private SponsorshipPeriodStatus _periodStatus;

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || _checkingPeriod
                                       || _studentNotFound
                                       || _periodStatus == SponsorshipPeriodStatus.OverlapsExisting
                                       || !AnySponsorsOrCompanies
                                       || !_formValidationSucceeded
                                       || !DateSelectionIsValid;

    private bool DateSelectionIsValid =>
        _bindModel.SponsorshipStartDate is DateTime start
        && (!_isEndDateKnown
            || (_bindModel.SponsorshipEndDate is DateTime end && start < end));

    private bool AnySponsorsOrCompanies =>
        _bindModel.SponsorshipType == SponsorshipType.Sponsor
            ? _anySponsors
            : _anyCompanies;

    private string PageTitle =>
        string.IsNullOrEmpty(_studentDisplayName)
            ? "Asignar padrino"
            : $"Asignar padrino a {_studentDisplayName}";

    private string SponsorshipPartyLabel =>
        _bindModel.SponsorshipType == SponsorshipType.Sponsor
            ? "el padrino seleccionado"
            : "la empresa seleccionada";

    private string PrimaryActionLabel =>
        _periodStatus == SponsorshipPeriodStatus.ExtendsExisting
            ? "Extender apadrinamiento"
            : "Asignar";

    [Parameter]
    public int StudentId { get; set; }

    [Inject]
    public ISponsorshipService SponsorshipService { get; set; } = null!;

    [Inject]
    public IStudentService StudentService { get; set; } = null!;

    protected override async Task OnParametersSetAsync()
    {
        Loading = true;
        try
        {
            await base.OnParametersSetAsync();
            await LoadStudentAsync();
        }
        finally
        {
            Loading = false;
        }
    }

    private async Task<bool> LoadStudentAsync()
    {
        if (FonbecClaim is not { ChapterId: int chapterId })
        {
            _studentNotFound = true;
            _studentDisplayName = string.Empty;
            return false;
        }

        _studentDisplayName =
            await StudentService.GetActiveStudentDisplayNameInChapterAsync(StudentId, chapterId)
            ?? string.Empty;
        _studentNotFound = string.IsNullOrEmpty(_studentDisplayName);
        return !_studentNotFound;
    }

    private async Task NumberOfSponsorsLoaded(int sponsorsCount) =>
        _anySponsors = sponsorsCount > 0;

    private async Task NumberOfCompaniesLoaded(int companiesCount) =>
        _anyCompanies = companiesCount > 0;

    private async Task OnIsEndDateKnownCheckBoxChanged(bool isEndDateKnown)
    {
        _isEndDateKnown = isEndDateKnown;

        // This guarantees that the end date is null if it is not known
        if (!isEndDateKnown)
        {
            _bindModel.SponsorshipEndDate = null;
        }

        await RefreshPeriodStatusAsync();
    }

    private async Task OnEndDateChanged(DateTime? endDate)
    {
        _bindModel.SponsorshipEndDate = endDate is DateTime d
            ? new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month))
            : null;

        await RefreshPeriodStatusAsync();
    }

    private async Task OnStartDateChanged(DateTime? startDate)
    {
        _bindModel.SponsorshipStartDate = startDate is DateTime d
            ? new DateTime(d.Year, d.Month, 1)
            : null;

        await RefreshPeriodStatusAsync();
    }

    private async Task OnSponsorChanged(SelectableModel<int>? sponsor)
    {
        _bindModel.SelectedSponsor = sponsor;
        await RefreshPeriodStatusAsync();
    }

    private async Task OnCompanyChanged(int? companyId)
    {
        _bindModel.SelectedCompanyId = companyId;
        await RefreshPeriodStatusAsync();
    }

    private async Task OnSponsorshipTypeChanged(SponsorshipType sponsorshipType)
    {
        _bindModel.SponsorshipType = sponsorshipType;

        // This ensures that only one is not null (and either a sponsor or a company is selected)
        if (sponsorshipType == SponsorshipType.Sponsor)
        {
            _bindModel.SelectedCompanyId = null;
        }
        else if (sponsorshipType == SponsorshipType.Company)
        {
            _bindModel.SelectedSponsor = null;
        }

        await RefreshPeriodStatusAsync();
    }

    private async Task RefreshPeriodStatusAsync()
    {
        var checkVersion = ++_periodCheckVersion;
        _periodStatus = SponsorshipPeriodStatus.Available;

        if (!CanEvaluatePeriod())
        {
            _checkingPeriod = false;
            return;
        }

        _checkingPeriod = true;
        try
        {
            var status = await SponsorshipService.GetSponsorshipPeriodStatusAsync(
                CreateInputModel());
            if (checkVersion == _periodCheckVersion)
            {
                _periodStatus = status;
            }
        }
        finally
        {
            if (checkVersion == _periodCheckVersion)
            {
                _checkingPeriod = false;
            }
        }
    }

    private bool CanEvaluatePeriod() =>
        _bindModel.SponsorshipStartDate.HasValue
        && (!_isEndDateKnown || _bindModel.SponsorshipEndDate.HasValue)
        && (_bindModel.SponsorshipType == SponsorshipType.Sponsor
            ? _bindModel.SelectedSponsor is not null
            : _bindModel.SelectedCompanyId.HasValue);

    private CreateSponsorshipInputModel CreateInputModel() =>
        new(
            StudentId,
            _bindModel.SelectedSponsor,
            _bindModel.SelectedCompanyId,
            _bindModel.SponsorshipStartDate!.Value,
            _bindModel.SponsorshipEndDate,
            _bindModel.SponsorshipNotes,
            FonbecClaim.UserId);

    private void ShowPeriodConflict()
    {
        _periodStatus = SponsorshipPeriodStatus.OverlapsExisting;
        Snackbar.Add(
            "No se puede asignar porque el período se superpone con un apadrinamiento existente.",
            Severity.Error);
    }

    private async Task Save()
    {
        _saving = true;
        try
        {
            if (!await LoadStudentAsync())
            {
                return;
            }

            var result = await SponsorshipService.CreateSponsorshipAsync(CreateInputModel());
            if (result.PeriodStatus == SponsorshipPeriodStatus.OverlapsExisting)
            {
                ShowPeriodConflict();
                return;
            }

            if (result.CompletedPlanMonthLabels is { Count: > 0 })
            {
                var months = string.Join(", ", result.CompletedPlanMonthLabels);
                Snackbar.Add(
                    $"No se puede agregar este apadrinamiento a {months} porque esa campaña ya fue completada.",
                    Severity.Error);
                return;
            }

            if (!result.AnyAffectedRows)
            {
                Snackbar.Add("No se pudo crear la asignación.", Severity.Error);
                return;
            }

            Snackbar.Add(
                result.PeriodStatus == SponsorshipPeriodStatus.ExtendsExisting
                    ? "El apadrinamiento existente fue extendido."
                    : "El apadrinamiento fue asignado.",
                Severity.Success);
            NavigationManager.NavigateTo(NavRoutes.Sponsorships(StudentId));
        }
        finally
        {
            _saving = false;
        }
    }
}