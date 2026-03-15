using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.Logic.Models.Companies.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Companies.Input;

public class CreatePointOfContactInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_InputModel_To_InputDataModel()
    {
        var input = new CreatePointOfContactInputModel(
            FirstName: "John",
            LastName: "Doe",
            NickName: "Johnny",
            Email: "john.doe@company.com",
            PhoneNumber: "123456789",
            CreatedById: 42
        );

        var result = input.Adapt<CreatePointOfContactInputDataModel>(Config);

        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.NickName.Should().Be("Johnny");
        result.Email.Should().Be("john.doe@company.com");
        result.PhoneNumber.Should().Be("123456789");
        result.CreatedById.Should().Be(42);
    }

    [Fact]
    public void InputModel_FirstName_MustBeNonEmpty()
    {
        var input = new CreatePointOfContactInputModel(
            FirstName: string.Empty,
            LastName: "Doe",
            NickName: "Johnny",
            Email: "john.doe@company.com",
            PhoneNumber: "123456789",
            CreatedById: 42
        );

        var result = () => input.Adapt<CreatePointOfContactInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_LastName_MustBeNonEmpty()
    {
        var input = new CreatePointOfContactInputModel(
            FirstName: "John",
            LastName: string.Empty,
            NickName: "Johnny",
            Email: "john.doe@company.com",
            PhoneNumber: "123456789",
            CreatedById: 42
        );

        var result = () => input.Adapt<CreatePointOfContactInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_FirstName_IsTrimmed()
    {
        var input = new CreatePointOfContactInputModel(
            FirstName: " John  ",
            LastName: "Doe",
            NickName: "Johnny",
            Email: "john.doe@company.com",
            PhoneNumber: "123456789",
            CreatedById: 42
        );
        var result = input.Adapt<CreatePointOfContactInputDataModel>(Config);
        result.FirstName.Should().Be("John");
    }

    [Fact]
    public void InputModel_LastName_IsTrimmed()
    {
        var input = new CreatePointOfContactInputModel(
            FirstName: "John",
            LastName: " Doe  ",
            NickName: "Johnny",
            Email: "john.doe@company.com",
            PhoneNumber: "123456789",
            CreatedById: 42
        );
        var result = input.Adapt<CreatePointOfContactInputDataModel>(Config);

        result.LastName.Should().Be("Doe");
    }

    [Fact]
    public void InputModel_NickName_IsTrimmed()
    {

        var input = new CreatePointOfContactInputModel(
            FirstName: "John",
            LastName: "Doe",
            NickName: " Johnny  ",
            Email: "john.doe@company.com",
            PhoneNumber: "123456789",
            CreatedById: 42
        );

        var result = input.Adapt<CreatePointOfContactInputDataModel>(Config);

        result.NickName.Should().Be("Johnny");
    }

    [Fact]
    public void InputModel_Email_IsTrimmed()
    {
        var input = new CreatePointOfContactInputModel(
            FirstName: "John",
            LastName: "Doe",
            NickName: "Johnny",
            Email: " john.doe@company.com  ",
            PhoneNumber: "123456789",
            CreatedById: 42
        );

        var result = input.Adapt<CreatePointOfContactInputDataModel>(Config);

        result.Email.Should().Be("john.doe@company.com");
    }

    [Fact]
    public void InputModel_PhoneNumber_IsTrimmed()
    {

        var input = new CreatePointOfContactInputModel(
            FirstName: "John",
            LastName: "Doe",
            NickName: "Johnny",
            Email: "john.doe@company.com",
            PhoneNumber: " 123456789  ",
            CreatedById: 42
        );

        var result = input.Adapt<CreatePointOfContactInputDataModel>(Config);

        result.PhoneNumber.Should().Be("123456789");
    }

    [Fact]
    public void InputModel_NickName_Email_Phone_CanBeNull()
    {
        var input = new CreatePointOfContactInputModel(
            FirstName: "John",
            LastName: "Doe",
            NickName: null,
            Email: null,
            PhoneNumber: null,
            CreatedById: 42
        );

        var result = input.Adapt<CreatePointOfContactInputDataModel>(Config);

        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.NickName.Should().BeNull();
        result.Email.Should().BeNull();
        result.PhoneNumber.Should().BeNull();
        result.CreatedById.Should().Be(42);
    }

    [Fact]
    public void InputModel_NickName_Email_Phone_CanBeEmpty()
    {
        var input = new CreatePointOfContactInputModel(
            FirstName: "John",
            LastName: "Doe",
            NickName: string.Empty,
            Email: string.Empty,
            PhoneNumber: string.Empty,
            CreatedById: 42
        );

        var result = input.Adapt<CreatePointOfContactInputDataModel>(Config);

        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.NickName.Should().BeNull();
        result.Email.Should().BeNull();
        result.PhoneNumber.Should().BeNull();
        result.CreatedById.Should().Be(42);
    }
}