using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Users.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Users.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Users.Input;

/// <summary>
/// These tests cover:
/// - All fields mapped directly.
/// - Conditional mapping for UserNickName and UserPhoneNumber (only mapped if not null/whitespace).
/// - Edge cases for empty or whitespace values.
/// </summary>
public class UpdateUserInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_Correctly_When_All_Values_Present()
    {
        var input = new UpdateUserInputModel(
            UserId: 42,
            UserFirstName: "Alice",
            UserLastName: "Smith",
            UserNickName: "Ally",
            Gender: Gender.Female,
            UserEmail: "alice@example.com",
            UserPhoneNumber: "555-1234",
            UpdatedById: 99
        );

        var result = input.Adapt<UpdateUserInputDataModel>(Config);

        result.UserId.Should().Be("42");
        result.UserFirstName.Should().Be("Alice");
        result.UserLastName.Should().Be("Smith");
        result.UserNickName.Should().Be("Ally");
        result.Gender.Should().Be(Gender.Female);
        result.UserEmail.Should().Be("alice@example.com");
        result.UserPhoneNumber.Should().Be("555-1234");
        result.UpdatedById.Should().Be(99);
    }

    [Fact]
    public void InputModel_UserFirstName_MustBeNonEmpty()
    {
        var input = new UpdateUserInputModel(
            UserId: 42,
            UserFirstName: string.Empty,
            UserLastName: "Smith",
            UserNickName: "Ally",
            Gender: Gender.Female,
            UserEmail: "alice@example.com",
            UserPhoneNumber: "555-1234",
            UpdatedById: 99
        );

        var result = () => input.Adapt<UpdateUserInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_UserFirstName_IsNormalized()
    {
        var input = new UpdateUserInputModel(
            UserId: 42,
            UserFirstName: "  aliCe  ",
            UserLastName: "Smith",
            UserNickName: "Ally",
            Gender: Gender.Female,
            UserEmail: "alice@example.com",
            UserPhoneNumber: "555-1234",
            UpdatedById: 99
        );

        var result = input.Adapt<UpdateUserInputDataModel>(Config);

        result.UserFirstName.Should().Be("Alice");
    }

    [Fact]
    public void InputModel_UserLastName_MustBeNonEmpty()
    {
        var input = new UpdateUserInputModel(
            UserId: 42,
            UserFirstName: "Alice",
            UserLastName: string.Empty,
            UserNickName: "Ally",
            Gender: Gender.Female,
            UserEmail: "alice@example.com",
            UserPhoneNumber: "555-1234",
            UpdatedById: 99
        );

        var result = () => input.Adapt<UpdateUserInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_UserLastName_IsNormalized()
    {
        var input = new UpdateUserInputModel(
            UserId: 42,
            UserFirstName: "Alice",
            UserLastName: "  smiTh   ",
            UserNickName: "Ally",
            Gender: Gender.Female,
            UserEmail: "alice@example.com",
            UserPhoneNumber: "555-1234",
            UpdatedById: 99
        );

        var result = input.Adapt<UpdateUserInputDataModel>(Config);

        result.UserLastName.Should().Be("Smith");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_UserNickName_When_Null_Or_Whitespace(string nickName)
    {
        var input = new UpdateUserInputModel(
            UserId: 1,
            UserFirstName: "A",
            UserLastName: "B",
            UserNickName: nickName,
            Gender: Gender.Unknown,
            UserEmail: "a@b.com",
            UserPhoneNumber: "123",
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateUserInputDataModel>(Config);

        result.UserNickName.Should().BeNull();
    }

    [Fact]
    public void InputModel_UserNickName_IsNormalized()
    {
        var input = new UpdateUserInputModel(
            UserId: 1,
            UserFirstName: "A",
            UserLastName: "B",
            UserNickName: "  usEr  nickNAMe   ",
            Gender: Gender.Unknown,
            UserEmail: "a@b.com",
            UserPhoneNumber: "123",
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateUserInputDataModel>(Config);

        result.UserNickName.Should().Be("User Nickname");
    }

    [Fact]
    public void InputModel_UserEmail_IsTrimmedAndLowered()
    {
        var input = new UpdateUserInputModel(
            UserId: 1,
            UserFirstName: "A",
            UserLastName: "B",
            UserNickName: "Nick",
            Gender: Gender.Unknown,
            UserEmail: "  A@b.Com   ",
            UserPhoneNumber: "555-1234",
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateUserInputDataModel>(Config);

        result.UserEmail.Should().Be("a@b.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_UserPhoneNumber_When_Null_Or_Whitespace(string phone)
    {
        var input = new UpdateUserInputModel(
            UserId: 1,
            UserFirstName: "A",
            UserLastName: "B",
            UserNickName: "Nick",
            Gender: Gender.Unknown,
            UserEmail: "a@b.com",
            UserPhoneNumber: phone,
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateUserInputDataModel>(Config);

        result.UserPhoneNumber.Should().BeNull();
    }

    [Fact]
    public void InputModel_UserPhoneNumber_IsTrimmed()
    {
        var input = new UpdateUserInputModel(
            UserId: 1,
            UserFirstName: "A",
            UserLastName: "B",
            UserNickName: "Nick",
            Gender: Gender.Unknown,
            UserEmail: "a@b.com",
            UserPhoneNumber: "  555-1234   ",
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateUserInputDataModel>(Config);

        result.UserPhoneNumber.Should().Be("555-1234");
    }
}