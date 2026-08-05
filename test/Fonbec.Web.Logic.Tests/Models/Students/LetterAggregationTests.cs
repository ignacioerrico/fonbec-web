using FluentAssertions;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Students;

namespace Fonbec.Web.Logic.Tests.Models.Students;

public class LetterAggregationTests
{
    [Fact]
    public void Aggregate_Returns_NoPlan_When_No_Active_Plan()
    {
        var result = LetterAggregation.Aggregate(
            hasActivePlan: false, isExempt: false, slotStatuses: [LetterSlotStatus.Rejected]);

        result.Should().Be(LetterAggregateStatus.NoPlan);
    }

    [Fact]
    public void Aggregate_Returns_NoPlan_Even_When_Exempt()
    {
        // No active plan is checked first: there is nothing to be exempt from.
        var result = LetterAggregation.Aggregate(
            hasActivePlan: false, isExempt: true, slotStatuses: []);

        result.Should().Be(LetterAggregateStatus.NoPlan);
    }

    [Fact]
    public void Aggregate_Returns_Exempt_When_Student_Is_Exempt()
    {
        // Exempt short-circuits regardless of what the (irrelevant) slot statuses look like.
        var result = LetterAggregation.Aggregate(
            hasActivePlan: true, isExempt: true, slotStatuses: [LetterSlotStatus.Rejected, LetterSlotStatus.None]);

        result.Should().Be(LetterAggregateStatus.Exempt);
    }

    [Fact]
    public void Aggregate_Returns_Rejected_When_Any_Slot_Is_Rejected()
    {
        var result = LetterAggregation.Aggregate(
            hasActivePlan: true,
            isExempt: false,
            slotStatuses: [LetterSlotStatus.Approved, LetterSlotStatus.Rejected, LetterSlotStatus.Approved]);

        result.Should().Be(LetterAggregateStatus.Rejected);
    }

    [Fact]
    public void Aggregate_Rejected_Wins_Over_Pending()
    {
        var result = LetterAggregation.Aggregate(
            hasActivePlan: true,
            isExempt: false,
            slotStatuses: [LetterSlotStatus.Rejected, LetterSlotStatus.None, LetterSlotStatus.InReview]);

        result.Should().Be(LetterAggregateStatus.Rejected);
    }

    [Theory]
    [InlineData(LetterSlotStatus.None)]
    [InlineData(LetterSlotStatus.InReview)]
    public void Aggregate_Returns_Pending_When_Any_Slot_Is_Not_Approved(LetterSlotStatus incompleteStatus)
    {
        var result = LetterAggregation.Aggregate(
            hasActivePlan: true,
            isExempt: false,
            slotStatuses: [LetterSlotStatus.Approved, incompleteStatus]);

        result.Should().Be(LetterAggregateStatus.Pending);
    }

    [Fact]
    public void Aggregate_Returns_Approved_When_All_Slots_Approved()
    {
        var result = LetterAggregation.Aggregate(
            hasActivePlan: true,
            isExempt: false,
            slotStatuses: [LetterSlotStatus.Approved, LetterSlotStatus.Approved]);

        result.Should().Be(LetterAggregateStatus.Approved);
    }

    [Theory]
    [InlineData(LetterAggregateStatus.Rejected, true, true)]
    [InlineData(LetterAggregateStatus.Pending, true, true)]
    [InlineData(LetterAggregateStatus.Approved, true, false)]
    [InlineData(LetterAggregateStatus.Exempt, true, false)]
    [InlineData(LetterAggregateStatus.NoPlan, true, false)]
    [InlineData(LetterAggregateStatus.Rejected, false, true)]
    [InlineData(LetterAggregateStatus.Approved, false, true)]
    public void MatchesLetterFilter_Includes_Only_Rejected_Or_Pending_When_Active(
        LetterAggregateStatus aggregate, bool filterActive, bool expectedMatch)
    {
        var result = LetterAggregation.MatchesLetterFilter(aggregate, filterActive);

        result.Should().Be(expectedMatch);
    }

    [Fact]
    public void ToSlotStatus_Returns_None_When_No_Letter()
    {
        var result = LetterAggregation.ToSlotStatus(null);

        result.Should().Be(LetterSlotStatus.None);
    }

    [Theory]
    [InlineData(DocumentStatus.Pending)]
    [InlineData(DocumentStatus.PendingImprovement)]
    [InlineData(DocumentStatus.ProcessingImprovement)]
    [InlineData(DocumentStatus.Processing)]
    public void ToSlotStatus_Returns_InReview_For_Non_Terminal_Statuses(DocumentStatus status)
    {
        var result = LetterAggregation.ToSlotStatus(status);

        result.Should().Be(LetterSlotStatus.InReview);
    }

    [Fact]
    public void ToSlotStatus_Returns_Approved_For_Approved_Letter()
    {
        var result = LetterAggregation.ToSlotStatus(DocumentStatus.Approved);

        result.Should().Be(LetterSlotStatus.Approved);
    }

    [Fact]
    public void ToSlotStatus_Returns_Rejected_For_Rejected_Letter()
    {
        var result = LetterAggregation.ToSlotStatus(DocumentStatus.Rejected);

        result.Should().Be(LetterSlotStatus.Rejected);
    }
}
