using FluentAssertions;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.LetterPlanProgress;

namespace Fonbec.Web.Logic.Tests.Models.LetterPlanProgress;

public class LetterPlanDisplayStatusExtensionsTests
{
    [Fact]
    public void FromRow_Keeps_A_Held_Rejection_In_Progress()
    {
        var status = LetterPlanDisplayStatusExtensions.FromRow(false, DocumentStatus.PendingManagerDecision);

        status.Should().Be(LetterPlanDisplayStatus.PendingManagerDecision);
        status.ToStatusLabel().Should().Be("Pendiente del coordinador");
        status.CountsAsInProgress().Should().BeTrue();
        status.CountsAsApproved().Should().BeFalse();
        status.CountsAsMissingOrRejected().Should().BeFalse();
    }

    [Fact]
    public void IsReadyToComplete_Is_False_While_A_Letter_Waits_For_The_Manager()
    {
        var ready = new[]
        {
            LetterPlanDisplayStatus.Approved,
            LetterPlanDisplayStatus.PendingManagerDecision,
        }.IsReadyToComplete();

        ready.Should().BeFalse();
    }
}