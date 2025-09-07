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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_UserNickName_When_Null_Or_Whitespace(string? nickName)
    {
        var input = new UpdateUserInputModel(
            UserId: 1,
            UserFirstName: "A",
            UserLastName: "B",
            UserNickName: nickName!,
            Gender: Gender.Unknown,
            UserEmail: "a@b.com",
            UserPhoneNumber: "123",
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateUserInputDataModel>(Config);

        result.UserNickName.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_UserPhoneNumber_When_Null_Or_Whitespace(string? phone)
    {
        var input = new UpdateUserInputModel(
            UserId: 1,
            UserFirstName: "A",
            UserLastName: "B",
            UserNickName: "Nick",
            Gender: Gender.Unknown,
            UserEmail: "a@b.com",
            UserPhoneNumber: phone!,
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateUserInputDataModel>(Config);

        result.UserPhoneNumber.Should().BeNull();
    }

    [Fact]
    public void Maps_UserNickName_And_UserPhoneNumber_When_Not_Whitespace()
    {
        var input = new UpdateUserInputModel(
            UserId: 1,
            UserFirstName: "A",
            UserLastName: "B",
            UserNickName: "Nick",
            Gender: Gender.Unknown,
            UserEmail: "a@b.com",
            UserPhoneNumber: "12345",
            UpdatedById: 2
        );

        var result = input.Adapt<UpdateUserInputDataModel>(Config);

        result.UserNickName.Should().Be("Nick");
        result.UserPhoneNumber.Should().Be("12345");
    }
}