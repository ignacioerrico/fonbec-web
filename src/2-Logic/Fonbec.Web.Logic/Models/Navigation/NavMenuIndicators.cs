namespace Fonbec.Web.Logic.Models.Navigation;

public sealed class NavMenuIndicators
{
    public static NavMenuIndicators Empty { get; } = new();

    public int? StudentCount { get; init; }

    public int? SponsorCount { get; init; }

    public int? CompanyCount { get; init; }

    /// <summary>Completion of the chapter's open campaign, or null when there is no open campaign.</summary>
    public int? CampaignCompletionPercent { get; init; }

    public int RedFlags { get; init; }

    public int GreenFlags { get; init; }

    /// <summary>Documents waiting for digital improvement, or null when the user cannot improve images.</summary>
    public int? PendingImprovement { get; init; }

    /// <summary>Documents waiting for review, or null when the user cannot review.</summary>
    public int? PendingReview { get; init; }
}

public static class NavMenuFormatting
{
    public static string CountNote(int count) => $"({count})";

    public static string PercentNote(int percent) => $"({percent}%)";

    public static string RedFlags(int count) =>
        count == 1 ? "1 bandera roja" : $"{count} banderas rojas";

    public static string GreenFlags(int count) =>
        count == 1 ? "1 bandera verde" : $"{count} banderas verdes";

    public static string PendingChanges(int count) => count switch
    {
        > 10 => "Más de 10 pendientes",
        1 => "1 pendiente",
        _ => $"{count} pendientes",
    };
}