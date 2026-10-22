using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Models.Review;

/// <summary>How the reviewer answered a name multiple-choice confirmation.</summary>
public enum CandidateNameSelection
{
    /// <summary>No option selected yet.</summary>
    None = 0,

    /// <summary>The correct candidate was chosen.</summary>
    Correct = 1,

    /// <summary>A distractor name was chosen.</summary>
    Wrong = 2,

    /// <summary>The explicit "No figura" (missing) option was chosen.</summary>
    Missing = 3,
}

/// <summary>Outcome of evaluating letter-review confirmations and flag rules.</summary>
public sealed class LetterReviewDecision
{
    public bool CanApprove { get; private init; }

    /// <summary>
    /// When non-null, confirmations failed and the UI should force reject with this reason
    /// (reason picker disabled). Null when confirmations pass (approve or manual reject).
    /// </summary>
    public int? AutoRejectReasonId { get; private init; }

    /// <summary>
    /// When non-null, the reviewer read a name that does not match our records. The review is still
    /// completable — approving submits a rejection with this reason instead. Reviewers are only asked
    /// to report what the document says, so they are not told which name was wrong.
    /// </summary>
    public int? SilentRejectReasonId { get; private init; }

    /// <summary>
    /// Shown under the sponsor list when the selected name is not the addressee.
    /// The review page renders it only when <c>Review:ShowNameMismatchHints</c> is enabled.
    /// </summary>
    public string? WrongAddresseeNotice { get; private init; }

    /// <summary>
    /// Shown under the student list when the selected name is not the signer.
    /// The review page renders it only when <c>Review:ShowNameMismatchHints</c> is enabled.
    /// Independent of <see cref="WrongAddresseeNotice"/>: both can be set at once.
    /// </summary>
    public string? WrongSignerNotice { get; private init; }

    public bool ConfirmedAddressee { get; private init; }
    public bool ConfirmedSignerMatchesStudent { get; private init; }
    public DateTime? ConfirmedWrittenDate { get; private init; }

    public IReadOnlyList<string> BlockingErrors { get; private init; } = [];

    /// <summary>Number of review inputs the reviewer has already provided.</summary>
    public int CompletedSteps { get; private init; }

    /// <summary>
    /// Number of review inputs required to approve. Grows when the reviewer raises a flag, since
    /// each flag adds its own required inputs.
    /// </summary>
    public int TotalSteps { get; private init; }

    /// <summary>Completion of the review, 0-100.</summary>
    public int CompletionPercent =>
        TotalSteps <= 0 ? 100 : (int)Math.Round(CompletedSteps * 100d / TotalSteps, MidpointRounding.AwayFromZero);

    public static LetterReviewDecision Evaluate(
        DateTime? writtenDate,
        CandidateNameSelection addressee,
        CandidateNameSelection signer,
        int spellingScore,
        int penmanshipScore,
        int contentScore,
        bool hasGreenFlags,
        string? appraisal,
        bool hasRedFlags,
        RedFlagPriority? redFlagPriority,
        string? issuesNotes,
        string? selectedAddresseeName = null,
        string? selectedSignerName = null)
    {
        var (completedSteps, totalSteps) = CountSteps(
            writtenDate,
            addressee,
            signer,
            spellingScore,
            penmanshipScore,
            contentScore,
            hasGreenFlags,
            appraisal,
            hasRedFlags,
            redFlagPriority,
            issuesNotes);

        if (addressee == CandidateNameSelection.Missing)
        {
            return AutoReject(RejectedReasonIds.MissingAddressee, completedSteps, totalSteps);
        }

        if (signer == CandidateNameSelection.Missing)
        {
            return AutoReject(RejectedReasonIds.MissingAuthor, completedSteps, totalSteps);
        }

        var errors = new List<string>();

        if (!writtenDate.HasValue)
        {
            errors.Add("Indicá la fecha escrita en la carta.");
        }

        if (addressee == CandidateNameSelection.None)
        {
            errors.Add("Seleccioná el destinatario (nombre del padrino).");
        }

        if (signer == CandidateNameSelection.None)
        {
            errors.Add("Seleccioná el firmante (nombre del becario).");
        }

        if (spellingScore < 1)
        {
            errors.Add("Puntuá la ortografía.");
        }

        if (penmanshipScore < 1)
        {
            errors.Add("Puntuá la legibilidad.");
        }

        if (contentScore < 1)
        {
            errors.Add("Puntuá el contenido.");
        }

        if (hasGreenFlags && string.IsNullOrWhiteSpace(appraisal))
        {
            errors.Add("La bandera verde requiere un comentario.");
        }

        if (hasRedFlags)
        {
            if (redFlagPriority is null)
            {
                errors.Add("La bandera roja requiere una prioridad.");
            }

            if (string.IsNullOrWhiteSpace(issuesNotes))
            {
                errors.Add("La bandera roja requiere una descripción.");
            }
        }

        // A wrong pick still completes the review: the reviewer reported what they saw, and the
        // mismatch with our records is resolved by rejecting instead of approving.
        var silentRejectReasonId = addressee == CandidateNameSelection.Wrong
            ? RejectedReasonIds.WrongAddressee
            : signer == CandidateNameSelection.Wrong
                ? RejectedReasonIds.WrongSigner
                : (int?)null;

        // Each list gets its own hint. A wrong sponsor and a wrong student are reported separately,
        // even though the rejection itself keeps a single reason (the addressee, when both are wrong).
        var wrongAddresseeNotice = addressee == CandidateNameSelection.Wrong
                                   && !string.IsNullOrWhiteSpace(selectedAddresseeName)
            ? $"{selectedAddresseeName} no es el destinatario correcto"
            : null;

        var wrongSignerNotice = signer == CandidateNameSelection.Wrong
                                && !string.IsNullOrWhiteSpace(selectedSignerName)
            ? $"{selectedSignerName} no es el firmante correcto"
            : null;

        var canApprove = errors.Count == 0;

        return new LetterReviewDecision
        {
            CanApprove = canApprove,
            AutoRejectReasonId = null,
            SilentRejectReasonId = silentRejectReasonId,
            WrongAddresseeNotice = wrongAddresseeNotice,
            WrongSignerNotice = wrongSignerNotice,
            ConfirmedAddressee = addressee == CandidateNameSelection.Correct,
            ConfirmedSignerMatchesStudent = signer == CandidateNameSelection.Correct,
            ConfirmedWrittenDate = writtenDate,
            BlockingErrors = errors,
            CompletedSteps = completedSteps,
            TotalSteps = totalSteps,
        };
    }

    private static (int Completed, int Total) CountSteps(
        DateTime? writtenDate,
        CandidateNameSelection addressee,
        CandidateNameSelection signer,
        int spellingScore,
        int penmanshipScore,
        int contentScore,
        bool hasGreenFlags,
        string? appraisal,
        bool hasRedFlags,
        RedFlagPriority? redFlagPriority,
        string? issuesNotes)
    {
        var total = 0;
        var completed = 0;

        void Step(bool done)
        {
            total++;
            if (done)
            {
                completed++;
            }
        }

        Step(writtenDate.HasValue);
        Step(addressee != CandidateNameSelection.None);
        Step(signer != CandidateNameSelection.None);
        Step(spellingScore >= 1);
        Step(penmanshipScore >= 1);
        Step(contentScore >= 1);

        if (hasGreenFlags)
        {
            Step(!string.IsNullOrWhiteSpace(appraisal));
        }

        if (hasRedFlags)
        {
            Step(redFlagPriority is not null);
            Step(!string.IsNullOrWhiteSpace(issuesNotes));
        }

        return (completed, total);
    }

    private static LetterReviewDecision AutoReject(int reasonId, int completedSteps, int totalSteps) =>
        new()
        {
            CanApprove = false,
            AutoRejectReasonId = reasonId,
            BlockingErrors = [],
            CompletedSteps = completedSteps,
            TotalSteps = totalSteps,
        };
}