namespace Fonbec.Web.Ui.Constants;

public static class NavRoutes
{
    public const string Home = "/";

    public const string Chapters = "/filiales";

    public const string ChapterCreate = $"{Chapters}/alta";

    public const string Users = "/usuarios";

    public const string UserCreate = $"{Users}/alta";

    public static string UsersUserIdPermissions(int userId) => $"{Users}/{userId}/permisos";

    public const string PlannedDeliveries = "/planificaciones";

    public const string PlannnedDeliveryCreate = $"{PlannedDeliveries}/alta";

    public static string LetterPlanProgress(int planId) => $"{PlannedDeliveries}/{planId}/cartas";

    public const string LetterPlanProgressRouteTemplate = $"{PlannedDeliveries}/{{PlanId:int}}/cartas";

    public const string Students = "/becarios";

    public const string StudentCreate = $"{Students}/alta";

    public const string Sponsors = "/padrinos";

    public const string SponsorCreate = $"{Sponsors}/alta";

    public const string FacilitatorStudents = "/mediadores/mis-becarios";

    public const string FacilitatorUploadDocumentTemplate =
        $"{FacilitatorStudents}/becarios/{{studentId:int}}/subir";

    public static string FacilitatorUploadDocument(int studentId) =>
        $"{FacilitatorStudents}/becarios/{studentId}/subir";

    public static string FacilitatorUploadLetter(int studentId, int planId, int? sponsorId, int? companyId)
    {
        var query = $"tipo=carta&planId={planId}";
        if (sponsorId.HasValue)
        {
            query += $"&padrinoId={sponsorId.Value}";
        }

        if (companyId.HasValue)
        {
            query += $"&empresaId={companyId.Value}";
        }

        return $"{FacilitatorUploadDocument(studentId)}?{query}";
    }

    public static string FacilitatorUploadReportCard(int studentId) =>
        $"{FacilitatorUploadDocument(studentId)}?tipo=boletin";

    public static string FacilitatorUploadOther(int studentId) =>
        $"{FacilitatorUploadDocument(studentId)}?tipo=otro";

    public const string ManagerUploadDocumentTemplate =
        $"{Students}/{{studentId:int}}/subir";

    public static string ManagerUploadDocument(int studentId) =>
        $"{Students}/{studentId}/subir";

    public static string ManagerUploadLetter(int studentId, int planId, int? sponsorId, int? companyId)
    {
        var query = $"tipo=carta&planId={planId}";
        if (sponsorId.HasValue)
        {
            query += $"&padrinoId={sponsorId.Value}";
        }

        if (companyId.HasValue)
        {
            query += $"&empresaId={companyId.Value}";
        }

        return $"{ManagerUploadDocument(studentId)}?{query}";
    }

    public static string ManagerUploadReportCard(int studentId) =>
        $"{ManagerUploadDocument(studentId)}?tipo=boletin";

    public static string ManagerUploadOther(int studentId) =>
        $"{ManagerUploadDocument(studentId)}?tipo=otro";

    public static string Sponsorships(int studentId) => $"/becarios/{studentId}/padrinos";

    public static string SponsorshipCreate(int studentId) => $"{Sponsorships(studentId)}/alta";

    public const string Companies = "/empresas";

    public const string CompanyCreate = $"{Companies}/alta";
}