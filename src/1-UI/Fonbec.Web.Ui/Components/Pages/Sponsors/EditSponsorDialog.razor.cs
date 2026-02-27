using Fonbec.Web.Logic.Models.Sponsors;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Models.Sponsor;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Sponsors;

public partial class EditSponsorDialog : AuthenticationRequiredComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public SponsorsListViewModel Sponsor { get; set; } = null!;

    [Inject]
    private ISponsorService SponsorService { get; set; } = null!;

    private SponsorEditBindModel _model = new();
    private SponsorEditBindModel _original = new();

    private bool _isValid;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        _model = new SponsorEditBindModel(Sponsor);
        _original = new SponsorEditBindModel(Sponsor);
    }

    private bool SaveDisabled => !_isValid || !_model.HasChangesComparedTo(_original);

    private async Task Save()
    {
        // Nothing to do if no changes
        if (!_model.HasChangesComparedTo(_original))
            return;

        var input = new UpdateSponsorInputModel(
            Sponsor.SponsorId,
            _model.SponsorFirstName,
            _model.SponsorLastName,
            _model.SponsorNickName,
            _model.SponsorGender,
            _model.SponsorPhoneNumber,
            _model.SponsorEmail,
            FonbecClaim.UserId
        );

        var result = await SponsorService.UpdateSponsorAsync(input);

        if (result.AnyAffectedRows)
            MudDialog.Close(DialogResult.Ok(true));
        else
            MudDialog.Close(DialogResult.Ok(false));
    }

    private void Cancel() => MudDialog.Cancel();
}