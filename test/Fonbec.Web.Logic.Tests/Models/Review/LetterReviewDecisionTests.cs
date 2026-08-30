using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Review;

namespace Fonbec.Web.Logic.Tests.Models.Review;

public class LetterReviewDecisionTests
{
    private static LetterReviewDecision Evaluate(
        DateTime? writtenDate = null,
        bool omitDefaultWrittenDate = false,
        CandidateNameSelection addressee = CandidateNameSelection.Correct,
        CandidateNameSelection signer = CandidateNameSelection.Correct,
        int spellingScore = 4,
        int penmanshipScore = 4,
        int contentScore = 4,
        bool hasGreenFlags = false,
        string? appraisal = null,
        bool hasRedFlags = false,
        RedFlagPriority? redFlagPriority = null,
        string? issuesNotes = null,
        string? selectedAddresseeName = null,
        string? selectedSignerName = null)
    {
        if (!omitDefaultWrittenDate)
        {
            writtenDate ??= new DateTime(2026, 3, 15);
        }

        return LetterReviewDecision.Evaluate(
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
            issuesNotes,
            selectedAddresseeName,
            selectedSignerName);
    }

    [Fact]
    public void WrongAddressee_StaysApprovableButRejectsSilently()
    {
        var decision = Evaluate(
            addressee: CandidateNameSelection.Wrong,
            selectedAddresseeName: "Juan Pérez");

        decision.CanApprove.Should().BeTrue();
        decision.AutoRejectReasonId.Should().BeNull();
        decision.SilentRejectReasonId.Should().Be(RejectedReasonIds.WrongAddressee);
        decision.ConfirmedAddressee.Should().BeFalse();

        // TODO [silent-reject-notice]: delete this assertion.
        decision.SilentRejectNotice.Should().Be("Juan Pérez no es el destinatario correcto");
    }

    [Fact]
    public void WrongAddresseeWithMissingScores_CannotApproveYet()
    {
        var decision = Evaluate(addressee: CandidateNameSelection.Wrong, spellingScore: 0);

        decision.CanApprove.Should().BeFalse();
        decision.SilentRejectReasonId.Should().Be(RejectedReasonIds.WrongAddressee);
    }

    [Fact]
    public void WrongAddresseeAndWrongSigner_RejectsOnTheAddressee()
    {
        var decision = Evaluate(
            addressee: CandidateNameSelection.Wrong,
            signer: CandidateNameSelection.Wrong);

        decision.SilentRejectReasonId.Should().Be(RejectedReasonIds.WrongAddressee);
    }

    [Fact]
    public void CorrectNames_HaveNoSilentReject()
    {
        var decision = Evaluate();

        decision.SilentRejectReasonId.Should().BeNull();

        // TODO [silent-reject-notice]: delete this assertion.
        decision.SilentRejectNotice.Should().BeNull();
    }

    [Fact]
    public void MissingAddressee_AutoRejects()
    {
        var decision = Evaluate(addressee: CandidateNameSelection.Missing);

        decision.AutoRejectReasonId.Should().Be(RejectedReasonIds.MissingAddressee);
    }

    [Fact]
    public void WrongSigner_StaysApprovableButRejectsSilently()
    {
        var decision = Evaluate(
            signer: CandidateNameSelection.Wrong,
            selectedSignerName: "María López");

        decision.CanApprove.Should().BeTrue();
        decision.AutoRejectReasonId.Should().BeNull();
        decision.SilentRejectReasonId.Should().Be(RejectedReasonIds.WrongSigner);
        decision.ConfirmedSignerMatchesStudent.Should().BeFalse();

        // TODO [silent-reject-notice]: delete this assertion.
        decision.SilentRejectNotice.Should().Be("María López no es el firmante correcto");
    }

    [Fact]
    public void MissingAuthor_AutoRejects()
    {
        var decision = Evaluate(signer: CandidateNameSelection.Missing);

        decision.AutoRejectReasonId.Should().Be(RejectedReasonIds.MissingAuthor);
    }

    [Fact]
    public void ValidConfirmations_CanApprove()
    {
        var decision = Evaluate();

        decision.CanApprove.Should().BeTrue();
        decision.AutoRejectReasonId.Should().BeNull();
        decision.ConfirmedAddressee.Should().BeTrue();
        decision.ConfirmedSignerMatchesStudent.Should().BeTrue();
        decision.ConfirmedWrittenDate.Should().Be(new DateTime(2026, 3, 15));
    }

    [Fact]
    public void GreenFlagWithoutAppraisal_BlocksApprove()
    {
        var decision = Evaluate(hasGreenFlags: true, appraisal: "  ");

        decision.CanApprove.Should().BeFalse();
        decision.AutoRejectReasonId.Should().BeNull();
        decision.BlockingErrors.Should().Contain(e => e.Contains("bandera verde", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RedFlagWithoutPriorityOrNotes_BlocksApprove()
    {
        var decision = Evaluate(hasRedFlags: true, redFlagPriority: null, issuesNotes: null);

        decision.CanApprove.Should().BeFalse();
        decision.BlockingErrors.Should().Contain(e => e.Contains("prioridad", StringComparison.OrdinalIgnoreCase));
        decision.BlockingErrors.Should().Contain(e => e.Contains("descripción", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RedFlagWithPriorityAndNotes_CanApprove()
    {
        var decision = Evaluate(
            hasRedFlags: true,
            redFlagPriority: RedFlagPriority.High,
            issuesNotes: "Contenido preocupante");

        decision.CanApprove.Should().BeTrue();
        decision.BlockingErrors.Should().BeEmpty();
    }

    [Fact]
    public void GreenFlagWithAppraisal_CanApprove()
    {
        var decision = Evaluate(hasGreenFlags: true, appraisal: "Excelente esfuerzo");

        decision.CanApprove.Should().BeTrue();
    }

    [Fact]
    public void IncompleteConfirmations_BlockApproveWithoutAutoReject()
    {
        var decision = Evaluate(addressee: CandidateNameSelection.None);

        decision.CanApprove.Should().BeFalse();
        decision.AutoRejectReasonId.Should().BeNull();
        decision.BlockingErrors.Should().NotBeEmpty();
    }

    [Fact]
    public void MissingWrittenDate_BlocksApproveWithoutAutoReject()
    {
        var decision = Evaluate(omitDefaultWrittenDate: true);

        decision.CanApprove.Should().BeFalse();
        decision.AutoRejectReasonId.Should().BeNull();
        decision.BlockingErrors.Should().Contain(e => e.Contains("fecha escrita", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NothingAnswered_ReportsZeroProgressOverSixSteps()
    {
        var decision = LetterReviewDecision.Evaluate(
            writtenDate: null,
            addressee: CandidateNameSelection.None,
            signer: CandidateNameSelection.None,
            spellingScore: 0,
            penmanshipScore: 0,
            contentScore: 0,
            hasGreenFlags: false,
            appraisal: null,
            hasRedFlags: false,
            redFlagPriority: null,
            issuesNotes: null);

        decision.CompletedSteps.Should().Be(0);
        decision.TotalSteps.Should().Be(6);
        decision.CompletionPercent.Should().Be(0);
    }

    [Fact]
    public void ApprovableReview_ReportsFullProgress()
    {
        var decision = Evaluate();

        decision.CompletedSteps.Should().Be(decision.TotalSteps);
        decision.CompletionPercent.Should().Be(100);
    }

    [Fact]
    public void PartiallyAnswered_ReportsProportionalProgress()
    {
        var decision = Evaluate(spellingScore: 0, penmanshipScore: 0, contentScore: 0);

        decision.CompletedSteps.Should().Be(3);
        decision.TotalSteps.Should().Be(6);
        decision.CompletionPercent.Should().Be(50);
    }

    [Fact]
    public void RaisingFlags_AddsTheirInputsToTheRequiredSteps()
    {
        var decision = Evaluate(hasGreenFlags: true, hasRedFlags: true);

        // Six base inputs, plus the appraisal, plus the red flag priority and description.
        decision.TotalSteps.Should().Be(9);
        decision.CompletedSteps.Should().Be(6);
    }

    [Fact]
    public void AutoRejectedReview_StillReportsProgress()
    {
        var decision = Evaluate(addressee: CandidateNameSelection.Missing);

        decision.AutoRejectReasonId.Should().Be(RejectedReasonIds.MissingAddressee);
        decision.TotalSteps.Should().Be(6);
        decision.CompletedSteps.Should().Be(6);
    }
}
