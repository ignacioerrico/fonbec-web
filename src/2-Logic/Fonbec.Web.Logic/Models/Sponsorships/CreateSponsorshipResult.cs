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
    IReadOnlyList<string>? CompletedPlanMonthLabels = null)
{
    public bool AnyAffectedRows => AffectedRows > 0;
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