namespace Fonbec.Web.DataAccess.Constants;

public static class FonbecRole
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Uploader = "Uploader";
    public const string Reviewer = "Reviewer";

    public static string[] AllRoles => [Admin, Manager, Uploader, Reviewer];
}