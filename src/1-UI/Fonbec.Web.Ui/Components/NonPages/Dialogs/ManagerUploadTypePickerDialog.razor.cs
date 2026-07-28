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
    private string? _lettersAlreadyUploadedMessage;
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

    protected override async Task OnInitializedAsync()
    {
        // Load eagerly so the "Carta" button and its info message reflect the
        // letter status as soon as the dialog opens (not after the first click).
        await LoadLetterOptionsAsync();
    }

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
        _planIdForRecipients = options.PlanId.Value;

        // Recipients that already have a (non-rejected) letter for this plan cannot be picked.
        var selectable = _recipientOptions.Where(o => !o.AlreadyHasLetterForPlan).ToList();

        if (selectable.Count == 0)
        {
            _letterBlockedMessage = _recipientOptions.Count == 1
                ? "La carta del plan actual ya fue subida para el padrino."
                : "La carta del plan actual ya fue subida para todos los padrinos.";
            return;
        }

        // Some (but not all) recipients already have a letter for this plan: keep the
        // menu usable, disable those entries, and let the manager know which are done.
        var alreadyUploaded = _recipientOptions
            .Where(o => o.AlreadyHasLetterForPlan)
            .Select(o => o.RecipientName)
            .ToList();

        if (alreadyUploaded.Count > 0)
        {
            _lettersAlreadyUploadedMessage = BuildAlreadyUploadedMessage(alreadyUploaded);
        }
    }

    private static string BuildAlreadyUploadedMessage(IReadOnlyList<string> names)
    {
        var joined = JoinWithConjunction(names);
        return names.Count == 1
            ? $"La carta de {joined} ya fue subida para el plan actual."
            : $"Las cartas de {joined} ya fueron subidas para el plan actual.";
    }

    private static string JoinWithConjunction(IReadOnlyList<string> names)
    {
        if (names.Count == 1)
        {
            return names[0];
        }

        var allButLast = string.Join(", ", names.Take(names.Count - 1));
        return $"{allButLast} y {names[^1]}";
    }

    private void SelectSingleLetterRecipient()
    {
        var selectable = _recipientOptions.Where(o => !o.AlreadyHasLetterForPlan).ToList();
        if (selectable.Count == 1)
        {
            SelectLetterRecipient(selectable[0], _planIdForRecipients);
        }
    }

    private void SelectLetterRecipient(ManagerLetterRecipientOptionViewModel option)
    {
        if (option.AlreadyHasLetterForPlan)
        {
            return;
        }

        SelectLetterRecipient(option, _planIdForRecipients);
    }

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
