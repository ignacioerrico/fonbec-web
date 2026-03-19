namespace Fonbec.Web.Ui.Models.Company;

public class CompanyCreateBindModel
{
    public string CompanyName { get; set; } = null!;

    public string CompanyEmail { get; set; } = null!;

    public string CompanyPhoneNumber { get; set; } = null!;
    public List<PointOfContactBindModel> PointsOfContact { get; set; } = [];

}

