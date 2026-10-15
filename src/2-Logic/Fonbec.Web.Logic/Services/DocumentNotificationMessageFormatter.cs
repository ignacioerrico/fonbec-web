using System.Net;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Services;

public static class DocumentNotificationMessageFormatter
{
    public static string GetSponsorSalutation(string? nickName, string firstName) =>
        string.IsNullOrWhiteSpace(nickName) ? firstName : nickName;

    public static string GetStudentTerm(Gender gender) => GenderWords.Ahijado(gender);

    public static string GetStudentDisplayName(string firstName, string lastName, string? nickName)
    {
        var givenName = string.IsNullOrWhiteSpace(nickName) ? firstName : nickName;
        return $"{givenName} {lastName}";
    }

    public static string BuildBodyHtml(
        string sponsorSalutation,
        string studentTerm,
        string studentDisplayName,
        string historyUrl) =>
        $"""
         <p>Hola, {sponsorSalutation}:</p>
         <p>Hay un nuevo documento disponible de tu {studentTerm} {studentDisplayName}.</p>
         <p><a href="{historyUrl}">Ver historial de documentos</a></p>
         """;

    public static string BuildNotificationHtml(DocumentShareNotificationDataModel share, string historyUrl)
    {
        // Person-sponsors and companies get the same message; a company's name has no nickname.
        var salutation = GetSponsorSalutation(share.RecipientNickName, share.RecipientName);
        var godchildTerm = GetStudentTerm(share.StudentGender);
        var studentName = GetStudentDisplayName(
            share.StudentFirstName, share.StudentLastName, share.StudentNickName);

        return BuildBodyHtml(salutation, godchildTerm, studentName, historyUrl);
    }

    public const string FacilitatorRejectionSubject = "Documento rechazado";

    public static string GetRejectedDocumentPhrase(DocumentType documentType) => documentType switch
    {
        DocumentType.Letter => "la carta",
        DocumentType.ReportCard => "el boletín",
        DocumentType.Other => "el otro documento",
        _ => "el documento",
    };

    public static string FormatRejectionReason(string? description, string? notes)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.IsNullOrWhiteSpace(notes) ? "No se indicó un motivo." : notes.Trim();
        }

        return string.IsNullOrWhiteSpace(notes)
            ? description.Trim()
            : $"{description.Trim()}: {notes.Trim()}";
    }

    public static string BuildFacilitatorRejectionHtml(
        string facilitatorFirstName,
        string studentDisplayName,
        DocumentType documentType,
        string reason,
        string studentsUrl)
    {
        var greeting = WebUtility.HtmlEncode(facilitatorFirstName);
        var student = WebUtility.HtmlEncode(studentDisplayName);
        var encodedReason = WebUtility.HtmlEncode(reason);
        var phrase = GetRejectedDocumentPhrase(documentType);

        return $"""
                <p>Hola, {greeting}:</p>
                <p>Se rechazó {phrase} de {student}.</p>
                <p>Motivo: {encodedReason}</p>
                <p><a href="{studentsUrl}">Ver Mis becarios</a></p>
                """;
    }

    public const string HeldRejectionManagerSubject = "Documento rechazado para decidir";

    public static string BuildHeldRejectionManagerHtml(
        string managerFirstName,
        string studentDisplayName,
        DocumentType documentType,
        string reason,
        string rejectedDocumentsUrl)
    {
        var greeting = WebUtility.HtmlEncode(managerFirstName);
        var student = WebUtility.HtmlEncode(studentDisplayName);
        var encodedReason = WebUtility.HtmlEncode(reason);
        var phrase = GetRejectedDocumentPhrase(documentType);

        return $"""
                <p>Hola, {greeting}:</p>
                <p>Se rechazó {phrase} de {student} y está esperando tu decisión.</p>
                <p>Motivo: {encodedReason}</p>
                <p><a href="{rejectedDocumentsUrl}">Ver documentos rechazados</a></p>
                """;
    }

    public static string BuildPlanReadyHtml(string planLabel, string progressUrl) =>
        $"""
         <p>La campaña de {planLabel} está lista para marcarse como completada.</p>
         <p><a href="{progressUrl}">Ver avance de la campaña</a></p>
         """;
}
