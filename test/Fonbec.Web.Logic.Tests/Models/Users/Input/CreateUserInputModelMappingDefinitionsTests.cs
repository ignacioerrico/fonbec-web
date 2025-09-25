using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Users.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Users.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Users.Input;

public class CreateUserInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_Correctly_When_All_Values_Present()
    {
        var input = new CreateUserInputModel(
            UserFirstName: "John",
            UserLastName: "Doe",
            UserNickName: "JD",
            UserGender: Gender.Male,
            UserEmail: "John@Email.com ",
            UserPhoneNumber: " 555-1234 ",
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );
        var result = input.BuildAdapter(Config)
            .AddParameters("generatedPassword", "secret")
            .AdaptToType<CreateUserInputDataModel>();

        result.UserFirstName.Should().Be("John");
        result.UserLastName.Should().Be("Doe");
        result.UserNickName.Should().Be("JD");
        result.UserGender.Should().Be(Gender.Male);
        result.UserEmail.Should().Be("john@email.com");
        result.UserPhoneNumber.Should().Be("555-1234");
        result.UserRole.Should().Be("Admin");
        result.CreatedById.Should().Be(99);
        result.UserChapterId.Should().Be(5);
        result.GeneratedPassword.Should().Be("secret");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_UserNickName_When_Null_Or_Whitespace(string? nickName)
    {
        var input = new CreateUserInputModel(
            UserFirstName: "John",
            UserLastName: "Doe",
            UserNickName: nickName!,
            UserGender: Gender.Male,
            UserEmail: "john@email.com",
            UserPhoneNumber: "555-1234",
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );
        var result = input.BuildAdapter(Config)
            .AddParameters("generatedPassword", "secret")
            .AdaptToType<CreateUserInputDataModel>();

        result.UserNickName.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_UserPhoneNumber_When_Null_Or_Whitespace(string? phone)
    {
        var input = new CreateUserInputModel(
            UserFirstName: "John",
            UserLastName: "Doe",
            UserNickName: "JD",
            UserGender: Gender.Male,
            UserEmail: "john@email.com",
            UserPhoneNumber: phone!,
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );
        var result = input.BuildAdapter(Config)
            .AddParameters("generatedPassword", "secret")
            .AdaptToType<CreateUserInputDataModel>();

        result.UserPhoneNumber.Should().BeNull();
    }

    [Fact]
    public void Does_Not_Map_GeneratedPassword_If_Not_Provided()
    {
        var input = new CreateUserInputModel(
            UserFirstName: "John",
            UserLastName: "Doe",
            UserNickName: "JD",
            UserGender: Gender.Male,
            UserEmail: "john@email.com",
            UserPhoneNumber: "555-1234",
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );
        var result = input.Adapt<CreateUserInputDataModel>(Config);

        result.GeneratedPassword.Should().BeNull();
    }
}