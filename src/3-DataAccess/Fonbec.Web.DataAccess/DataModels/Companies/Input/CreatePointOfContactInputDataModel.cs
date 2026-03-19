namespace Fonbec.Web.DataAccess.DataModels.Companies.Input;

public class CreatePointOfContactInputDataModel
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? NickName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int CreatedById { get; set; }
}