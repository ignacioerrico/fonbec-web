namespace Fonbec.Web.Logic.Models.Companies;

public record CreateCompanyResult(
    int AffectedRows = 0,
    IReadOnlyList<MissingSponsor>? MissingSponsors = null)
{
    public bool AnyAffectedRows => AffectedRows > 0;

    public bool HasMissingSponsors => MissingSponsors is { Count: > 0 };
}

public record MissingSponsor(int SponsorId, string SponsorName);