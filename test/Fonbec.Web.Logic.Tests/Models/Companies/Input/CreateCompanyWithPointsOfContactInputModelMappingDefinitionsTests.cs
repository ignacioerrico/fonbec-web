using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.Logic.Models.Companies.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Companies.Input;

public class CreateCompanyWithPointsOfContactInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_InputModel_To_InputDataModel()
    {
        var pointsOfContact = new List<CreatePointOfContactInputModel>
        {
            new(
                FirstName: "John",
                LastName: "Doe",
                NickName: "Johnny",
                Email: "john.doe@company.com",
                PhoneNumber: "123456789",
                CreatedById: 42
            ),
            new(
                FirstName: "Jane",
                LastName: "Smith",
                NickName: "Janey",
                Email: "jane.smith@company.com",
                PhoneNumber: "987654321",
                CreatedById: 42
            )
        };

        var input = new CreateCompanyWithPointsOfContactInputModel(
            CompanyName: "Test Company",
            CompanyEmail: "test@company.com",
            CompanyPhoneNumber: "555-1234",
            CreatedById: 42,
            PointsOfContact: pointsOfContact
        );

        var result = input.Adapt<CreateCompanyWithPointsOfContactInputDataModel>(Config);

        result.CompanyName.Should().Be("Test Company");
        result.CompanyEmail.Should().Be("test@company.com");
        result.CompanyPhoneNumber.Should().Be("555-1234");
        result.CreatedById.Should().Be(42);

        result.PointsOfContact.Should().HaveCount(2);

        var firstPoc = result.PointsOfContact[0];
        firstPoc.FirstName.Should().Be("John");
        firstPoc.LastName.Should().Be("Doe");
        firstPoc.NickName.Should().Be("Johnny");
        firstPoc.Email.Should().Be("john.doe@company.com");
        firstPoc.PhoneNumber.Should().Be("123456789");
        firstPoc.CreatedById.Should().Be(42);

        var secondPoc = result.PointsOfContact[1];
        secondPoc.FirstName.Should().Be("Jane");
        secondPoc.LastName.Should().Be("Smith");
        secondPoc.NickName.Should().Be("Janey");
        secondPoc.Email.Should().Be("jane.smith@company.com");
        secondPoc.PhoneNumber.Should().Be("987654321");
        secondPoc.CreatedById.Should().Be(42);
    }

    [Fact]
    public void Maps_InputModel_With_No_PointsOfContact()
    {
        var input = new CreateCompanyWithPointsOfContactInputModel(
            CompanyName: "Test Company",
            CompanyEmail: "test@company.com",
            CompanyPhoneNumber: "555-1234",
            CreatedById: 42,
            PointsOfContact: new List<CreatePointOfContactInputModel>()
        );

        var result = input.Adapt<CreateCompanyWithPointsOfContactInputDataModel>(Config);

        result.CompanyName.Should().Be("Test Company");
        result.CompanyEmail.Should().Be("test@company.com");
        result.CompanyPhoneNumber.Should().Be("555-1234");
        result.CreatedById.Should().Be(42);
        result.PointsOfContact.Should().BeEmpty();
    }

    [Fact]
    public void InputModel_CompanyName_MustBeNonEmpty()
    {
        var input = new CreateCompanyWithPointsOfContactInputModel(
            CompanyName: string.Empty,
            CompanyEmail: "test@company.com",
            CompanyPhoneNumber: "555-1234",
            CreatedById: 42,
            PointsOfContact: new List<CreatePointOfContactInputModel>()
        );

        var result = () => input.Adapt<CreateCompanyWithPointsOfContactInputDataModel>(Config);


        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_CompanyEmail_IsTrimmed()
    {
        var input = new CreateCompanyWithPointsOfContactInputModel(
            CompanyName: "Test Company",
            CompanyEmail: " test@company.com  ",
            CompanyPhoneNumber: "555-1234",
            CreatedById: 42,
            PointsOfContact: new List<CreatePointOfContactInputModel>()
        );

        var result = input.Adapt<CreateCompanyWithPointsOfContactInputDataModel>(Config);

        result.CompanyEmail.Should().Be("test@company.com");
    }

    [Fact]
    public void InputModel_CompanyPhoneNumber_IsTrimmed()
    {
        var input = new CreateCompanyWithPointsOfContactInputModel(
            CompanyName: "Test Company",
            CompanyEmail: "test@company.com",
            CompanyPhoneNumber: " 555-1234  ",
            CreatedById: 42,
            PointsOfContact: new List<CreatePointOfContactInputModel>()
        );

        var result = input.Adapt<CreateCompanyWithPointsOfContactInputDataModel>(Config);

        result.CompanyPhoneNumber.Should().Be("555-1234");
    }

    [Fact]
    public void InputModel_CompanyEmailAndPhone_CanBeEmpty()
    {
        var input = new CreateCompanyWithPointsOfContactInputModel(
            CompanyName: "Test Company",
            CompanyEmail: string.Empty,
            CompanyPhoneNumber: string.Empty,
            CreatedById: 42,
            PointsOfContact: new List<CreatePointOfContactInputModel>()
        );

        var result = input.Adapt<CreateCompanyWithPointsOfContactInputDataModel>(Config);

        result.CompanyEmail.Should().BeNull();
        result.CompanyPhoneNumber.Should().BeNull();
    }
}