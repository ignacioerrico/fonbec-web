namespace Fonbec.Web.DataAccess.DataModels.Companies.Input;

public class UpdateCompanyRelationsInputDataModel
{
    public int CompanyId { get; set; }

    public int UpdatedById { get; set; }

    public List<UpdateCompanyContactInputDataModel> Contacts { get; set; } = [];

    public List<int> SponsorIds { get; set; } = [];
}

public class UpdateCompanyContactInputDataModel
{
    public int? Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; }

    public string? NickName { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Notes { get; set; }
}

public record UpdateCompanyRelationsRepositoryResult(
    bool CompanyFound,
    int AffectedRows = 0,
    IReadOnlyList<int>? MissingSponsorIds = null,
    bool HasUnknownContacts = false);
