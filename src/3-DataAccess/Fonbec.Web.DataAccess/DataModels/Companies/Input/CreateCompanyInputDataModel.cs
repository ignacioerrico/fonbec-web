namespace Fonbec.Web.DataAccess.DataModels.Companies.Input;

public class CreateCompanyInputDataModel
{
    public string CompanyName { get; set; } = null!;

    public string? CompanyEmail { get; set; }

    public string? CompanyPhoneNumber { get; set; }

    public string? CompanyNotes { get; set; }

    public List<CreateCompanyPointOfContactInputDataModel> PointsOfContact { get; set; } = [];

    public List<int> SponsorIds { get; set; } = [];

    public int CreatedById { get; set; }
}

public class CreateCompanyPointOfContactInputDataModel
{
    public string PocFirstName { get; set; } = null!;
    public string? PocLastName { get; set; } = null!;
    public string? PocNickName { get; set; } = null!;
    public string? PocEmail { get; set; }
    public string? PocPhoneNumber { get; set; }
    public string? PocNotes { get; set; }
}