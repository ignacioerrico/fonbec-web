using System.Net;
using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Services;

public static class RecipientMessageNotificationFormatter
{
    public static string BuildSubject(string studentFullName)
    {
        var singleLine = string.Join(
            ' ',
            studentFullName.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
        return $"Nuevo mensaje para {singleLine}";
    }

    public static string FamiliarName(string firstName, string? nickName) =>
        string.IsNullOrWhiteSpace(nickName) ? firstName.Trim() : nickName.Trim();

    public static string BuildHtml(
        string studentFullName,
        Gender studentGender,
        string studentFirstName,
        string? studentNickName,
        bool isCompany,
        Gender? senderGender,
        string senderName,
        string body,
        string messagesUrl)
    {
        var encodedStudent = WebUtility.HtmlEncode(studentFullName);
        var encodedSender = WebUtility.HtmlEncode(senderName);
        var encodedFamiliarName = WebUtility.HtmlEncode(FamiliarName(studentFirstName, studentNickName));
        var encodedBody = WebUtility.HtmlEncode(body)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal);
        var encodedUrl = WebUtility.HtmlEncode(messagesUrl);
        var studentPhrase = GenderWords.BecarioWithArticle(studentGender);
        var senderPhrase = isCompany ? "la empresa" : GenderWords.SuPadrino(senderGender);

        return $"""
            <p>Hay un mensaje nuevo para {studentPhrase} <strong>{encodedStudent}</strong> de {senderPhrase} <strong>{encodedSender}</strong>:</p>
            <blockquote>{encodedBody}</blockquote>
            <p>Hacele llegar el mensaje a <strong>{encodedFamiliarName}</strong> y <strong>marcalo como compartido</strong> en FONBEC Web.</p>
            <p><a href="{encodedUrl}">Ver todos los mensajes para becarios</a></p>
            """;
    }
}