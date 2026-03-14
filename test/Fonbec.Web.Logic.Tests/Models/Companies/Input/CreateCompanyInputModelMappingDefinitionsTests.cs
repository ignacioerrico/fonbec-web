using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.Logic.Models.Companies.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Companies.Input;

public class CreateCompanyInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_InputModel_To_InputDataModel()
    {
        var input = new CreateCompanyInputModel(
            CompanyName: "Test Company",
            CompanyEmail: "test@hotmail.com",
            CompanyPhoneNumber: "123456789",
            CompanySponsors: [],
            CreatedById: 42
        );

        var result = input.Adapt<CreateCompanyInputDataModel>(Config);

        result.CompanyName.Should().Be("Test Company");
        result.CompanyEmail.Should().Be("test@hotmail.com");
        result.CompanyPhoneNumber.Should().Be("123456789");
        result.CreatedById.Should().Be(42);
    }

    [Fact]
    public void InputModel_CompanyName_MustBeNonEmpty()
    {
        var input = new CreateCompanyInputModel(
            CompanyName: string.Empty,
            CompanyEmail: "test@gmail.com",
            CompanyPhoneNumber: "123456789",
            CompanySponsors: [],
            CreatedById: 42

        );

        var result = () => input.Adapt<CreateCompanyInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_CompanyEmail_IsTrimmed()
    {
        var input = new CreateCompanyInputModel(
            CompanyName: "Test Company",
            CompanyEmail:" test@gmail.com  ",
            CompanyPhoneNumber: "123456789",
            CompanySponsors: [],
            CreatedById: 42
        );

        var result = input.Adapt<CreateCompanyInputDataModel>(Config);

        result.CompanyEmail.Should().Be("test@gmail.com");
    }

    [Fact]
    public void InputModel_CompanyEmailAndPhone_CanBeEmpty()
    {
        var input = new CreateCompanyInputModel(
            CompanyName: "Test Company",
            CompanyEmail: string.Empty,
            CompanyPhoneNumber: string.Empty,
            CompanySponsors: [],
            CreatedById: 42
        );

        var result = input.Adapt<CreateCompanyInputDataModel>(Config);

        result.CompanyEmail.Should().BeNull();
        result.CompanyPhoneNumber.Should().BeNull();
    }
}

