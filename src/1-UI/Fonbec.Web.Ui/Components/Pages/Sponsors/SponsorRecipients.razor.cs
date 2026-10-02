using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Sponsors;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.Sponsor;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Sponsors;

[PageMetadata(nameof(SponsorRecipients), "Gestionar destinatarios adicionales de un padrino", [FonbecRole.Admin, FonbecRole.Manager])]
public partial class SponsorRecipients : AuthenticationRequiredComponentBase
{
    private SponsorRecipientsViewModel? _viewModel;

    private List<SponsorRecipientBindModel> _recipients = [];

    private MudForm? _form;

    private bool _formValidationSucceeded;

    private bool _canEdit;

    private bool _saving;

    private bool _validatedOnce;

    private bool CanAddRecipient =>
        SponsorRecipientBindModel.CanAddRecipient(_recipients, _viewModel?.SponsorEmail);

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || !_canEdit
                                       || !_formValidationSucceeded;

    [Inject]
    public ISponsorService SponsorService { get; set; } = null!;

    [Parameter]
    public int SponsorId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_viewModel is null || _validatedOnce || _form is null)
        {
            return;
        }

        _validatedOnce = true;
        await _form.Validate();
        StateHasChanged();
    }

    private async Task LoadAsync()
    {
        Loading = true;
        _viewModel = await SponsorService.GetSponsorRecipientsAsync(SponsorId, FonbecClaim.ChapterId);
        Loading = false;

        if (_viewModel is null)
        {
            Snackbar.Add(SendAlsoToValidator.NotAvailableMessage, Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.Sponsors);
            return;
        }

        _canEdit = _viewModel.IsSponsorActive;
        _recipients = _viewModel.Recipients
            .Select(recipient => new SponsorRecipientBindModel
            {
                Id = recipient.Id,
                RecipientName = recipient.RecipientName,
                RecipientEmail = recipient.RecipientEmail,
                SendAsBcc = recipient.SendAsBcc,
            })
            .ToList();
        _validatedOnce = false;
    }

    private void AddRecipient() =>
        _recipients.Add(new SponsorRecipientBindModel());

    private async Task RemoveRecipient(Guid tempId)
    {
        _recipients.RemoveAll(recipient => recipient.TempId == tempId);
        await RevalidateAsync();
    }

    private string? ValidateRecipientEmail(SponsorRecipientBindModel recipient, string? email) =>
        SendAlsoToValidator.ValidateEmailField(
            email,
            _viewModel?.SponsorEmail,
            _recipients
                .Where(other => other.TempId != recipient.TempId)
                .Select(other => other.RecipientEmail));

    private async Task RevalidateAsync()
    {
        if (_form is null)
        {
            return;
        }

        await _form.Validate();
    }

    private async Task Save()
    {
        if (!_canEdit || _viewModel is null || _form is null)
        {
            return;
        }

        await _form.Validate();
        if (!_form.IsValid)
        {
            return;
        }

        _saving = true;

        var input = new UpdateSponsorSendAlsoTosInputModel(
            SponsorId,
            FonbecClaim.ChapterId,
            _recipients
                .Select(recipient => new UpdateSendAlsoToInputModel(
                    recipient.Id,
                    recipient.RecipientName,
                    recipient.RecipientEmail,
                    recipient.SendAsBcc))
                .ToList(),
            FonbecClaim.UserId);

        var result = await SponsorService.UpdateSponsorSendAlsoTosAsync(input);
        _saving = false;

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors ?? [])
            {
                Snackbar.Add(error, Severity.Error);
            }

            return;
        }

        if (result.AnyAffectedRows)
        {
            Snackbar.Add("Destinatarios actualizados correctamente.", Severity.Success);
            NavigationManager.NavigateTo(NavRoutes.Sponsors);
        }
        else
        {
            Snackbar.Add("No se realizaron cambios.", Severity.Info);
        }
    }
}