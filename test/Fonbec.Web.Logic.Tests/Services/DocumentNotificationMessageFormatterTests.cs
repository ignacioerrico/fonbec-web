using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Services;

namespace Fonbec.Web.Logic.Tests.Services;

public class DocumentNotificationMessageFormatterTests
{
    [Theory]
    [InlineData("Juancito", "Juan", "Juancito")]
    [InlineData(null, "Juan", "Juan")]
    [InlineData("", "Juan", "Juan")]
    [InlineData("   ", "Juan", "Juan")]
    public void GetSponsorSalutation_Uses_Nickname_When_Available(string? nickName, string firstName, string expected)
    {
        DocumentNotificationMessageFormatter.GetSponsorSalutation(nickName, firstName)
            .Should().Be(expected);
    }

    [Theory]
    [InlineData(Gender.Male, "ahijado")]
    [InlineData(Gender.Female, "ahijada")]
    [InlineData(Gender.Unknown, "ahijado/a")]
    public void GetGodchildTerm_Uses_Gender(Gender gender, string expected)
    {
        DocumentNotificationMessageFormatter.GetGodchildTerm(gender).Should().Be(expected);
    }

    [Theory]
    [InlineData("María", "García", "Mari", "Mari García")]
    [InlineData("María", "García", null, "María García")]
    [InlineData("María", "García", "", "María García")]
    [InlineData("María", "García", "   ", "María García")]
    public void GetStudentDisplayName_Always_Includes_LastName(
        string firstName, string lastName, string? nickName, string expected)
    {
        DocumentNotificationMessageFormatter.GetStudentDisplayName(firstName, lastName, nickName)
            .Should().Be(expected);
    }

    [Fact]
    public void BuildBodyHtml_Includes_Salutation_GodchildTerm_StudentName_And_Link()
    {
        var html = DocumentNotificationMessageFormatter.BuildBodyHtml(
            "Juancito",
            "ahijada",
            "Mari García",
            "https://fonbec.test/padrinos/token/1");

        html.Should().Contain("Hola, Juancito:");
        html.Should().Contain("de tu ahijada Mari García.");
        html.Should().Contain("https://fonbec.test/padrinos/token/1");
        html.Should().Contain("Ver historial de documentos");
    }

    [Fact]
    public void BuildNotificationHtml_Falls_Back_To_First_Names_When_Nicknames_Missing()
    {
        var share = new DocumentShareNotificationDataModel
        {
            SponsorFirstName = "Juan",
            StudentFirstName = "María",
            StudentLastName = "García",
            StudentGender = Gender.Male,
        };

        var html = DocumentNotificationMessageFormatter.BuildNotificationHtml(
            share, "https://fonbec.test/padrinos/token/1");

        html.Should().Contain("Hola, Juan:");
        html.Should().Contain("de tu ahijado María García.");
    }
}