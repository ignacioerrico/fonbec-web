using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Ui.Options;

public class AdminUserOptions
{
    public const string SectionName = "AdminUser";

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? NickName { get; set; }
    public Gender Gender { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}