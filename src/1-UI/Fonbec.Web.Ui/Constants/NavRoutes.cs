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

    public const string LetterFollowUp = "/cartas/seguimiento";

    public const string PlannnedDeliveryCreate = $"{PlannedDeliveries}/alta";

    public static string LetterPlanProgress(int planId) => $"{PlannedDeliveries}/{planId}/cartas";

    public const string LetterPlanProgressRouteTemplate = $"{PlannedDeliveries}/{{PlanId:int}}/cartas";

    public const string Students = "/becarios";

    public const string StudentCreate = $"{Students}/alta";

    public const string Sponsors = "/padrinos";

    public const string SponsorCreate = $"{Sponsors}/alta";

    public static string SponsorRecipients(int sponsorId) => $"{Sponsors}/{sponsorId}/destinatarios";

    public const string SponsorRecipientsRouteTemplate = $"{Sponsors}/{{SponsorId:int}}/destinatarios";

    public static string SponsorHistory(Guid token, int studentId) =>
        $"{Sponsors}/{token}/{studentId}";

    public const string SponsorHistoryRouteTemplate =
        $"{Sponsors}/{{Token:guid}}/{{StudentId:int}}";

    public static string SponsorHistoryDownload(Guid token, int studentId, long documentId, int pageNumber) =>
        $"{SponsorHistory(token, studentId)}/documentos/{documentId}/paginas/{pageNumber}";

    public const string SponsorHistoryDownloadRouteTemplate =
        $"{Sponsors}/{{token:guid}}/{{studentId:int}}/documentos/{{documentId:long}}/paginas/{{pageNumber:int}}";

    public const string FacilitatorStudents = "/mediadores/mis-becarios";

    public const string StudentMessages = "/mensajes-para-becarios";

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

    public const string FacilitatorOtherDocumentsTemplate =
        $"{FacilitatorStudents}/becarios/{{studentId:int}}/otros";

    public static string FacilitatorOtherDocuments(int studentId) =>
        $"{FacilitatorStudents}/becarios/{studentId}/otros";

    public const string FacilitatorOtherDocumentPageTemplate =
        $"{FacilitatorStudents}/becarios/{{studentId:int}}/otros/documentos/{{documentId:long}}/paginas/{{pageNumber:int}}";

    public static string FacilitatorOtherDocumentPage(int studentId, long documentId, int pageNumber, bool download = false)
    {
        var url = $"{FacilitatorOtherDocuments(studentId)}/documentos/{documentId}/paginas/{pageNumber}";
        return download ? $"{url}?descargar=true" : url;
    }

    public const string ManagerUploadDocumentTemplate =
        $"{Students}/{{studentId:int}}/subir";

    public static string ManagerUploadDocument(int studentId) =>
        $"{Students}/{studentId}/subir";

    public static string ManagerUploadLetter(
        int studentId,
        int planId,
        int? sponsorId,
        int? companyId,
        string? returnUrl = null)
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

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            query += $"&volver={Uri.EscapeDataString(returnUrl)}";
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

    public static string CompanyHistory(Guid token, int studentId) =>
        $"{Companies}/{token}/{studentId}";

    public const string CompanyHistoryRouteTemplate =
        $"{Companies}/{{Token:guid}}/{{StudentId:int}}";

    public static string CompanyHistoryDownload(Guid token, int studentId, long documentId, int pageNumber) =>
        $"{CompanyHistory(token, studentId)}/documentos/{documentId}/paginas/{pageNumber}";

    public const string CompanyHistoryDownloadRouteTemplate =
        $"{Companies}/{{token:guid}}/{{studentId:int}}/documentos/{{documentId:long}}/paginas/{{pageNumber:int}}";

    public const string ReviewQueue = "/revisar";

    public static string ReviewDocument(long documentId) => $"{ReviewQueue}/{documentId}";

    public const string ReviewDocumentRouteTemplate = $"{ReviewQueue}/{{DocumentId:long}}";

    public static string ReviewDocumentPage(long documentId, int pageNumber) =>
        $"{ReviewQueue}/documento/{documentId}/pagina/{pageNumber}";

    public const string ReviewDocumentPageRouteTemplate =
        $"{ReviewQueue}/documento/{{documentId:long}}/pagina/{{pageNumber:int}}";

    public const string ImprovementQueue = "/mejorar";

    public static string ImproveDocument(long documentId) => $"{ImprovementQueue}/{documentId}";

    public const string ImproveDocumentRouteTemplate = $"{ImprovementQueue}/{{DocumentId:long}}";

    public static string ImproveDocumentPage(long documentId, int pageNumber) =>
        $"{ImprovementQueue}/documento/{documentId}/pagina/{pageNumber}";

    public const string ImproveDocumentPageRouteTemplate =
        $"{ImprovementQueue}/documento/{{documentId:long}}/pagina/{{pageNumber:int}}";
}