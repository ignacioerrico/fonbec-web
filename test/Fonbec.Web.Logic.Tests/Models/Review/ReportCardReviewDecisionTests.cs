using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Review;

namespace Fonbec.Web.Logic.Tests.Models.Review;

public class ReportCardReviewDecisionTests
{
    [Fact]
    public void CompleteSchoolReview_CanApprove()
    {
        var decision = Evaluate();

        decision.CanApprove.Should().BeTrue();
        decision.SilentRejectReasonId.Should().BeNull();
        decision.BlockingErrors.Should().BeEmpty();
        decision.CompletionPercent.Should().Be(100);

        // TODO [silent-reject-notice]: delete this assertion.
        decision.SilentRejectNotice.Should().BeNull();
    }

    [Fact]
    public void WrongStudent_StaysApprovableButRejectsSilently()
    {
        var decision = Evaluate(
            student: CandidateNameSelection.Wrong,
            selectedStudentName: "Juan Pérez");

        decision.CanApprove.Should().BeTrue();
        decision.SilentRejectReasonId.Should().Be(RejectedReasonIds.WrongStudentName);
        decision.BlockingErrors.Should().BeEmpty();

        // TODO [silent-reject-notice]: delete this assertion.
        decision.SilentRejectNotice.Should().Be("Juan Pérez no es el becario correcto");
    }

    [Fact]
    public void MissingStudent_CompletesReviewAsSilentRejection()
    {
        var decision = Evaluate(student: CandidateNameSelection.Missing);

        decision.CanApprove.Should().BeTrue();
        decision.SilentRejectReasonId.Should().Be(RejectedReasonIds.WrongStudentName);
        decision.BlockingErrors.Should().BeEmpty();

        // TODO [silent-reject-notice]: delete this assertion.
        decision.SilentRejectNotice.Should().BeNull();
    }

    [Fact]
    public void AssessmentMissing_CannotApprove()
    {
        var decision = Evaluate(overallAssessment: null);

        decision.CanApprove.Should().BeFalse();
        decision.BlockingErrors.Should().Contain("Elegí la evaluación general.");
    }

    [Fact]
    public void AbsencesMissing_CanApprove()
    {
        var decision = Evaluate(absences: null);

        decision.CanApprove.Should().BeTrue();
        decision.BlockingErrors.Should().BeEmpty();
    }

    [Fact]
    public void NegativeAbsences_CannotApprove()
    {
        var decision = Evaluate(absences: -1);

        decision.CanApprove.Should().BeFalse();
        decision.BlockingErrors.Should().Contain("Las inasistencias no pueden ser negativas.");
    }

    [Fact]
    public void EmptyReview_ReportsNoProgress()
    {
        var decision = Evaluate(
            student: CandidateNameSelection.None,
            overallAssessment: null,
            absences: null);

        decision.CompletedSteps.Should().Be(0);
        decision.CompletionPercent.Should().Be(0);
        decision.BlockingErrors.Should().Contain("Seleccioná el nombre del becario.");
    }

    [Fact]
    public void PartialReview_ReportsProgress()
    {
        var decision = Evaluate(overallAssessment: null, absences: null);

        decision.CompletedSteps.Should().Be(1);
        decision.TotalSteps.Should().Be(2);
        decision.CompletionPercent.Should().Be(50);
    }

    private static ReportCardReviewDecision Evaluate(
        CandidateNameSelection student = CandidateNameSelection.Correct,
        ReportCardAssessment? overallAssessment = ReportCardAssessment.Green,
        int? absences = 0,
        string? selectedStudentName = null) =>
        ReportCardReviewDecision.Evaluate(student, overallAssessment, absences, selectedStudentName);
}