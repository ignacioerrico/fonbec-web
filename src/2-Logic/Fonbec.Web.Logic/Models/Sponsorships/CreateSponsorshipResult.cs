namespace Fonbec.Web.Logic.Models.Sponsorships;

public enum SponsorshipPeriodStatus
{
    Available,
    OverlapsExisting,
    ExtendsExisting,
    UncoversLockedPlan,
}

public record CreateSponsorshipResult(
    int AffectedRows = 0,
    SponsorshipPeriodStatus PeriodStatus = SponsorshipPeriodStatus.Available,
    IReadOnlyList<string>? CompletedPlanMonthLabels = null,
    IReadOnlyList<string>? LockedPlanMonthLabels = null)
{
    public bool AnyAffectedRows => AffectedRows > 0;
}

public class OverlappingSponsorshipToEndViewModel
{
    public int SponsorshipId { get; init; }
    public string RecipientName { get; init; } = string.Empty;
    public string StartMonthLabel { get; init; } = string.Empty;
    public DateTime ProposedEndDate { get; init; }
    public string ProposedEndMonthLabel { get; init; } = string.Empty;
}

public class CreateSponsorshipPreview
{
    public SponsorshipPeriodStatus PeriodStatus { get; init; }

    public IReadOnlyList<OverlappingSponsorshipToEndViewModel> OverlappingToEnd { get; init; } = [];
}

public enum UpdateSponsorshipStatus
{
    Saved,
    OverlapsExisting,
    UncoversLockedPlan,
    RequiresExemptionRevocation,
    AddsSlotToCompletedPlan,
    NotFound,
}

public record UpdateSponsorshipResult(
    int AffectedRows = 0,
    UpdateSponsorshipStatus Status = UpdateSponsorshipStatus.Saved,
    IReadOnlyList<string>? LockedPlanMonthLabels = null,
    IReadOnlyList<string>? ExemptPlanMonthLabels = null,
    IReadOnlyList<string>? CompletedPlanMonthLabels = null)
{
    public bool AnyAffectedRows => AffectedRows > 0;
}