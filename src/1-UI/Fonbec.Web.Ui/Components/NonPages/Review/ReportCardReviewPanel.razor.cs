using System.Globalization;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Models.Review;
using Fonbec.Web.Logic.Options;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Review;

public partial class ReportCardReviewPanel
{
    private static readonly CultureInfo SpanishArgentina = CultureInfo.GetCultureInfo("es-AR");

    /// <summary>
    /// Shown whether the review ends in an approval or in a silent rejection, so the outcome of a
    /// name mismatch is not revealed to the reviewer.
    /// </summary>
    private const string ReviewCompletedMessage = "Revisión completada.";

    private bool _loading = true;
    private bool _saving;
    private bool _showManualRejectReason;
    private CandidateNameSelection _studentSelection;
    private string? _selectedStudentName;
    private ReportCardAssessment? _overallAssessment;
    private int? _absences;
    private CandidateNamesViewModel? _studentChoices;
    private string? _rejectionNotes;
    private List<string> _actionErrors = [];

    [Parameter]
    public long DocumentId { get; set; }

    [Parameter]
    public byte[] RowVersion { get; set; } = null!;

    [Parameter]
    public int StudentId { get; set; }

    [Parameter]
    public DateOnly Period { get; set; }

    [Parameter]
    public EducationLevel EducationLevel { get; set; }

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
    public ILogger<ReportCardReviewPanel> Logger { get; set; } = null!;

    private ReportCardReviewDecision Decision => ReportCardReviewDecision.Evaluate(
        _studentSelection,
        _overallAssessment,
        _absences,
        _selectedStudentName);

    private bool ActionsDisabled => Disabled || _saving;

    private bool ApproveDisabled => ActionsDisabled || _loading || !Decision.CanApprove;

    private bool RejectDisabled
    {
        get
        {
            if (ActionsDisabled || _loading)
            {
                return true;
            }

            // First click reveals the free-text reason; subsequent clicks submit it as Other.
            return _showManualRejectReason && string.IsNullOrWhiteSpace(_rejectionNotes);
        }
    }

    /// <summary>The uploaded period, shown so the reviewer can compare it against the document.</summary>
    private string PeriodLabel =>
        SpanishArgentina.TextInfo.ToTitleCase(Period.ToString("MMMM yyyy", SpanishArgentina));

    protected override async Task OnInitializedAsync()
    {
        var choicesTask = CandidateNamePickerService.GetStudentNameChoicesAsync(
            DocumentId,
            StudentId,
            ReviewOptions.Value.CandidateNameCount);
        _studentChoices = await choicesTask;
        _loading = false;
    }

    private static string ReasonDescription(int reasonId) => reasonId switch
    {
        RejectedReasonIds.NotReportCard => "No es boletín o libreta",
        RejectedReasonIds.WrongStudentName => "Nombre del estudiante incorrecto",
        RejectedReasonIds.Unreadable => "Ilegible",
        RejectedReasonIds.WrongPeriod => "Período incorrecto",
        RejectedReasonIds.Other => "Otro",
        _ => "Motivo desconocido",
    };

    private void ClearActionErrors() => _actionErrors = [];

    private Task OnStudentSelectionChanged(CandidateNamePick pick)
    {
        _studentSelection = pick.Selection;
        _selectedStudentName = pick.DisplayName;
        return Task.CompletedTask;
    }

    private Task ReleaseClickedAsync() => OnRelease.InvokeAsync();

    private Task RejectUnreadableAsync() =>
        ConfirmAndRejectAsync(RejectedReasonIds.Unreadable,
            "¿Confirmás que el documento no se puede leer?");

    private Task RejectNotAReportCardAsync() =>
        ConfirmAndRejectAsync(RejectedReasonIds.NotReportCard,
            "¿Confirmás que el documento no es un boletín ni una libreta?");

    private Task RejectWrongPeriodAsync() =>
        ConfirmAndRejectAsync(RejectedReasonIds.WrongPeriod,
            $"¿Confirmás que el documento no corresponde al período {PeriodLabel}?");

    private Task RejectWrongStudentNameAsync() =>
        ConfirmAndRejectAsync(RejectedReasonIds.WrongStudentName,
            "¿Confirmás que el becario del boletín no es ninguno de los nombres listados?");

    private Task RejectMissingStudentNameAsync() =>
        ConfirmAndRejectAsync(RejectedReasonIds.WrongStudentName,
            "¿Confirmás que el boletín no indica el nombre del becario?");

    private async Task ConfirmAndRejectAsync(int reasonId, string question)
    {
        if (ActionsDisabled)
        {
            return;
        }

        var confirmed = await DialogService.ShowMessageBox(
            "Rechazar el boletín",
            $"{question} Se rechazará con el motivo «{ReasonDescription(reasonId)}».",
            yesText: "Rechazar",
            cancelText: "Cancelar");

        if (confirmed != true)
        {
            return;
        }

        await SubmitRejectAsync(reasonId, null, "Boletín rechazado.", Severity.Info);
    }

    private async Task ApproveAsync()
    {
        var decision = Decision;
        if (!decision.CanApprove || _overallAssessment is null)
        {
            return;
        }

        // The reviewer only reports what the report card says. When that contradicts our records the
        // document is rejected instead, with the same feedback an approval gives.
        if (decision.SilentRejectReasonId is { } silentRejectReasonId)
        {
            await SubmitRejectAsync(silentRejectReasonId, null, ReviewCompletedMessage, Severity.Success);
            return;
        }

        // Reaching an approval means the reviewer never raised the structured rejections, so the
        // document is a report card for the reported period.
        var input = new ApproveReportCardInputModel(
            DocumentId,
            ReviewerId,
            ReviewerRole,
            RowVersion,
            ConfirmedIsReportCardOrTranscript: true,
            ConfirmedPeriodMatches: true,
            ConfirmedStudentNameCorrect: true,
            _overallAssessment.Value,
            EducationLevel == EducationLevel.University ? null : _absences);

        var succeeded = await RunReviewActionAsync(
            () => DocumentService.ApproveReportCardAsync(input),
            "No se pudo aprobar el boletín.");

        if (!succeeded)
        {
            return;
        }

        Snackbar.Add(ReviewCompletedMessage, Severity.Success);
        await OnCompleted.InvokeAsync();
    }

    private async Task RejectAsync()
    {
        if (!_showManualRejectReason)
        {
            _showManualRejectReason = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(_rejectionNotes))
        {
            return;
        }

        await SubmitRejectAsync(
            RejectedReasonIds.Other,
            _rejectionNotes.Trim(),
            "Boletín rechazado.",
            Severity.Info);
    }

    private async Task SubmitRejectAsync(
        int reasonId,
        string? rejectionNotes,
        string successMessage,
        Severity successSeverity)
    {
        var input = new RejectReportCardInputModel(
            DocumentId,
            ReviewerId,
            ReviewerRole,
            RowVersion,
            reasonId,
            rejectionNotes);

        var succeeded = await RunReviewActionAsync(
            () => DocumentService.RejectReportCardAsync(input),
            "No se pudo rechazar el boletín.");

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

            _actionErrors = result.Errors is { Count: > 0 } ? [.. result.Errors] : [fallbackError];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Report card review action failed for document {DocumentId}.", DocumentId);
            _actionErrors = [fallbackError];
        }
        finally
        {
            _saving = false;
        }

        Snackbar.Add(_actionErrors[0], Severity.Error);
        return false;
    }
}