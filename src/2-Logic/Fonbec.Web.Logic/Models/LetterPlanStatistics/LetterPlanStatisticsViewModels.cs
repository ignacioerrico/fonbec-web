namespace Fonbec.Web.Logic.Models.LetterPlanStatistics;

public class LetterPlanStatisticsViewModel
{
    public string PlanLabel { get; init; } = null!;

    public DurationChartViewModel WrittenToNotification { get; init; } = null!;

    public DurationChartViewModel WaitForImprovement { get; init; } = null!;

    public DurationChartViewModel WaitForReview { get; init; } = null!;

    public DurationChartViewModel ImprovementDuration { get; init; } = null!;

    public DurationChartViewModel ReviewDuration { get; init; } = null!;

    public CountChartViewModel RejectionsByReviewer { get; init; } = null!;

    public StackedCountChartViewModel ReviewerActivity { get; init; } = null!;

    public CountChartViewModel RejectionReasons { get; init; } = null!;

    public CountChartViewModel Outcomes { get; init; } = null!;

    public GroupedCountChartViewModel Scores { get; init; } = null!;
}

public class DurationChartViewModel
{
    public string[] Labels { get; init; } = [];

    public double[] Counts { get; init; } = [];

    public string? MedianText { get; init; }

    public string? AverageText { get; init; }

    public string? MinText { get; init; }

    public string? MaxText { get; init; }

    public string? Note { get; init; }

    public bool HasData { get; init; }
}

public class CountChartViewModel
{
    public string[] Labels { get; init; } = [];

    public double[] Counts { get; init; } = [];

    public string? Note { get; init; }

    public bool HasData { get; init; }
}

public class StackedCountChartViewModel
{
    public string[] Labels { get; init; } = [];

    public double[] Approvals { get; init; } = [];

    public double[] Rejections { get; init; } = [];

    public string? Note { get; init; }

    public bool HasData { get; init; }
}

public class GroupedCountChartViewModel
{
    public string[] Labels { get; init; } = [];

    public double[] Spelling { get; init; } = [];

    public double[] Penmanship { get; init; } = [];

    public double[] Content { get; init; } = [];

    public bool HasData { get; init; }
}