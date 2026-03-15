namespace Fonbec.Web.Ui.Constants;

public static class NavRoutes
{
    public const string Home = "/";

    public const string Chapters = "/filiales";

    public const string ChapterCreate = $"{Chapters}/alta";

    public const string Users = "/usuarios";

    public const string UserCreate = $"{Users}/alta";

    public const string Students = "/becarios";

    public const string StudentCreate = $"{Students}/alta";

    public const string Sponsors = "/padrinos";

    public const string SponsorCreate = $"{Sponsors}/alta";

    public static string UsersUserIdPermissions(int userId) => $"{Users}/{userId}/permisos";
    public static string Sponsorship(int studentId) => $"/becarios/{studentId}/padrinos";
    public static string SponsorshipCreate(int studentId) => $"{Sponsorship(studentId)}/alta";
    public static string Sponsorships(int studentId) => $"/becarios/{studentId}/padrinos";
        
    public const string Companies = "/empresas";

    public const string CompanyCreate = $"{Companies}/alta";

    public const string PlannedDeliveries = $"/PlannedDeliveries";

    public const string PlannnedDeliveryCreate = $"{PlannedDeliveries}/alta";
}