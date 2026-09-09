namespace Fonbec.Web.Logic.Models.Sponsorships;

public enum SponsorshipPeriodStatus
{
    Available,
    OverlapsExisting,
    ExtendsExisting,
}

public record CreateSponsorshipResult(
    int AffectedRows = 0,
    SponsorshipPeriodStatus PeriodStatus = SponsorshipPeriodStatus.Available)
{
    public bool AnyAffectedRows => AffectedRows > 0;
}