using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Students;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Students;

namespace Fonbec.Web.Logic.Tests.Models.Students;

public class LetterAssessmentSummariesTests
{
    [Fact]
    public void FromScores_Averages_Each_Rating_And_The_Overall_Score()
    {
        var summaries = LetterAssessmentSummaries.FromScores(
        [
            Score(studentId: 10, planId: 1, spelling: 5, penmanship: 4, content: 3),
            Score(studentId: 10, planId: 2, spelling: 3, penmanship: 4, content: 5),
        ]);

        var summary = summaries[10];
        summary.OverallText.Should().Be("4,0");
        summary.SpellingText.Should().Be("4,0");
        summary.PenmanshipText.Should().Be("4,0");
        summary.ContentText.Should().Be("4,0");
        summary.ShowTrend.Should().BeTrue();
        summary.PlanLabels.Should().Equal(
            new DateTime(2026, 3, 1).ToSpanishMonthYear(),
            new DateTime(2026, 6, 1).ToSpanishMonthYear());
    }

    [Fact]
    public void FromScores_Uses_One_Point_Per_Delivery_When_A_Campaign_Has_Two_Letters()
    {
        var summaries = LetterAssessmentSummaries.FromScores(
        [
            Score(studentId: 10, planId: 1, spelling: 2, penmanship: 4, content: 4),
            Score(studentId: 10, planId: 1, spelling: 4, penmanship: 2, content: 4),
        ]);

        var summary = summaries[10];
        summary.ShowTrend.Should().BeFalse();
        summary.SpellingByPlan.Should().Equal(3d);
        summary.PenmanshipByPlan.Should().Equal(3d);
        summary.ContentByPlan.Should().Equal(4d);
        summary.OverallText.Should().Be("3,3");
        summary.PlanLabels.Should().ContainSingle();
    }

    [Fact]
    public void FromScores_Returns_Empty_When_There_Are_No_Letters()
    {
        LetterAssessmentSummaries.FromScores([]).Should().BeEmpty();
    }

    private static StudentLetterScoreDataModel Score(
        int studentId,
        int planId,
        int spelling,
        int penmanship,
        int content) =>
        new()
        {
            StudentId = studentId,
            PlanId = planId,
            PlanStartsOn = new DateTime(2026, planId == 1 ? 3 : 6, 1),
            SpellingScore = spelling,
            PenmanshipScore = penmanship,
            ContentScore = content,
        };
}