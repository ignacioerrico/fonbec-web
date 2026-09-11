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
    SponsorshipPeriodStatus PeriodStatus = SponsorshipPeriodStatus.Available)
{
    public bool AnyAffectedRows => AffectedRows > 0;
}

public enum UpdateSponsorshipStatus
{
    Saved,
    OverlapsExisting,
    UncoversLockedPlan,
    RequiresExemptionRevocation,
    NotFound,
}

public record UpdateSponsorshipResult(
    int AffectedRows = 0,
    UpdateSponsorshipStatus Status = UpdateSponsorshipStatus.Saved,
    IReadOnlyList<string>? LockedPlanMonthLabels = null,
    IReadOnlyList<string>? ExemptPlanMonthLabels = null)
{
    public bool AnyAffectedRows => AffectedRows > 0;
}