using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Models.Review;
using Fonbec.Web.Logic.Options;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Review;

/// <summary>Quality criteria scored by the reviewer, in the order the keyboard shortcuts walk them.</summary>
public enum ScoreCriterion
{
    Spelling = 0,
    Penmanship = 1,
    Content = 2,
}

public partial class LetterReviewPanel : IAsyncDisposable
{
    /// <summary>
    /// Shown whether the review ends in an approval or in a silent rejection, so the outcome of a
    /// name mismatch is not revealed to the reviewer.
    /// </summary>
    private const string ReviewCompletedMessage = "Revisión completada.";

    private bool _loadingChoices = true;
    private bool _saving;
    private bool _showManualRejectReason;

    private DateTime? _writtenDate;
    private DateTime? _writtenDatePickerMonth;
    private CandidateNameSelection _addresseeSelection;
    private CandidateNameSelection _signerSelection;
    private string? _selectedAddresseeName;
    private string? _selectedSignerName;

    private int _spellingScore;
    private int _penmanshipScore;
    private int _contentScore;
    private ScoreCriterion _activeCriterion = ScoreCriterion.Spelling;

    private bool _hasGreenFlags;
    private string? _appraisal;
    private bool _hasRedFlags;
    private RedFlagPriority? _redFlagPriority;
    private string? _issuesNotes;

    private string? _rejectionNotes;

    private CandidateNamesViewModel? _addresseeChoices;
    private CandidateNamesViewModel? _signerChoices;
    private List<string> _actionErrors = [];

    private DotNetObjectReference<LetterReviewPanel>? _dotNetRef;
    private bool _shortcutsRegistered;

    [Parameter]
    public long DocumentId { get; set; }

    [Parameter]
    public byte[] RowVersion { get; set; } = null!;

    [Parameter]
    public int StudentId { get; set; }

    [Parameter]
    public int? SponsorId { get; set; }

    [Parameter]
    public int? CompanyId { get; set; }

    /// <summary>Start of the planned delivery, used as the month the date picker opens on.</summary>
    [Parameter]
    public DateTime? PlanStartsOn { get; set; }

    [Parameter]
    public int ReviewerId { get; set; }

    [Parameter]
    public string ReviewerRole { get; set; } = string.Empty;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback OnCompleted { get; set; }

    [Parameter]
    public EventCallback OnRelease { get; set; }

    private Task ReleaseClickedAsync() => OnRelease.InvokeAsync();

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

    [Inject]
    public ICandidateNamePickerService CandidateNamePickerService { get; set; } = null!;

    [Inject]
    public IOptions<ReviewOptions> ReviewOptions { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    public IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    public ILogger<LetterReviewPanel> Logger { get; set; } = null!;

    private LetterReviewDecision _decision => BuildDecision();

    private bool ActionsDisabled => Disabled || _saving;

    /// <summary>A letter cannot have been written in the future.</summary>
    private static DateTime Today => DateTime.Today;

    /// <summary>
    /// The month the date picker displays. Clamping on the way in stops the "next month" chevron from
    /// scrolling into months where every day is already disabled: MudBlazor only blocks that
    /// navigation at <see cref="DateTime.MaxValue"/>, not at <c>MaxDate</c>.
    /// </summary>
    private DateTime? WrittenDatePickerMonth
    {
        get => _writtenDatePickerMonth;
        set => _writtenDatePickerMonth = ClampToSelectableMonth(value);
    }

    private static DateTime? ClampToSelectableMonth(DateTime? month)
    {
        if (month is not { } requested)
        {
            return null;
        }

        var currentMonth = new DateTime(Today.Year, Today.Month, 1);

        return new DateTime(requested.Year, requested.Month, 1) > currentMonth ? currentMonth : requested;
    }

    /// <summary>
    /// The picker opens on the planned delivery's month, which is when the letter was written, rather
    /// than on today.
    /// </summary>
    private DateTime? InitialPickerMonth() =>
        PlanStartsOn is { } planStartsOn
            ? ClampToSelectableMonth(new DateTime(planStartsOn.Year, planStartsOn.Month, 1))
            : null;

    private bool ApproveDisabled =>
        Disabled || _saving || _loadingChoices || !_decision.CanApprove;

    private bool RejectDisabled
    {
        get
        {
            if (Disabled || _saving || _loadingChoices)
            {
                return true;
            }

            if (_decision.AutoRejectReasonId.HasValue)
            {
                return false;
            }

            // First click reveals the free-text reason; subsequent clicks submit it as Other.
            if (!_showManualRejectReason)
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(_rejectionNotes);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        _writtenDatePickerMonth = InitialPickerMonth();

        var count = ReviewOptions.Value.CandidateNameCount;

        if (!SponsorId.HasValue && !CompanyId.HasValue)
        {
            Snackbar.Add("La carta no tiene destinatario configurado.", Severity.Error);
            _loadingChoices = false;
            return;
        }

        var signerTask = CandidateNamePickerService.GetStudentNameChoicesAsync(DocumentId, StudentId, count);
        var addresseeTask = CandidateNamePickerService.GetAddresseeNameChoicesAsync(DocumentId, SponsorId, CompanyId, count);

        await Task.WhenAll(signerTask, addresseeTask);

        _signerChoices = await signerTask;
        _addresseeChoices = await addresseeTask;
        _loadingChoices = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _shortcutsRegistered)
        {
            return;
        }

        try
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            await JsRuntime.InvokeVoidAsync("fonbecReviewShortcuts.register", _dotNetRef);
            _shortcutsRegistered = true;
        }
        catch (JSException)
        {
            // Script not loaded; the panel stays fully usable with the mouse.
        }
        catch (InvalidOperationException)
        {
            // JS interop unavailable (e.g. prerender).
        }
    }

    [JSInvokable]
    public async Task OnShortcutAsync(string shortcut)
    {
        if (ActionsDisabled || _loadingChoices)
        {
            return;
        }

        switch (shortcut)
        {
            case "ArrowUp":
                MoveActiveCriterion(-1);
                break;

            case "ArrowDown":
                MoveActiveCriterion(1);
                break;

            case "Ctrl+Enter":
                if (!ApproveDisabled)
                {
                    await InvokeAsync(ApproveAsync);
                }

                return;

            default:
                if (int.TryParse(shortcut, out var score) && score is >= 1 and <= 5)
                {
                    SetScore(_activeCriterion, score);
                    AdvanceToNextUnscoredCriterion();
                }

                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    private static string ReasonDescription(int reasonId) => reasonId switch
    {
        RejectedReasonIds.MissingWrittenDate => "No figura la fecha",
        RejectedReasonIds.MissingAddressee => "No figura el destinatario",
        RejectedReasonIds.MissingAuthor => "No figura el firmante",
        RejectedReasonIds.NotALetter => "No es una carta",
        RejectedReasonIds.WrongAddressee => "Destinatario incorrecto",
        RejectedReasonIds.WrongSigner => "Firmante incorrecto",
        RejectedReasonIds.Unreadable => "Ilegible",
        RejectedReasonIds.Other => "Otro",
        _ => "Motivo desconocido",
    };

    private void ClearActionErrors() => _actionErrors = [];

    private void SetActiveCriterion(ScoreCriterion criterion) => _activeCriterion = criterion;

    private void SetScore(ScoreCriterion criterion, int value)
    {
        switch (criterion)
        {
            case ScoreCriterion.Spelling:
                _spellingScore = value;
                break;
            case ScoreCriterion.Penmanship:
                _penmanshipScore = value;
                break;
            case ScoreCriterion.Content:
                _contentScore = value;
                break;
        }
    }

    private int ScoreOf(ScoreCriterion criterion) => criterion switch
    {
        ScoreCriterion.Spelling => _spellingScore,
        ScoreCriterion.Penmanship => _penmanshipScore,
        ScoreCriterion.Content => _contentScore,
        _ => 0,
    };

    private void MoveActiveCriterion(int delta)
    {
        var criteria = Enum.GetValues<ScoreCriterion>();
        var next = ((int)_activeCriterion + delta + criteria.Length) % criteria.Length;
        _activeCriterion = criteria[next];
    }

    private void AdvanceToNextUnscoredCriterion()
    {
        var criteria = Enum.GetValues<ScoreCriterion>();

        for (var offset = 1; offset <= criteria.Length; offset++)
        {
            var candidate = criteria[((int)_activeCriterion + offset) % criteria.Length];
            if (ScoreOf(candidate) == 0)
            {
                _activeCriterion = candidate;
                return;
            }
        }

        // Everything is scored: keep the highlight so the next keypress corrects the same criterion.
    }

    private Task OnAddresseeSelectionChanged(CandidateNamePick pick)
    {
        _addresseeSelection = pick.Selection;
        _selectedAddresseeName = pick.DisplayName;
        return Task.CompletedTask;
    }

    private Task OnSignerSelectionChanged(CandidateNamePick pick)
    {
        _signerSelection = pick.Selection;
        _selectedSignerName = pick.DisplayName;
        return Task.CompletedTask;
    }

    private Task RejectNotALetterAsync() =>
        ConfirmAndRejectAsync(RejectedReasonIds.NotALetter, "¿Confirmás que el documento no es una carta?");

    private Task RejectUnreadableAsync() =>
        ConfirmAndRejectAsync(RejectedReasonIds.Unreadable, "¿Confirmás que el documento es ilegible?");

    private Task RejectMissingWrittenDateAsync() =>
        ConfirmAndRejectAsync(RejectedReasonIds.MissingWrittenDate, "¿Confirmás que la carta no tiene fecha escrita?");

    private Task RejectWrongAddresseeAsync() =>
        ConfirmAndRejectAsync(RejectedReasonIds.WrongAddressee,
            "¿Confirmás que el destinatario de la carta no es ninguno de los nombres listados?");

    private Task RejectMissingAddresseeAsync() =>
        ConfirmAndRejectAsync(RejectedReasonIds.MissingAddressee, "¿Confirmás que la carta no indica destinatario?");

    private Task RejectWrongSignerAsync() =>
        ConfirmAndRejectAsync(RejectedReasonIds.WrongSigner,
            "¿Confirmás que el firmante de la carta no es ninguno de los nombres listados?");

    private Task RejectMissingAuthorAsync() =>
        ConfirmAndRejectAsync(RejectedReasonIds.MissingAuthor, "¿Confirmás que la carta no indica quién la firma?");

    private async Task ConfirmAndRejectAsync(int reasonId, string question)
    {
        if (ActionsDisabled)
        {
            return;
        }

        var confirmed = await DialogService.ShowMessageBox(
            "Rechazar la carta",
            $"{question} Se rechazará con el motivo «{ReasonDescription(reasonId)}».",
            yesText: "Rechazar",
            cancelText: "Cancelar");

        if (confirmed != true)
        {
            return;
        }

        await SubmitRejectAsync(reasonId, null);
    }

    private LetterReviewDecision BuildDecision() =>
        LetterReviewDecision.Evaluate(
            _writtenDate,
            _addresseeSelection,
            _signerSelection,
            _spellingScore,
            _penmanshipScore,
            _contentScore,
            _hasGreenFlags,
            _appraisal,
            _hasRedFlags,
            _redFlagPriority,
            _issuesNotes,
            _selectedAddresseeName,
            _selectedSignerName);

    private async Task ApproveAsync()
    {
        var decision = BuildDecision();
        if (!decision.CanApprove || decision.ConfirmedWrittenDate is null)
        {
            return;
        }

        // The reviewer only reports what the letter says. When that contradicts our records the
        // document is rejected instead, with the same feedback an approval gives.
        if (decision.SilentRejectReasonId is { } silentRejectReasonId)
        {
            await SubmitRejectAsync(silentRejectReasonId, null, ReviewCompletedMessage, Severity.Success);
            return;
        }

        var input = new ApproveLetterInputModel(
            DocumentId,
            ReviewerId,
            ReviewerRole,
            RowVersion,
            ConfirmedIsLetter: true,
            decision.ConfirmedWrittenDate.Value,
            decision.ConfirmedAddressee,
            decision.ConfirmedSignerMatchesStudent,
            _spellingScore,
            _penmanshipScore,
            _contentScore,
            _hasRedFlags,
            _hasGreenFlags,
            _hasRedFlags ? _issuesNotes : null,
            _hasGreenFlags ? _appraisal : null,
            _hasRedFlags ? _redFlagPriority : null);

        var succeeded = await RunReviewActionAsync(
            () => DocumentService.ApproveLetterAsync(input),
            "No se pudo aprobar la carta.");

        if (!succeeded)
        {
            return;
        }

        Snackbar.Add(ReviewCompletedMessage, Severity.Success);
        await OnCompleted.InvokeAsync();
    }

    private async Task RejectAsync()
    {
        if (_decision.AutoRejectReasonId is null && !_showManualRejectReason)
        {
            _showManualRejectReason = true;
            return;
        }

        if (_decision.AutoRejectReasonId is { } autoRejectReasonId)
        {
            await SubmitRejectAsync(autoRejectReasonId, null);
            return;
        }

        if (string.IsNullOrWhiteSpace(_rejectionNotes))
        {
            return;
        }

        await SubmitRejectAsync(RejectedReasonIds.Other, _rejectionNotes.Trim());
    }

    private async Task SubmitRejectAsync(
        int reasonId,
        string? rejectionNotes,
        string successMessage = "Carta rechazada.",
        Severity successSeverity = Severity.Info)
    {
        var input = new RejectLetterInputModel(
            DocumentId,
            ReviewerId,
            ReviewerRole,
            RowVersion,
            reasonId,
            rejectionNotes);

        var succeeded = await RunReviewActionAsync(
            () => DocumentService.RejectLetterAsync(input),
            "No se pudo rechazar la carta.");

        if (!succeeded)
        {
            return;
        }

        Snackbar.Add(successMessage, successSeverity);
        await OnCompleted.InvokeAsync();
    }

    /// <summary>
    /// Runs an approve/reject call, surfacing any failure (including a concurrency conflict) inline
    /// without clearing what the reviewer entered, so the action can be retried.
    /// </summary>
    private async Task<bool> RunReviewActionAsync(Func<Task<ReviewResult>> action, string fallbackError)
    {
        _actionErrors = [];
        _saving = true;

        try
        {
            var result = await action();
            if (result.IsSuccess)
            {
                return true;
            }

            _actionErrors = result.Errors is { Count: > 0 }
                ? [.. result.Errors]
                : [fallbackError];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Review action failed for document {DocumentId}.", DocumentId);
            _actionErrors = [fallbackError];
        }
        finally
        {
            _saving = false;
        }

        Snackbar.Add(_actionErrors[0], Severity.Error);
        return false;
    }

    public async ValueTask DisposeAsync()
    {
        if (_shortcutsRegistered)
        {
            try
            {
                await JsRuntime.InvokeVoidAsync("fonbecReviewShortcuts.unregister");
            }
            catch (JSDisconnectedException)
            {
                // The circuit is already gone; nothing to clean up in the browser.
            }
            catch (JSException)
            {
                // Ignore: the page is going away.
            }
            catch (InvalidOperationException)
            {
                // JS interop unavailable.
            }
        }

        _dotNetRef?.Dispose();
        GC.SuppressFinalize(this);
    }
}