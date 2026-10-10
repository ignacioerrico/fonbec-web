using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.RecipientMessages;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Services;

namespace Fonbec.Web.Logic.Tests.Services;

public class StudentMessageCopyTests
{
    [Fact]
    public void Format_Female_PutsGenderHeaderBlankLineThenMessage()
    {
        var sent = new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Local);

        var text = StudentMessageCopy.Format(
            isCompany: false,
            senderGender: Gender.Female,
            senderName: "Claudia Romero",
            studentGender: Gender.Female,
            studentFullName: "Camila Escobar",
            sentOnUtc: sent,
            body: "Hola Cami");

        text.Should().Be(
            "De madrina: Claudia Romero\n" +
            "Para becaria: Camila Escobar\n" +
            "Enviado: 9 de octubre de 2026\n" +
            "\n" +
            "Hola Cami");
    }

    [Fact]
    public void Format_Male_UsesPadrinoAndBecario()
    {
        var text = StudentMessageCopy.Format(
            false, Gender.Male, "Luis Gómez", Gender.Male, "Juan García",
            new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Local), "hola");

        text.Should().StartWith("De padrino: Luis Gómez\nPara becario: Juan García\n");
    }

    [Fact]
    public void Format_Company_SaysEmpresa_AndUnknownGenderUsesBothWords()
    {
        var sent = new DateTime(2026, 10, 9, 15, 0, 0, DateTimeKind.Utc);
        var company = StudentMessageCopy.Format(
            true, null, "Acme SA", Gender.Male, "Juan García", sent, "gracias");
        company.Should().StartWith("De empresa: Acme SA\nPara becario: Juan García\n");
        company.Should().Contain($"Enviado: {sent.ToLocalTime().ToSpanishLongDate()}\n\ngracias");

        var unknown = StudentMessageCopy.Format(
            false, null, "Río", Gender.Unknown, "Alex García", sent, "hola");
        unknown.Should().StartWith("De padrino/madrina: Río\nPara becario/becaria: Alex García\n");
    }

    [Fact]
    public void Format_FromTheQueueRow_MatchesTheClipboard()
    {
        var row = new StudentMessageQueueItemDataModel
        {
            IsCompany = false,
            SenderGender = Gender.Female,
            SenderName = "Claudia Romero",
            StudentGender = Gender.Female,
            StudentFullName = "Camila Escobar",
            SentOn = new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Local),
            Body = "Hola",
        };

        StudentMessageCopy.Format(row).Should().Be(StudentMessageCopy.Format(
            false, Gender.Female, "Claudia Romero", Gender.Female, "Camila Escobar", row.SentOn, "Hola"));
    }
}