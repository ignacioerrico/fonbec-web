namespace Fonbec.Web.DataAccess.DataModels.Companies.Input;

public record CreateCompanyRepositoryResult(int CompanyId = 0, IReadOnlyList<int>? MissingSponsorIds = null);