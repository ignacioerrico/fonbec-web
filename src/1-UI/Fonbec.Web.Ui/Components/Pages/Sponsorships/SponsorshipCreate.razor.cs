using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Sponsorships.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.Sponsorship;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Sponsorships;

[PageMetadata(nameof(SponsorshipCreate), "Crear apadrinamiento", [FonbecRole.Manager])]
public partial class SponsorshipCreate : AuthenticationRequiredComponentBase
{
    private readonly SponsorshipCreateBindModel _bindModel = new();
    private bool IsFormDisabled => !_anySponsors;

    private bool _formValidationSucceeded;

    private bool _anySponsors;

    private bool _saving;

    private bool _knowEndDate;
    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || IsFormDisabled
                                       || !_formValidationSucceeded;

    // para que no me deje crear si no se cumple la condicion de la fecha
    private bool DatesAreInvalid =>
    _knowEndDate &&
    _bindModel.SponsorshipEndDate.HasValue &&
    _bindModel.SponsorshipEndDate < _bindModel.SponsorshipStartDate;

    [Parameter]
    public int StudentId { get; set; }

    [Inject]
    public ISponsorshipService SponsorshipService { get; set; } = null!;

    private async Task OnSponsorsLoaded(int sponsorsCount) =>
        _anySponsors = sponsorsCount > 0;

    private async Task Save()
    {
        _saving = true;

        var createSponsorshitInputModel = new CreateSponsorshipInputModel(
            StudentId,
            _bindModel.SponsorId,
            _bindModel.SponsorshipStartDate,
            _bindModel.SponsorshipEndDate,
            FonbecClaim.UserId);

        var result = await SponsorshipService.CreateSponsorshipAsync(createSponsorshitInputModel);

        _saving = false;
        if (result.AnyAffectedRows)
        {
            NavigationManager.NavigateTo(NavRoutes.Students);
        }
        else
        {
            Snackbar.Add("No se pudo crear el apadrinamiento.", Severity.Error);
        }

    }
}
