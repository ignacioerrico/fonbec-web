using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Services;

public static class DocumentNotificationMessageFormatter
{
    public static string GetSponsorSalutation(string? nickName, string firstName) =>
        string.IsNullOrWhiteSpace(nickName) ? firstName : nickName;

    public static string GetStudentTerm(Gender gender) =>
        gender switch
        {
            Gender.Male => "ahijado",
            Gender.Female => "ahijada",
            _ => "ahijado/a",
        };

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
}
