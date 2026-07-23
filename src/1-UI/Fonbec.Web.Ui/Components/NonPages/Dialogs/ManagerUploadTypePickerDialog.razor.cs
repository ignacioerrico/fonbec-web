using Fonbec.Web.Logic.Models.Managers;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Dialogs;

public partial class ManagerUploadTypePickerDialog
{
    private bool _loadingLetterOptions;
    private bool _letterOptionsLoaded;
    private string? _letterBlockedMessage;
    private List<ManagerLetterRecipientOptionViewModel> _recipientOptions = [];
    private int _planIdForRecipients;

    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public int StudentId { get; set; }

    [Parameter]
    public string StudentFullName { get; set; } = string.Empty;

    [Parameter]
    public int ManagerChapterId { get; set; }

    [Inject]
    public IManagerUploadService ManagerUploadService { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    private async Task LoadLetterOptionsAsync()
    {
        _loadingLetterOptions = true;
        StateHasChanged();

        var options = await ManagerUploadService.GetLetterRecipientOptionsAsync(ManagerChapterId, StudentId);

        _loadingLetterOptions = false;
        _letterOptionsLoaded = true;

        if (options is null)
        {
            _letterBlockedMessage = "No se puede subir una carta para este becario.";
            return;
        }

        if (options.PlanId is null)
        {
            _letterBlockedMessage = "No hay un plan activo.";
            return;
        }

        if (options.IsExempt)
        {
            _letterBlockedMessage = "El becario está eximido de entregar carta para este plan.";
            return;
        }

        if (options.Options.Count == 0)
        {
            _letterBlockedMessage = "El becario no tiene apadrinamientos activos.";
            return;
        }

        _recipientOptions = options.Options;

        if (_recipientOptions.Count == 1)
        {
            SelectLetterRecipient(_recipientOptions[0], options.PlanId.Value);
        }
        else
        {
            _planIdForRecipients = options.PlanId.Value;
        }
    }

    private void SelectLetterRecipient(ManagerLetterRecipientOptionViewModel option) =>
        SelectLetterRecipient(option, _planIdForRecipients);

    private void SelectLetterRecipient(ManagerLetterRecipientOptionViewModel option, int planId)
    {
        var url = NavRoutes.ManagerUploadLetter(StudentId, planId, option.SponsorId, option.CompanyId);
        NavigationManager.NavigateTo(url);
        MudDialog.Close();
    }

    private void SelectReportCard()
    {
        NavigationManager.NavigateTo(NavRoutes.ManagerUploadReportCard(StudentId));
        MudDialog.Close();
    }

    private void SelectOther()
    {
        NavigationManager.NavigateTo(NavRoutes.ManagerUploadOther(StudentId));
        MudDialog.Close();
    }

    private void Cancel() => MudDialog.Cancel();
}
