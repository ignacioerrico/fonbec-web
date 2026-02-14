using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Sponsorship.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.Sponsorship;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Sponsorship;

[PageMetadata(nameof(SponsorshipCreate), "Crear (y actualizar --luego) apadrinamiento", [FonbecRole.Manager])]

public partial class SponsorshipCreate : AuthenticationRequiredComponentBase
{
    private readonly SponsorshipCreateBindModel _bindModel = new();

    private bool IsFormDisabled => !_anyStudents || !_anySponsors;

    private bool _anyStudents;

    private bool _anySponsors;

    private bool _formValidationSucceeded;

    private bool _saving;

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || IsFormDisabled
                                       || !_formValidationSucceeded;
    [Inject]

    public ISponsorshipService SponsorshipService { get; set; } = null!;

    public async Task OnStudentsLoaded(int studentsCount) =>
        _anyStudents = studentsCount > 0;

    public async Task OnSponsorsLoaded(int sponsorsCount) =>
        _anySponsors = sponsorsCount > 0;

    private async Task Save()
    {
        _saving = true;

        var createSponsorshipInputModel = new CreateSponsorshipInputModel(
            _bindModel.StudentId,
            _bindModel.SponsorId,
            _bindModel.SponsorshipStartDate,
            _bindModel.SponsorshipEndDate,
            _bindModel.SponsorshipNotes,
            FonbecClaim.UserId);

        var result = await SponsorshipService.CreateSponsorshipAsync(createSponsorshipInputModel);
        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo crear el apadrinamiento.", Severity.Error);
        }

        _saving = false;

        NavigationManager.NavigateTo(NavRoutes.Sponsorships);
    }
}

