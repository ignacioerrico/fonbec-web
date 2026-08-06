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
            hasActivePlan: false, isExempt: false, slotStatuses: [LetterSlotStatus.None]);

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
    public void Aggregate_Returns_NotUploaded_When_No_Letters()
    {
        var result = LetterAggregation.Aggregate(
            hasActivePlan: true,
            isExempt: false,
            slotStatuses: [LetterSlotStatus.None, LetterSlotStatus.None]);

        result.Should().Be(LetterAggregateStatus.NotUploaded);
    }

    [Fact]
    public void Aggregate_Returns_NotUploaded_When_Every_Letter_Was_Rejected()
    {
        // Rejected letters still need a (re)upload, so nothing counts as satisfied.
        var result = LetterAggregation.Aggregate(
            hasActivePlan: true,
            isExempt: false,
            slotStatuses: [LetterSlotStatus.Rejected, LetterSlotStatus.None]);

        result.Should().Be(LetterAggregateStatus.NotUploaded);
    }

    [Theory]
    [InlineData(LetterSlotStatus.None)]
    [InlineData(LetterSlotStatus.Rejected)]
    public void Aggregate_Returns_Partial_When_Some_Uploaded_And_Some_Missing(LetterSlotStatus needsUploadStatus)
    {
        var result = LetterAggregation.Aggregate(
            hasActivePlan: true,
            isExempt: false,
            slotStatuses: [LetterSlotStatus.Approved, needsUploadStatus]);

        result.Should().Be(LetterAggregateStatus.Partial);
    }

    [Theory]
    [InlineData(LetterSlotStatus.Approved, LetterSlotStatus.Approved)]
    [InlineData(LetterSlotStatus.InReview, LetterSlotStatus.InReview)]
    [InlineData(LetterSlotStatus.Approved, LetterSlotStatus.InReview)]
    public void Aggregate_Returns_Complete_When_Every_Sponsor_Has_A_Letter(LetterSlotStatus first, LetterSlotStatus second)
    {
        var result = LetterAggregation.Aggregate(
            hasActivePlan: true, isExempt: false, slotStatuses: [first, second]);

        result.Should().Be(LetterAggregateStatus.Complete);
    }

    [Fact]
    public void Aggregate_Returns_Complete_When_There_Are_No_Sponsors()
    {
        var result = LetterAggregation.Aggregate(
            hasActivePlan: true, isExempt: false, slotStatuses: []);

        result.Should().Be(LetterAggregateStatus.Complete);
    }

    [Theory]
    [InlineData(LetterSlotStatus.InReview, true)]
    [InlineData(LetterSlotStatus.Approved, true)]
    [InlineData(LetterSlotStatus.None, false)]
    [InlineData(LetterSlotStatus.Rejected, false)]
    public void IsSatisfied_Is_True_Only_For_InReview_Or_Approved(LetterSlotStatus status, bool expected)
    {
        LetterAggregation.IsSatisfied(status).Should().Be(expected);
    }

    [Theory]
    [InlineData(LetterSlotStatus.None, true)]
    [InlineData(LetterSlotStatus.Rejected, true)]
    [InlineData(LetterSlotStatus.InReview, false)]
    [InlineData(LetterSlotStatus.Approved, false)]
    public void NeedsUpload_Is_True_Only_For_Missing_Or_Rejected(LetterSlotStatus status, bool expected)
    {
        LetterAggregation.NeedsUpload(status).Should().Be(expected);
    }

    [Theory]
    [InlineData(LetterAggregateStatus.NotUploaded, true, true)]
    [InlineData(LetterAggregateStatus.Partial, true, true)]
    [InlineData(LetterAggregateStatus.Complete, true, false)]
    [InlineData(LetterAggregateStatus.Exempt, true, false)]
    [InlineData(LetterAggregateStatus.NoPlan, true, false)]
    [InlineData(LetterAggregateStatus.NotUploaded, false, true)]
    [InlineData(LetterAggregateStatus.Complete, false, true)]
    public void MatchesLetterFilter_Includes_Only_Students_Still_Owing_Letters_When_Active(
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