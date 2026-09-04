using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Models.Review;

/// <summary>Outcome of evaluating the report card review inputs.</summary>
public sealed class ReportCardReviewDecision
{
    public bool CanApprove { get; private init; }

    /// <summary>
    /// When non-null, the reviewer read a name that does not match our records. The review is still
    /// completable — approving submits a rejection with this reason instead. Reviewers are only asked
    /// to report what the document says, so they are not told which name was wrong.
    /// </summary>
    public int? SilentRejectReasonId { get; private init; }

    /// <summary>
    /// Describes the mismatch behind <see cref="SilentRejectReasonId"/> so the behaviour can be
    /// verified while testing.
    /// </summary>
    // TODO [silent-reject-notice]: remove. Reviewers must not learn whether they picked the right
    // name — that is the whole point of the challenge. Grep for "silent-reject-notice" to find every
    // piece to delete: this property, its assignment in Evaluate below, the alert in
    // ReportCardReviewPanel.razor, and the assertion in ReportCardReviewDecisionTests.
    public string? SilentRejectNotice { get; private init; }

    public IReadOnlyList<string> BlockingErrors { get; private init; } = [];

    /// <summary>Number of review inputs the reviewer has already provided.</summary>
    public int CompletedSteps { get; private init; }

    /// <summary>Number of review inputs required to approve.</summary>
    public int TotalSteps { get; private init; }

    /// <summary>Completion of the review, 0-100.</summary>
    public int CompletionPercent =>
        TotalSteps <= 0 ? 100 : (int)Math.Round(CompletedSteps * 100d / TotalSteps, MidpointRounding.AwayFromZero);

    public static ReportCardReviewDecision Evaluate(
        CandidateNameSelection student,
        ReportCardAssessment? overallAssessment,
        int? absences,
        string? selectedStudentName = null)
    {
        var (completedSteps, totalSteps) = CountSteps(student, overallAssessment);

        var errors = new List<string>();

        if (student == CandidateNameSelection.None)
        {
            errors.Add("Seleccioná el nombre del becario.");
        }

        if (overallAssessment is null)
        {
            errors.Add("Elegí la evaluación general.");
        }

        if (absences < 0)
        {
            errors.Add("Las inasistencias no pueden ser negativas.");
        }

        // A wrong pick still completes the review: the reviewer reported what they saw, and the
        // mismatch with our records is resolved by rejecting instead of approving.
        var silentRejectReasonId = student is CandidateNameSelection.Wrong or CandidateNameSelection.Missing
            ? RejectedReasonIds.WrongStudentName
            : (int?)null;

        // TODO [silent-reject-notice]: delete this assignment.
        var silentRejectNotice = silentRejectReasonId is RejectedReasonIds.WrongStudentName
                                 && student == CandidateNameSelection.Wrong
                                 && !string.IsNullOrWhiteSpace(selectedStudentName)
            ? $"{selectedStudentName} no es el becario correcto"
            : (string?)null;

        return new ReportCardReviewDecision
        {
            CanApprove = errors.Count == 0,
            SilentRejectReasonId = silentRejectReasonId,
            SilentRejectNotice = silentRejectNotice, // TODO [silent-reject-notice]: delete this line.
            BlockingErrors = errors,
            CompletedSteps = completedSteps,
            TotalSteps = totalSteps,
        };
    }

    private static (int Completed, int Total) CountSteps(
        CandidateNameSelection student,
        ReportCardAssessment? overallAssessment)
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

        Step(student != CandidateNameSelection.None);
        Step(overallAssessment is not null);

        return (completed, total);
    }
}