namespace Fonbec.Web.DataAccess.DataModels.Companies;

public class CompanyRelationsDataModel
{
    public int CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public List<CompanyContactDataModel> Contacts { get; set; } = [];

    public List<CompanyLinkedSponsorDataModel> Sponsors { get; set; } = [];
}

public class CompanyContactDataModel
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; }

    public string? NickName { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Notes { get; set; }
}

public class CompanyLinkedSponsorDataModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;
}