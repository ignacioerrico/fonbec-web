using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Services;

public static class DocumentNotificationMessageFormatter
{
    public static string GetSponsorSalutation(string? nickName, string firstName) =>
        string.IsNullOrWhiteSpace(nickName) ? firstName : nickName;

    public static string GetGodchildTerm(Gender gender) =>
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
        string godchildTerm,
        string studentDisplayName,
        string historyUrl) =>
        $"""
         <p>Hola, {sponsorSalutation}:</p>
         <p>Hay un nuevo documento disponible de tu {godchildTerm} {studentDisplayName}.</p>
         <p><a href="{historyUrl}">Ver historial de documentos</a></p>
         """;

    public static string BuildNotificationHtml(DocumentShareNotificationDataModel share, string historyUrl)
    {
        var sponsorSalutation = GetSponsorSalutation(share.SponsorNickName, share.SponsorFirstName);
        var godchildTerm = GetGodchildTerm(share.StudentGender);
        var studentName = GetStudentDisplayName(
            share.StudentFirstName, share.StudentLastName, share.StudentNickName);

        return BuildBodyHtml(sponsorSalutation, godchildTerm, studentName, historyUrl);
    }
}
