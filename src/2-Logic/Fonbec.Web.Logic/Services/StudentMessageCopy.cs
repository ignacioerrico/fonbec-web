using Fonbec.Web.DataAccess.DataModels.RecipientMessages;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;

namespace Fonbec.Web.Logic.Services;

public static class StudentMessageCopy
{
    public static string Format(StudentMessageQueueItemDataModel row) =>
        Format(
            row.IsCompany,
            row.SenderGender,
            row.SenderName,
            row.StudentGender,
            row.StudentFullName,
            row.SentOn,
            row.Body);

    public static string Format(
        bool isCompany,
        Gender? senderGender,
        string senderName,
        Gender studentGender,
        string studentFullName,
        DateTime sentOnUtc,
        string body)
    {
        var senderRole = isCompany ? "empresa" : GenderWords.Padrino(senderGender);
        var studentRole = GenderWords.Becario(studentGender);
        var sentOn = sentOnUtc.ToLocalTime().ToSpanishLongDate();

        return $"De {senderRole}: {senderName}\nPara {studentRole}: {studentFullName}\nEnviado: {sentOn}\n\n{body}";
    }
}