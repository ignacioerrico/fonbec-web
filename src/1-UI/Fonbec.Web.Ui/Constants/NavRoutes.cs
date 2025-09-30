namespace Fonbec.Web.Ui.Constants;

public static class NavRoutes
{
    public const string Home = "/";

    public const string Chapters = "/filiales";

    public const string Users = "/usuarios";

    public const string UserCreate = $"{Users}/alta";
    
    public const string Students = "/becarios";

    public const string StudentCreate = $"{Students}/alta";

    public static string UsersUserIdPermissions(int userId) => $"{Users}/{userId}/permisos";
}