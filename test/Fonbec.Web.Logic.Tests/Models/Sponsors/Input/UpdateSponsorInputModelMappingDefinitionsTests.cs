using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Sponsors.Input;

public class UpdateSponsorInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_Correctly_When_All_Values_Present()
    {
        var input = new UpdateSponsorInputModel(
            SponsorId: 1,
            SponsorFirstName: "John",
            SponsorLastName: "Doe",
            SponsorNickName: "Johnny",
            SponsorGender: Gender.Male,
            SponsorPhoneNumber: "555-1234",
            SponsorEmail: "john@example.com",
            SponsorCompanyId: 10,
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateSponsorInputDataModel>(Config);

        result.SponsorId.Should().Be(1);
        result.SponsorFirstName.Should().Be("John");
        result.SponsorLastName.Should().Be("Doe");
        result.SponsorNickName.Should().Be("Johnny");
        result.SponsorGender.Should().Be(Gender.Male);
        result.SponsorPhoneNumber.Should().Be("555-1234");
        result.SponsorEmail.Should().Be("john@example.com");
        result.UpdatedById.Should().Be(2);
        result.SponsorCompanyId.Should().Be(10);
    }

    [Fact]
    public void InputModel_SponsorFirstName_MustBeNonEmpty()
    {
        var input = new UpdateSponsorInputModel(
            SponsorId: 1,
            SponsorFirstName: string.Empty,
            SponsorLastName: "Doe",
            SponsorNickName: "Johnny",
            SponsorGender: Gender.Male,
            SponsorPhoneNumber: "555-1234",
            SponsorEmail: "john@example.com",
            SponsorCompanyId: 10,
            UpdatedById: 2
        );

        var result = () => input.Adapt<UpdateSponsorInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_SponsorFirstName_IsNormalized()
    {
        var input = new UpdateSponsorInputModel(
            SponsorId: 1,
            SponsorFirstName: "  jOHn   ",
            SponsorLastName: "Doe",
            SponsorNickName: "Johnny",
            SponsorGender: Gender.Male,
            SponsorPhoneNumber: "555-1234",
            SponsorEmail: "john@example.com",
            SponsorCompanyId: 10,
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateSponsorInputDataModel>(Config);

        result.SponsorFirstName.Should().Be("John");
    }

    [Fact]
    public void InputModel_SponsorLastName_MustBeNonEmpty()
    {
        var input = new UpdateSponsorInputModel(
            SponsorId: 1,
            SponsorFirstName: "John",
            SponsorLastName: string.Empty,
            SponsorNickName: "Johnny",
            SponsorGender: Gender.Male,
            SponsorPhoneNumber: "555-1234",
            SponsorEmail: "john@example.com",
            SponsorCompanyId: 10,
            UpdatedById: 2
        );

        var result = () => input.Adapt<UpdateSponsorInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_SponsorLastName_IsNormalized()
    {
        var input = new UpdateSponsorInputModel(
            SponsorId: 1,
            SponsorFirstName: "John",
            SponsorLastName: "  dOE   ",
            SponsorNickName: "Johnny",
            SponsorGender: Gender.Male,
            SponsorPhoneNumber: "555-1234",
            SponsorEmail: "john@example.com",
            SponsorCompanyId: 10,
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateSponsorInputDataModel>(Config);

        result.SponsorLastName.Should().Be("Doe");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_SponsorNickName_When_Null_Or_Whitespace(string nickName)
    {
        var input = new UpdateSponsorInputModel(
            SponsorId: 1,
            SponsorFirstName: "John",
            SponsorLastName: "Doe",
            SponsorNickName: nickName,
            SponsorGender: Gender.Male,
            SponsorPhoneNumber: "555-1234",
            SponsorEmail: "john@example.com",
            SponsorCompanyId: 10,
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateSponsorInputDataModel>(Config);

        result.SponsorNickName.Should().BeNull();
    }

    [Fact]
    public void InputModel_SponsorNickName_IsNormalized()
    {
        var input = new UpdateSponsorInputModel(
            SponsorId: 1,
            SponsorFirstName: "John",
            SponsorLastName: "Doe",
            SponsorNickName: "  jOHnNy   ",
            SponsorGender: Gender.Male,
            SponsorPhoneNumber: "555-1234",
            SponsorEmail: "john@example.com",
            SponsorCompanyId: 10,
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateSponsorInputDataModel>(Config);

        result.SponsorNickName.Should().Be("Johnny");
    }

    [Fact]
    public void Maps_SponsorGender_Correctly()
    {
        var input = new UpdateSponsorInputModel(
            SponsorId: 1,
            SponsorFirstName: "John",
            SponsorLastName: "Doe",
            SponsorNickName: "Johnny",
            SponsorGender: Gender.Female,
            SponsorPhoneNumber: "555-1234",
            SponsorEmail: "john@example.com",
            SponsorCompanyId: 10,
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateSponsorInputDataModel>(Config);

        result.SponsorGender.Should().Be(Gender.Female);
    }

    [Fact]
    public void InputModel_SponsorEmail_IsTrimmedAndLowered()
    {
        var input = new UpdateSponsorInputModel(
            SponsorId: 1,
            SponsorFirstName: "John",
            SponsorLastName: "Doe",
            SponsorNickName: "Johnny",
            SponsorGender: Gender.Male,
            SponsorPhoneNumber: "555-1234",
            SponsorEmail: "  JOHn@Example.Com   ",
            SponsorCompanyId: 10,
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateSponsorInputDataModel>(Config);

        result.SponsorEmail.Should().Be("john@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_SponsorPhoneNumber_When_Empty_Or_Whitespace(string phone)
    {
        var input = new UpdateSponsorInputModel(
            SponsorId: 1,
            SponsorFirstName: "John",
            SponsorLastName: "Doe",
            SponsorNickName: "Johnny",
            SponsorGender: Gender.Male,
            SponsorPhoneNumber: phone,
            SponsorEmail: "john@example.com",
            SponsorCompanyId: 10,
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateSponsorInputDataModel>(Config);

        result.SponsorPhoneNumber.Should().BeNull();
    }

    [Fact]
    public void InputModel_SponsorPhoneNumber_IsTrimmed()
    {
        var input = new UpdateSponsorInputModel(
            SponsorId: 1,
            SponsorFirstName: "John",
            SponsorLastName: "Doe",
            SponsorNickName: "Johnny",
            SponsorGender: Gender.Male,
            SponsorPhoneNumber: "  555-1234   ",
            SponsorEmail: "john@example.com",
            SponsorCompanyId: 10,
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateSponsorInputDataModel>(Config);

        result.SponsorPhoneNumber.Should().Be("555-1234");
    }

    [Fact]
    public void InputModel_UpdatedById_IsMapped()
    {
        var input = new UpdateSponsorInputModel(
            SponsorId: 1,
            SponsorFirstName: "John",
            SponsorLastName: "Doe",
            SponsorNickName: "Johnny",
            SponsorGender: Gender.Male,
            SponsorPhoneNumber: "555-1234",
            SponsorEmail: "john@example.com",
            SponsorCompanyId: 10,
            UpdatedById: 99
        );

        var result = input.Adapt<UpdateSponsorInputDataModel>(Config);

        result.UpdatedById.Should().Be(99);
    }

    [Fact]
    public void Does_Not_Map_SponsorCompanyId_When_Null()
    {
        var input = new UpdateSponsorInputModel(
            SponsorId: 1,
            SponsorFirstName: "John",
            SponsorLastName: "Doe",
            SponsorNickName: "Johnny",
            SponsorGender: Gender.Male,
            SponsorPhoneNumber: "555-1234",
            SponsorEmail: "john@example.com",
            SponsorCompanyId: null,
            UpdatedById: 99
        );

        var result = input.Adapt<UpdateSponsorInputDataModel>(Config);

        result.SponsorCompanyId.Should().BeNull();
    }
}