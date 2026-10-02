using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Sponsors;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.Sponsor;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Sponsors;

[PageMetadata(nameof(SponsorCreate), "Crear y actualizar padrino", [FonbecRole.Admin, FonbecRole.Manager])]
public partial class SponsorCreate : AuthenticationRequiredComponentBase
{
    private readonly SponsorCreateBindModel _bindModel = new();

    private bool _anyChapters;

    private MudForm _form = null!;

    private bool _formValidationSucceeded;

    private bool _saving;

    private bool CanAddRecipient =>
        SponsorRecipientBindModel.CanAddRecipient(_bindModel.Recipients, _bindModel.SponsorEmail);

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || !_formValidationSucceeded;

    [Inject]
    public ISponsorService SponsorService { get; set; } = null!;

    private async Task OnChaptersLoaded(int chaptersCount) =>
        _anyChapters = chaptersCount > 0;

    private void AddRecipient() =>
        _bindModel.Recipients.Add(new SponsorRecipientBindModel());

    private async Task RemoveRecipient(Guid tempId)
    {
        _bindModel.Recipients.RemoveAll(recipient => recipient.TempId == tempId);
        await RevalidateRecipientsAsync();
    }

    private string? ValidateRecipientEmail(SponsorRecipientBindModel recipient, string? email) =>
        SendAlsoToValidator.ValidateEmailField(
            email,
            _bindModel.SponsorEmail,
            _bindModel.Recipients
                .Where(other => other.TempId != recipient.TempId)
                .Select(other => other.RecipientEmail));

    private async Task RevalidateRecipientsAsync()
    {
        if (_form is null)
        {
            return;
        }

        await _form.Validate();
    }

    private async Task Save()
    {
        if (FonbecClaim.ChapterId is null)
        {
            if (_bindModel.ChapterId == 0)
            {
                Snackbar.Add("La filial no es válida.", Severity.Error);
                return;
            }
        }
        else
        {
            _bindModel.ChapterId = FonbecClaim.ChapterId.Value;
        }

        await _form.Validate();
        if (!_form.IsValid)
        {
            return;
        }

        _saving = true;

        var createSponsorInputModel = new CreateSponsorInputModel(
            _bindModel.ChapterId,
            _bindModel.SponsorFirstName,
            _bindModel.SponsorLastName,
            _bindModel.SponsorNickName,
            _bindModel.SponsorGender,
            _bindModel.SponsorEmail,
            _bindModel.SponsorPhoneNumber,
            _bindModel.CompanyId,
            _bindModel.SponsorNotes,
            FonbecClaim.UserId,
            _bindModel.Recipients
                .Select(recipient => new CreateSendAlsoToInputModel(
                    recipient.RecipientName,
                    recipient.RecipientEmail,
                    recipient.SendAsBcc))
                .ToList()
        );

        var result = await SponsorService.CreateSponsorAsync(createSponsorInputModel);
        _saving = false;

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors ?? [])
            {
                Snackbar.Add(error, Severity.Error);
            }

            return;
        }

        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo crear el padrino.", Severity.Error);
        }

        NavigationManager.NavigateTo(NavRoutes.Sponsors);
    }
}