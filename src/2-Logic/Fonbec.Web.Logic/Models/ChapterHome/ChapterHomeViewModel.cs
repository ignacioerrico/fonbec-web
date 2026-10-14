using Fonbec.Web.Logic.Models.LetterPlanProgress;

namespace Fonbec.Web.Logic.Models.ChapterHome;

public class ChapterHomeViewModel
{
    public IReadOnlyList<ChapterHomePersonViewModel> Managers { get; init; } = [];

    public IReadOnlyList<ChapterHomePersonViewModel> Facilitators { get; init; } = [];

    public IReadOnlyList<ChapterHomePersonViewModel> Reviewers { get; init; } = [];

    public int StudentCount { get; init; }

    public int SponsorCount { get; init; }

    public ChapterHomeCampaignViewModel? Campaign { get; init; }
}

public class ChapterHomePersonViewModel
{
    public int UserId { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? NickName { get; init; }

    public string Email { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public bool IsCurrentUser { get; init; }
}

public class ChapterHomeCampaignViewModel
{
    public int PlanId { get; init; }

    public string Label { get; init; } = string.Empty;

    public LetterPlanProgressSummaryViewModel Summary { get; init; } = new();
}