using Fonbec.Web.DataAccess.Constants;
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
    private bool _formValidationSucceeded;
    private bool _isEndDateKnown;
    private bool SaveButtonDisabled => _saving
                                       || !_anySponsors
                                       || !_formValidationSucceeded
                                       || !DateSelectionIsValid;

    private bool DateSelectionIsValid =>
        _bindModel.SponsorshipStartDate is DateTime start
        && (!_isEndDateKnown
            || (_bindModel.SponsorshipEndDate is DateTime end && start < end));

    [Parameter]
    public int StudentId { get; set; }

    [Inject]
    public ISponsorshipService SponsorshipService { get; set; } = null!;

    private async Task OnSponsorsLoaded(int sponsorsCount) =>
        _anySponsors = sponsorsCount > 0;

    private void OnIsEndDateKnownCheckBoxChanged(bool isEndDateKnown)
    {
        _isEndDateKnown = isEndDateKnown;

        // This guarantees that the end date is null if it is not known
        if (!isEndDateKnown)
        {
            _bindModel.SponsorshipEndDate = null;
        }
    }

    private void OnEndDateChanged(DateTime? endDate) =>
        _bindModel.SponsorshipEndDate = endDate is DateTime d
            ? new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month))
            : null;

    private async Task Save()
    {
        var createSponsorshipInputModel = new CreateSponsorshipInputModel(
            StudentId,
            _bindModel.SelectedSponsorId,
            _bindModel.SponsorshipStartDate!.Value,
            _bindModel.SponsorshipEndDate,
            _bindModel.SponsorshipNotes,
            FonbecClaim.UserId);

        _saving = true;

        var result = await SponsorshipService.CreateSponsorshipAsync(createSponsorshipInputModel);

        _saving = false;

        if (result.AnyAffectedRows)
        {
            NavigationManager.NavigateTo(NavRoutes.Students);
        }
        else
        {
            Snackbar.Add("No se pudo crear la asignación.", Severity.Error);
        }
    }
}