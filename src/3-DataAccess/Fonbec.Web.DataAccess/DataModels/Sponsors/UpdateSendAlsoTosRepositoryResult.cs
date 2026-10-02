namespace Fonbec.Web.DataAccess.DataModels.Sponsors;

public record UpdateSendAlsoTosRepositoryResult(bool SponsorFound, int AffectedRows, bool Rejected = false);