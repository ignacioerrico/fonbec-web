using System.Globalization;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.DataModels.RecipientMessages;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Services;

namespace Fonbec.Web.Logic.Email;

public static class EmailNotificationValues
{
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-AR");

    public static Dictionary<string, string?> ForDocument(DocumentShareNotificationDataModel share, string historyUrl)
    {
        var values = PersonAndStudent(
            share.IsCompany,
            share.RecipientGender,
            share.RecipientNickName,
            share.RecipientName,
            share.RecipientLastName,
            share.StudentGender,
            share.StudentNickName,
            share.StudentFirstName,
            share.StudentLastName);
        AddDocumentWords(values, share.DocumentType, share.StudentIsAtUniversity);
        values["enlace-historial-documentos"] = historyUrl;
        return values;
    }

    public static Dictionary<string, string?> ForPlan(DateTime planStartsOn, string progressUrl) => new()
    {
        ["plan-mes"] = planStartsOn.ToString("MMMM", Spanish),
        ["plan-año"] = planStartsOn.ToString("yyyy", Spanish),
        ["enlace-avance-campaña"] = progressUrl,
    };

    public static Dictionary<string, string?> ForFacilitatorRejection(
        FacilitatorRejectionNotificationDataModel notice,
        string studentsUrl)
    {
        var values = Student(notice.StudentGender, notice.StudentNickName, notice.StudentFirstName, notice.StudentLastName);
        AddDocumentWords(values, notice.DocumentType, notice.StudentIsAtUniversity);
        values["motivo-de-rechazo"] = DocumentNotificationMessageFormatter.FormatRejectionReason(
            notice.RejectionReasonDescription, notice.RejectionNotes);
        values["mediador"] = Mediador(notice.FacilitatorGender);
        values["mediador-apodo"] = NickOrFirst(notice.FacilitatorNickName, notice.FacilitatorFirstName);
        values["enlace-mis-becarios"] = studentsUrl;
        return values;
    }

    public static Dictionary<string, string?> ForHeldRejection(
        HeldRejectionManagerNotificationDataModel notice,
        string managerNickName,
        string rejectedDocumentsUrl)
    {
        var values = Student(notice.StudentGender, notice.StudentNickName, notice.StudentFirstName, notice.StudentLastName);
        AddDocumentWords(values, notice.DocumentType, notice.StudentIsAtUniversity);
        values["motivo-de-rechazo"] = DocumentNotificationMessageFormatter.FormatRejectionReason(
            notice.RejectionReasonDescription, notice.RejectionNotes);
        values["coordinador-apodo"] = managerNickName;
        values["enlace-documentos-rechazados"] = rejectedDocumentsUrl;
        return values;
    }

    public static Dictionary<string, string?> ForRecipientMessage(
        RecipientMessageFacilitatorNotificationDataModel notice,
        string messagesUrl)
    {
        var values = PersonAndStudent(
            notice.IsCompany,
            notice.SenderGender,
            notice.SenderNickName,
            notice.SenderFirstName,
            notice.SenderLastName,
            notice.StudentGender,
            notice.StudentNickName,
            notice.StudentFirstName,
            notice.StudentLastName);
        values["mensaje-de-padrino"] = notice.Body;
        values["mediador"] = Mediador(notice.FacilitatorGender);
        values["mediador-apodo"] = NickOrFirst(notice.FacilitatorNickName, notice.FacilitatorFirstName);
        values["enlace-mensajes-para-becarios"] = messagesUrl;
        return values;
    }

    public static string NickOrFirst(string? nickName, string? firstName) =>
        string.IsNullOrWhiteSpace(nickName) ? (firstName ?? string.Empty).Trim() : nickName.Trim();

    public static string FirstWord(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        var space = trimmed.IndexOf(' ');
        return space < 0 ? trimmed : trimmed[..space];
    }

    private static Dictionary<string, string?> PersonAndStudent(
        bool isCompany,
        Gender? personGender,
        string? personNickName,
        string? personFirstName,
        string? personLastName,
        Gender studentGender,
        string? studentNickName,
        string? studentFirstName,
        string? studentLastName)
    {
        var values = Student(studentGender, studentNickName, studentFirstName, studentLastName);
        values["padrino-apodo"] = isCompany
            ? (personFirstName ?? string.Empty).Trim()
            : NickOrFirst(personNickName, personFirstName);
        values["padrino-nombre"] = (personFirstName ?? string.Empty).Trim();
        values["padrino-apellido"] = isCompany ? string.Empty : (personLastName ?? string.Empty).Trim();
        values["su-padrino"] = isCompany ? "la empresa" : GenderWords.SuPadrino(personGender);
        values["ahijado"] = GenderWords.Ahijado(studentGender);
        return values;
    }

    private static Dictionary<string, string?> Student(
        Gender gender,
        string? nickName,
        string? firstName,
        string? lastName) => new()
    {
        ["becario-apodo"] = NickOrFirst(nickName, firstName),
        ["becario-nombre"] = (firstName ?? string.Empty).Trim(),
        ["becario-apellido"] = (lastName ?? string.Empty).Trim(),
        ["el-becario"] = GenderWords.BecarioWithArticle(gender),
        ["ahijado"] = GenderWords.Ahijado(gender),
    };

    private static void AddDocumentWords(Dictionary<string, string?> values, DocumentType documentType, bool university)
    {
        var (nuevo, unNuevo, documento, elDocumento, rechazado) = documentType switch
        {
            DocumentType.Letter => ("nueva carta", "una nueva carta", "carta", "la carta", "carta rechazada"),
            DocumentType.ReportCard when university => (
                "nueva libreta universitaria",
                "una nueva libreta universitaria",
                "libreta universitaria",
                "la libreta universitaria",
                "libreta universitaria rechazada"),
            DocumentType.ReportCard => ("nuevo boletín", "un nuevo boletín", "boletín", "el boletín", "boletín rechazado"),
            _ => ("nuevo documento", "un nuevo documento", "documento", "el documento", "documento rechazado"),
        };

        values["nuevo-documento"] = nuevo;
        values["un-nuevo-documento"] = unNuevo;
        values["documento"] = documento;
        values["el-documento"] = elDocumento;
        values["documento-rechazado"] = rechazado;
    }

    private static string Mediador(Gender gender) => gender switch
    {
        Gender.Male => "mediador",
        Gender.Female => "mediadora",
        _ => "mediador/mediadora",
    };
}