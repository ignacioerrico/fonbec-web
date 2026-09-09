namespace Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;

public enum SponsorshipPeriodMatch
{
    None,
    Overlap,
    Adjacent,
}

public record CreateSponsorshipRepositoryResult(
    int AffectedRows = 0,
    SponsorshipPeriodMatch PeriodMatch = SponsorshipPeriodMatch.None);
