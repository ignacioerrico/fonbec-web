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
            CompanyNotes: "Important notes go here",
            PointsOfContact:
            [
                new(PocFirstName: "John",
                    PocLastName: "Doe",
                    PocNickName: "Buddy",
                    PocEmail: "john.doe@company.com",
                    PocPhoneNumber: "123456789",
                    PocNotes: "Notes about first POC"),
                new(PocFirstName: "Jane",
                    PocLastName: "Smith",
                    PocNickName: "Jay",
                    PocEmail: "jane.smith@company.com",
                    PocPhoneNumber: "987654321",
                    PocNotes: "Notes about second POC"),
            ],
            Sponsors: [
                new(7, "First sponsor"),
                new(11, "Second sponsor"),
                new(13, "Third sponsor"),
                ],
            CreatedById: 42
        );

        var result = input.Adapt<CreateCompanyInputDataModel>(Config);

        result.CompanyName.Should().Be("Test Company");
        result.CompanyEmail.Should().Be("test@hotmail.com");
        result.CompanyPhoneNumber.Should().Be("123456789");
        result.CompanyNotes.Should().Be("Important notes go here");

        result.PointsOfContact.Should().HaveCount(2);

        result.PointsOfContact[0].PocFirstName.Should().Be("John");
        result.PointsOfContact[0].PocLastName.Should().Be("Doe");
        result.PointsOfContact[0].PocNickName.Should().Be("Buddy");
        result.PointsOfContact[0].PocEmail.Should().Be("john.doe@company.com");
        result.PointsOfContact[0].PocPhoneNumber.Should().Be("123456789");
        result.PointsOfContact[0].PocNotes.Should().Be("Notes about first POC");

        result.PointsOfContact[1].PocFirstName.Should().Be("Jane");
        result.PointsOfContact[1].PocLastName.Should().Be("Smith");
        result.PointsOfContact[1].PocNickName.Should().Be("Jay");
        result.PointsOfContact[1].PocEmail.Should().Be("jane.smith@company.com");
        result.PointsOfContact[1].PocPhoneNumber.Should().Be("987654321");
        result.PointsOfContact[1].PocNotes.Should().Be("Notes about second POC");

        result.SponsorIds.Should().Equal(7, 11, 13);
        result.CreatedById.Should().Be(42);
    }

    [Fact]
    public void InputModel_CompanyName_MustBeNonEmpty()
    {
        var input = new CreateCompanyInputModel(
            CompanyName: string.Empty,
            CompanyEmail: "test@gmail.com",
            CompanyPhoneNumber: "123456789",
            CompanyNotes: "Important notes go here",
            PointsOfContact: [],
            Sponsors: [],
            CreatedById: 42
        );

        var result = () => input.Adapt<CreateCompanyInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_CompanyEmail_IsTrimmed_And_LowerCased()
    {
        var input = new CreateCompanyInputModel(
            CompanyName: "Test Company",
            CompanyEmail: " teST@gMAil.coM  ",
            CompanyPhoneNumber: "123456789",
            CompanyNotes: "Important notes go here",
            PointsOfContact: [],
            Sponsors: [],
            CreatedById: 42
        );

        var result = input.Adapt<CreateCompanyInputDataModel>(Config);

        result.CompanyEmail.Should().Be("test@gmail.com");
    }

    [Fact]
    public void InputModel_CompanyPhoneNumber_IsTrimmed()
    {
        var input = new CreateCompanyInputModel(
            CompanyName: "Test Company",
            CompanyEmail: "test@mail.com",
            CompanyPhoneNumber: "  123456789   ",
            CompanyNotes: "Important notes go here",
            PointsOfContact: [],
            Sponsors: [],
            CreatedById: 42
        );

        var result = input.Adapt<CreateCompanyInputDataModel>(Config);

        result.CompanyPhoneNumber.Should().Be("123456789");
    }

    [Fact]
    public void InputModel_CompanyEmail_And_Phone_And_Notes_AreMappedToNull_WhenEmpty()
    {
        var input = new CreateCompanyInputModel(
            CompanyName: "Test Company",
            CompanyEmail: "  ",
            CompanyPhoneNumber: "   ",
            CompanyNotes: " ",
            PointsOfContact: [],
            Sponsors: [],
            CreatedById: 42
        );

        var result = input.Adapt<CreateCompanyInputDataModel>(Config);

        result.CompanyEmail.Should().BeNull();
        result.CompanyPhoneNumber.Should().BeNull();
        result.CompanyNotes.Should().BeNull();
    }

    [Fact]
    public void InputModel_NoPointOfContacts_AreMappedToAnEmptyList()
    {
        var input = new CreateCompanyInputModel(
            CompanyName: "Test Company",
            CompanyEmail: "test@mail.com",
            CompanyPhoneNumber: "314159265",
            CompanyNotes: "Important notes go here",
            PointsOfContact: [],
            Sponsors: [],
            CreatedById: 42
        );

        var result = input.Adapt<CreateCompanyInputDataModel>(Config);

        result.PointsOfContact.Should().BeEmpty();
    }

    [Fact]
    public void InputModel_PointsOfContact_FieldsAreNormalized_And_Trimmed()
    {
        var input = new CreateCompanyInputModel(
            CompanyName: "Test Company",
            CompanyEmail: "test@hotmail.com",
            CompanyPhoneNumber: "123456789",
            CompanyNotes: "Important notes go here",
            PointsOfContact:
            [
                new(PocFirstName: "  waLRUs  ",
                    PocLastName: "  nUTs  ",
                    PocNickName: "  rINgo  ",
                    PocEmail: "  waLRUs@mAIl.COm  ",
                    PocPhoneNumber: "  314159265  ",
                    PocNotes: "  sOMe notES  "),
            ],
            Sponsors: [],
            CreatedById: 42
        );

        var result = input.Adapt<CreateCompanyInputDataModel>(Config);

        result.PointsOfContact.Should().ContainSingle();

        result.PointsOfContact[0].PocFirstName.Should().Be("Walrus");
        result.PointsOfContact[0].PocLastName.Should().Be("Nuts");
        result.PointsOfContact[0].PocNickName.Should().Be("Ringo");
        result.PointsOfContact[0].PocEmail.Should().Be("walrus@mail.com");
        result.PointsOfContact[0].PocPhoneNumber.Should().Be("314159265");
        result.PointsOfContact[0].PocNotes.Should().Be("sOMe notES");
    }

    [Fact]
    public void InputModel_PointsOfContact_FirstName_MustNotBeEmpty()
    {
        var input = new CreateCompanyInputModel(
            CompanyName: "Test Company",
            CompanyEmail: "test@hotmail.com",
            CompanyPhoneNumber: "123456789",
            CompanyNotes: "Important notes go here",
            PointsOfContact:
            [
                new(PocFirstName: string.Empty,
                    PocLastName: "Doe",
                    PocNickName: "Buddy",
                    PocEmail: "john.doe@company.com",
                    PocPhoneNumber: "123456789",
                    PocNotes: "Notes about POC"),
            ],
            Sponsors: [],
            CreatedById: 42
        );

        var result = () => input.Adapt<CreateCompanyInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_PointsOfContact_FieldsOtherThanFirstName_MayBeEmpty()
    {
        var input = new CreateCompanyInputModel(
            CompanyName: "Test Company",
            CompanyEmail: "test@hotmail.com",
            CompanyPhoneNumber: "123456789",
            CompanyNotes: "Important notes go here",
            PointsOfContact:
            [
                new(PocFirstName: "Walrus",
                    PocLastName: string.Empty,
                    PocNickName: string.Empty,
                    PocEmail: string.Empty,
                    PocPhoneNumber: string.Empty,
                    PocNotes: "Notes about POC"),
            ],
            Sponsors: [],
            CreatedById: 42
        );

        var result = input.Adapt<CreateCompanyInputDataModel>(Config);

        result.PointsOfContact.Should().ContainSingle();

        result.PointsOfContact[0].PocLastName.Should().BeNull();
        result.PointsOfContact[0].PocEmail.Should().BeNull();
        result.PointsOfContact[0].PocPhoneNumber.Should().BeNull();
    }
}