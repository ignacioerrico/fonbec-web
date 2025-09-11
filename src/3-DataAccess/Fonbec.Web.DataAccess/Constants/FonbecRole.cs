namespace Fonbec.Web.DataAccess.Constants;

public static class FonbecRole
{
    public static readonly Dictionary<string, string> Translator = new()
    {
        { Admin, "Admin" },
        { Manager, "Coordinador" },
        { Reviewer, "Revisor" },
        { Uploader, "Mediador" },
    };

    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Uploader = "Uploader";
    public const string Reviewer = "Reviewer";

    public static string[] AllRoles => [Admin, Manager, Uploader, Reviewer];
}