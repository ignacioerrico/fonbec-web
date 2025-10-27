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
            UserNotes: "Some personal notes",
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );
        var result = input.BuildAdapter(Config)
            .AddParameters("generatedPassword", "secret")
            .AdaptToType<CreateUserInputDataModel>();

        result.UserFirstName.Should().Be("John");
        result.UserLastName.Should().Be("Doe");
        result.UserNickName.Should().Be("Jd");
        result.UserGender.Should().Be(Gender.Male);
        result.UserEmail.Should().Be("john@email.com");
        result.UserPhoneNumber.Should().Be("555-1234");
        result.UserNotes.Should().Be("Some personal notes");
        result.UserRole.Should().Be("Admin");
        result.CreatedById.Should().Be(99);
        result.UserChapterId.Should().Be(5);
        result.GeneratedPassword.Should().Be("secret");
    }

    [Fact]
    public void InputModel_UserFirstName_MustBeNonEmpty()
    {
        var input = new CreateUserInputModel(
            UserFirstName: string.Empty,
            UserLastName: "Doe",
            UserNickName: "JD",
            UserGender: Gender.Male,
            UserEmail: "John@Email.com ",
            UserPhoneNumber: " 555-1234 ",
            UserNotes: "Some personal notes",
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );

        var result = () => input.Adapt<CreateUserInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_UserFirstName_IsNormalized()
    {
        var input = new CreateUserInputModel(
            UserFirstName: "  jOhn   ",
            UserLastName: "Doe",
            UserNickName: "JD",
            UserGender: Gender.Male,
            UserEmail: "John@Email.com ",
            UserPhoneNumber: " 555-1234 ",
            UserNotes: "Some personal notes",
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );

        var result = input.Adapt<CreateUserInputDataModel>(Config);

        result.UserFirstName.Should().Be("John");
    }

    [Fact]
    public void InputModel_UserLastName_MustBeNonEmpty()
    {
        var input = new CreateUserInputModel(
            UserFirstName: "John",
            UserLastName: string.Empty,
            UserNickName: "JD",
            UserGender: Gender.Male,
            UserEmail: "John@Email.com ",
            UserPhoneNumber: " 555-1234 ",
            UserNotes: "Some personal notes",
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );

        var result = () => input.Adapt<CreateUserInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_UserLastName_IsNormalized()
    {
        var input = new CreateUserInputModel(
            UserFirstName: "John",
            UserLastName: "  dOE  ",
            UserNickName: "JD",
            UserGender: Gender.Male,
            UserEmail: "John@Email.com ",
            UserPhoneNumber: " 555-1234 ",
            UserNotes: "Some personal notes",
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );

        var result = input.Adapt<CreateUserInputDataModel>(Config);

        result.UserLastName.Should().Be("Doe");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_UserNickName_When_Null_Or_Whitespace(string nickName)
    {
        var input = new CreateUserInputModel(
            UserFirstName: "John",
            UserLastName: "Doe",
            UserNickName: nickName,
            UserGender: Gender.Male,
            UserEmail: "john@email.com",
            UserPhoneNumber: "555-1234",
            UserNotes: "Some personal notes",
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );
        var result = input.BuildAdapter(Config)
            .AddParameters("generatedPassword", "secret")
            .AdaptToType<CreateUserInputDataModel>();

        result.UserNickName.Should().BeNull();
    }

    [Fact]
    public void InputModel_UserNickName_IsNormalized()
    {
        var input = new CreateUserInputModel(
            UserFirstName: "John",
            UserLastName: "Doe",
            UserNickName: "  jD  ",
            UserGender: Gender.Male,
            UserEmail: "john@email.com",
            UserPhoneNumber: "555-1234",
            UserNotes: "Some personal notes",
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );

        var result = input.Adapt<CreateUserInputDataModel>(Config);

        result.UserNickName.Should().Be("Jd");
    }

    [Fact]
    public void InputModel_UserEmail_IsTrimmedAndLowered()
    {
        var input = new CreateUserInputModel(
            UserFirstName: "John",
            UserLastName: "Doe",
            UserNickName: "JD",
            UserGender: Gender.Male,
            UserEmail: "  JOHN@emaIl.Com   ",
            UserPhoneNumber: "555-1234",
            UserNotes: "Some personal notes",
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );

        var result = input.Adapt<CreateUserInputDataModel>(Config);

        result.UserEmail.Should().Be("john@email.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Does_Not_Map_UserPhoneNumber_When_Null_Or_Whitespace(string phone)
    {
        var input = new CreateUserInputModel(
            UserFirstName: "John",
            UserLastName: "Doe",
            UserNickName: "JD",
            UserGender: Gender.Male,
            UserEmail: "john@email.com",
            UserPhoneNumber: phone,
            UserNotes: "Some personal notes",
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
    public void InputModel_UserPhoneNumber_IsTrimmed()
    {
        var input = new CreateUserInputModel(
            UserFirstName: "John",
            UserLastName: "Doe",
            UserNickName: "JD",
            UserGender: Gender.Male,
            UserEmail: "john@email.com",
            UserPhoneNumber: "  555-1234   ",
            UserNotes: "Some personal notes",
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );

        var result = input.Adapt<CreateUserInputDataModel>(Config);

        result.UserPhoneNumber.Should().Be("555-1234");
    }

    [Fact]
    public void Maps_InputModel_UserNotes_To_Trimmed_InputDataModel()
    {
        var input = new CreateUserInputModel(
            UserFirstName: "John",
            UserLastName: "Doe",
            UserNickName: "JD",
            UserGender: Gender.Male,
            UserEmail: "john@email.com",
            UserPhoneNumber: "555-1234",
            UserNotes: "  Some personal notes   ",
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );

        var result = input.Adapt<CreateUserInputDataModel>(Config);

        result.UserNotes.Should().Be("Some personal notes");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("  \t \n \r ")]
    public void Maps_EmptyOrWhitespace_InputModel_UserNotes_To_InputDataModel_Null(string userNotes)
    {
        var input = new CreateUserInputModel(
            UserFirstName: "John",
            UserLastName: "Doe",
            UserNickName: "JD",
            UserGender: Gender.Male,
            UserEmail: "john@email.com",
            UserPhoneNumber: "555-1234",
            UserNotes: userNotes,
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );

        var result = input.Adapt<CreateUserInputDataModel>(Config);

        result.UserNotes.Should().BeNull();
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
            UserNotes: "Some personal notes",
            UserRole: "Admin",
            CreatedById: 99,
            UserChapterId: 5
        );
        var result = input.Adapt<CreateUserInputDataModel>(Config);

        result.GeneratedPassword.Should().BeNull();
    }
}