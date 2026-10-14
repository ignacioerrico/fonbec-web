using System.Globalization;
using Fonbec.Web.DataAccess.DataModels.Students;
using Fonbec.Web.Logic.ExtensionMethods;

namespace Fonbec.Web.Logic.Models.Students;

public class LetterAssessmentSummaryViewModel
{
    public double OverallAverage { get; init; }

    public string OverallText { get; init; } = null!;

    public string SpellingText { get; init; } = null!;

    public string PenmanshipText { get; init; } = null!;

    public string ContentText { get; init; } = null!;

    public bool ShowTrend { get; init; }

    public string[] PlanLabels { get; init; } = [];

    public double[] SpellingByPlan { get; init; } = [];

    public double[] PenmanshipByPlan { get; init; } = [];

    public double[] ContentByPlan { get; init; } = [];
}

public static class LetterAssessmentSummaries
{
    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");

    public static Dictionary<int, LetterAssessmentSummaryViewModel> FromScores(
        IEnumerable<StudentLetterScoreDataModel> scores) =>
        scores
            .GroupBy(score => score.StudentId)
            .ToDictionary(group => group.Key, Summarize);

    private static LetterAssessmentSummaryViewModel Summarize(IEnumerable<StudentLetterScoreDataModel> letters)
    {
        var list = letters.ToList();
        var byPlan = list
            .GroupBy(letter => letter.PlanId)
            .Select(plan => new
            {
                plan.First().PlanStartsOn,
                PlanId = plan.Key,
                Spelling = plan.Average(letter => letter.SpellingScore),
                Penmanship = plan.Average(letter => letter.PenmanshipScore),
                Content = plan.Average(letter => letter.ContentScore),
            })
            .OrderBy(plan => plan.PlanStartsOn)
            .ThenBy(plan => plan.PlanId)
            .ToList();

        var overall = list.Average(letter =>
            (letter.SpellingScore + letter.PenmanshipScore + letter.ContentScore) / 3d);

        return new LetterAssessmentSummaryViewModel
        {
            OverallAverage = overall,
            OverallText = FormatScore(overall),
            SpellingText = FormatScore(list.Average(letter => letter.SpellingScore)),
            PenmanshipText = FormatScore(list.Average(letter => letter.PenmanshipScore)),
            ContentText = FormatScore(list.Average(letter => letter.ContentScore)),
            ShowTrend = byPlan.Count >= 2,
            PlanLabels = byPlan.Select(plan => plan.PlanStartsOn.ToSpanishMonthYear()).ToArray(),
            SpellingByPlan = byPlan.Select(plan => plan.Spelling).ToArray(),
            PenmanshipByPlan = byPlan.Select(plan => plan.Penmanship).ToArray(),
            ContentByPlan = byPlan.Select(plan => plan.Content).ToArray(),
        };
    }

    private static string FormatScore(double score) => score.ToString("0.0", EsAr);
}