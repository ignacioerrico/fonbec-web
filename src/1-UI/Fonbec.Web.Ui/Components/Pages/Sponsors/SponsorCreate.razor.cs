using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.Sponsor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Sponsors;
[PageMetadata(nameof(SponsorCreate), "Crear y actualizar Padrino", [FonbecRole.Admin])]

public partial class SponsorCreate : AuthenticationRequiredComponentBase
{
    private readonly SponsorCreateBindModel _bindModel = new();

    private bool _formValidationSucceeded;

    private bool _saving;

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || !_formValidationSucceeded;

    [Inject]
    public ISponsorService SponsorService { get; set; } = null!;

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !SaveButtonDisabled)
        {
            await Save();
        }
    }

    private async Task Save()
    {
        _saving = true;

        var createSponsorInputModel = new CreateSponsorInputModel(
            _bindModel.ChapterId,
            _bindModel.SponsorFirstName,
            _bindModel.SponsorLastName,
            _bindModel.SponsorNickName,
            _bindModel.SponsorGender,
            _bindModel.SponsorPhoneNumber,
            _bindModel.SponsorNotes,
            _bindModel.SponsorEmail,
            _bindModel.SponsorSendAlsoTo,
            _bindModel.SponsorBranchOffice
            );

        var result = await SponsorService.CreateSponsorAsync(createSponsorInputModel);
        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo crear el padrino.", Severity.Error);
        }

        _saving = false;

        NavigationManager.NavigateTo(NavRoutes.Sponsors);
    }
}
