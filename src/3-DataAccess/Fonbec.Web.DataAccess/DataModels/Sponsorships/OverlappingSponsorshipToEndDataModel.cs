namespace Fonbec.Web.DataAccess.DataModels.Sponsorships;

public class OverlappingSponsorshipToEndDataModel
{
    public int SponsorshipId { get; init; }
    public string RecipientName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime ProposedEndDate { get; init; }
}