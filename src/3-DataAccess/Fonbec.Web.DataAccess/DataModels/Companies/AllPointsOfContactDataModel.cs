namespace Fonbec.Web.DataAccess.DataModels.Companies;

public class AllPointsOfContactDataModel
{
    public int PointOfContactId { get; set; }
    public int CompanyId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? NickName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string CompanyName { get; set; } = null!;
}