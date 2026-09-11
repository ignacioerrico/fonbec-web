namespace Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;

public enum UpdateSponsorshipOutcome
{
    Saved,
    Overlap,
    UncoversLockedPlan,
    RequiresExemptionRevocation,
    NotFound,
}

public record UpdateSponsorshipRepositoryResult(
    int AffectedRows = 0,
    UpdateSponsorshipOutcome Outcome = UpdateSponsorshipOutcome.Saved,
    IReadOnlyList<DateTime>? UncoveredPlanStartsOn = null,
    IReadOnlyList<DateTime>? ExemptPlanStartsOn = null,
    SponsorshipPeriodMatch PeriodMatch = SponsorshipPeriodMatch.None);
