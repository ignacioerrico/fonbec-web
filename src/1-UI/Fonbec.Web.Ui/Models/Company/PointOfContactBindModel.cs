namespace Fonbec.Web.Ui.Models.Company;

public class PointOfContactBindModel
{
    public Guid TempId { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? NickName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}