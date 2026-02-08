using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Sponsors.Input;

public class CreateSponsorInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_Correctly_When_All_Values_Present()
    {
        var input = new CreateSponsorInputModel(
            ChapterId: 1,
            SponsorFirstName: "Jane",
            SponsorLastName: "Smith",
            SponsorNickName: "JS",
            SponsorGender: Gender.Female,
            SponsorPhoneNumber: "555-1234",
            SponsorNotes: "Some notes",
            SponsorEmail: "jane@x.com",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorInputDataModel>(Config);

        result.ChapterId.Should().Be(1);
        result.SponsorFirstName.Should().Be("Jane");
        result.SponsorLastName.Should().Be("Smith");
        result.SponsorNickName.Should().Be("Js");
        result.SponsorGender.Should().Be(Gender.Female);
        result.SponsorPhoneNumber.Should().Be("555-1234");
        result.SponsorNotes.Should().Be("Some notes");
        result.SponsorEmail.Should().Be("jane@x.com");
        result.CreatedById.Should().Be(99);
    }

    [Fact]
    public void InputModel_SponsorFirstName_MustBeNonEmpty()
    {
        var input = new CreateSponsorInputModel(
            ChapterId: 1,
            SponsorFirstName: string.Empty,
            SponsorLastName: "Smith",
            SponsorNickName: "JS",
            SponsorGender: Gender.Female,
            SponsorPhoneNumber: "555-1234",
            SponsorNotes: "Some notes",
            SponsorEmail: "jane@x.com",
            CreatedById: 99
        );

        var result = () => input.Adapt<CreateSponsorInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_SponsorFirstName_IsNormalized()
    {
        var input = new CreateSponsorInputModel(
            ChapterId: 1,
            SponsorFirstName: "  sponSor fiRSt naMe   ",
            SponsorLastName: "Smith",
            SponsorNickName: "JS",
            SponsorGender: Gender.Female,
            SponsorPhoneNumber: "555-1234",
            SponsorNotes: "Some notes",
            SponsorEmail: "jane@x.com",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorInputDataModel>(Config);

        result.SponsorFirstName.Should().Be("Sponsor First Name");
    }

    [Fact]
    public void InputModel_SponsorLastName_MustBeNonEmpty()
    {
        var input = new CreateSponsorInputModel(
            ChapterId: 1,
            SponsorFirstName: "Jane",
            SponsorLastName: string.Empty,
            SponsorNickName: "JS",
            SponsorGender: Gender.Female,
            SponsorPhoneNumber: "555-1234",
            SponsorNotes: "Some notes",
            SponsorEmail: "jane@x.com",
            CreatedById: 99
        );

        var result = () => input.Adapt<CreateSponsorInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_SponsorLastName_IsNormalized()
    {
        var input = new CreateSponsorInputModel(
            ChapterId: 1,
            SponsorFirstName: "Jane",
            SponsorLastName: "  sponSor laSt naMe   ",
            SponsorNickName: "JS",
            SponsorGender: Gender.Female,
            SponsorPhoneNumber: "555-1234",
            SponsorNotes: "Some notes",
            SponsorEmail: "jane@x.com",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorInputDataModel>(Config);

        result.SponsorLastName.Should().Be("Sponsor Last Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_SponsorNickName_When_Empty_Or_Whitespace(string nickName)
    {
        var input = new CreateSponsorInputModel(
            ChapterId: 1,
            SponsorFirstName: "Jane",
            SponsorLastName: "Smith",
            SponsorNickName: nickName,
            SponsorGender: Gender.Female,
            SponsorPhoneNumber: "555-1234",
            SponsorNotes: "Some notes",
            SponsorEmail: "jane@x.com",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorInputDataModel>(Config);

        result.SponsorNickName.Should().BeNull();
    }

    [Fact]
    public void InputModel_SponsorNickName_IsNormalized()
    {
        var input = new CreateSponsorInputModel(
            ChapterId: 1,
            SponsorFirstName: "Jane",
            SponsorLastName: "Smith",
            SponsorNickName: "  sponSor  niCKnaMe   ",
            SponsorGender: Gender.Female,
            SponsorPhoneNumber: "555-1234",
            SponsorNotes: "Some notes",
            SponsorEmail: "jane@x.com",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorInputDataModel>(Config);

        result.SponsorNickName.Should().Be("Sponsor Nickname");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void InputModel_SponsorEmail_MustBeNonEmpty(string email)
    {
        var input = new CreateSponsorInputModel(
            ChapterId: 1,
            SponsorFirstName: "Jane",
            SponsorLastName: "Smith",
            SponsorNickName: "JS",
            SponsorGender: Gender.Female,
            SponsorPhoneNumber: "555-1234",
            SponsorNotes: "Some notes",
            SponsorEmail: email,
            CreatedById: 99
        );

        var result = () => input.Adapt<CreateSponsorInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_StudentEmail_IsTrimmedAndLowered()
    {
        var input = new CreateSponsorInputModel(
            ChapterId: 1,
            SponsorFirstName: "Jane",
            SponsorLastName: "Smith",
            SponsorNickName: "JS",
            SponsorGender: Gender.Female,
            SponsorPhoneNumber: "555-1234",
            SponsorNotes: "Some notes",
            SponsorEmail: "  jaNE@x.Com   ",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorInputDataModel>(Config);

        result.SponsorEmail.Should().Be("jane@x.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_SponsorPhone_When_Empty_Or_Whitespace(string phone)
    {
        var input = new CreateSponsorInputModel(
            ChapterId: 1,
            SponsorFirstName: "Jane",
            SponsorLastName: "Smith",
            SponsorNickName: "JS",
            SponsorGender: Gender.Female,
            SponsorPhoneNumber: phone,
            SponsorNotes: "Some notes",
            SponsorEmail: "jane@x.com",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorInputDataModel>(Config);

        result.SponsorPhoneNumber.Should().BeNull();
    }

    [Fact]
    public void InputModel_SponsorPhone_IsTrimmed()
    {
        var input = new CreateSponsorInputModel(
            ChapterId: 1,
            SponsorFirstName: "Jane",
            SponsorLastName: "Smith",
            SponsorNickName: "JS",
            SponsorGender: Gender.Female,
            SponsorPhoneNumber: "  555-1234   ",
            SponsorNotes: "Some notes",
            SponsorEmail: "jane@x.com",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorInputDataModel>(Config);

        result.SponsorPhoneNumber.Should().Be("555-1234");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_SponsorNotes_When_Empty_Or_Whitespace(string notes)
    {
        var input = new CreateSponsorInputModel(
            ChapterId: 1,
            SponsorFirstName: "Jane",
            SponsorLastName: "Smith",
            SponsorNickName: "JS",
            SponsorGender: Gender.Female,
            SponsorPhoneNumber: "555-1234",
            SponsorNotes: notes,
            SponsorEmail: "jane@x.com",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorInputDataModel>(Config);

        result.SponsorNotes.Should().BeNull();
    }

    [Fact]
    public void InputModel_SponsorNotes_IsTrimmed()
    {
        var input = new CreateSponsorInputModel(
            ChapterId: 1,
            SponsorFirstName: "Jane",
            SponsorLastName: "Smith",
            SponsorNickName: "JS",
            SponsorGender: Gender.Female,
            SponsorPhoneNumber: "555-1234",
            SponsorNotes: "  Some noTEs   ",
            SponsorEmail: "jane@x.com",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorInputDataModel>(Config);

        result.SponsorNotes.Should().Be("Some noTEs");
    }
}