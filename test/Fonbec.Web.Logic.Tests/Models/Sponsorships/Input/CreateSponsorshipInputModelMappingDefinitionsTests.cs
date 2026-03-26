using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Sponsorships.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Sponsorships.Input;

public class CreateSponsorshipInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_Correctly_When_Sponsor_Present_And_Company_Null()
    {
        var input = new CreateSponsorshipInputModel(
            StudentId: 314,
            SponsorId: 512,
            CompanyId: null,
            Sponsor: new SelectableModel<int>(512, "Maxwell Smart"),
            SponsorshipStartDate: new DateTime(1996, 6, 19),
            SponsorshipEndDate: new DateTime(1996, 7, 3),
            SponsorshipNotes: "A nut for a jar of tuna",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorshipInputDataModel>(Config);

        result.StudentId.Should().Be(314);
        result.SponsorId.Should().Be(512);
        result.CompanyId.Should().BeNull();
        result.SponsorshipStartDate.Should().BeSameDateAs(new DateTime(1996, 6, 19));
        result.SponsorshipEndDate.Should().BeSameDateAs(new DateTime(1996, 7, 3));
        result.SponsorshipNotes.Should().Be("A nut for a jar of tuna");
        result.CreatedById.Should().Be(99);
    }

    [Fact]
    public void Maps_Correctly_When_Company_Present_And_Sponsor_Null()
    {
        var input = new CreateSponsorshipInputModel(
            StudentId: 314,
            SponsorId: null,
            CompanyId: 777,
            SponsorshipStartDate: new DateTime(1996, 6, 19),
            SponsorshipEndDate: new DateTime(1996, 7, 3),
            SponsorshipNotes: "A nut for a jar of tuna",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorshipInputDataModel>(Config);

        result.StudentId.Should().Be(314);
        result.CompanyId.Should().Be(777);
        result.SponsorId.Should().BeNull();
        result.SponsorshipStartDate.Should().BeSameDateAs(new DateTime(1996, 6, 19));
        result.SponsorshipEndDate.Should().BeSameDateAs(new DateTime(1996, 7, 3));
        result.SponsorshipNotes.Should().Be("A nut for a jar of tuna");
        result.CreatedById.Should().Be(99);
    }

    [Fact]
    public void InputModel_Sponsor_IsMappedToNull_WhenNull()
    {
        var input = new CreateSponsorshipInputModel(
            StudentId: 314,
            Sponsor: null,
            SponsorshipStartDate: new DateTime(1996, 6, 19),
            SponsorshipEndDate: new DateTime(1996, 7, 3),
            SponsorshipNotes: "A nut for a jar of tuna",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorshipInputDataModel>(Config);

        result.SponsorId.Should().BeNull();
    }

    [Fact]
    public void InputModel_SponsorshipNotes_IsTrimmed()
    {
        var input = new CreateSponsorshipInputModel(
            StudentId: 314,
            SponsorId: 512,
            CompanyId: null,
            Sponsor: new SelectableModel<int>(512, "Maxwell Smart"),
            SponsorshipStartDate: new DateTime(1996, 6, 19),
            SponsorshipEndDate: new DateTime(1996, 7, 3),
            SponsorshipNotes: "   A nut for a jar of tuna    ",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorshipInputDataModel>(Config);

        result.SponsorshipNotes.Should().Be("A nut for a jar of tuna");
    }

    [Fact]
    public void InputModel_SponsorshipNotes_Empty_String_Becomes_Null()
    {
        var input = new CreateSponsorshipInputModel(
            StudentId: 314,
            SponsorId: 512,
            CompanyId: null,
            SponsorshipStartDate: new DateTime(1996, 6, 19),
            SponsorshipEndDate: null,
            SponsorshipNotes: "   ",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorshipInputDataModel>(Config);

        result.SponsorshipNotes.Should().BeNull();
    }

    [Fact]
    public void Thrown_Exception_When_Company_And_Sponsor_Ausent()
    {
        var input = new CreateSponsorshipInputModel(
            StudentId: 314,
            SponsorId: null,
            CompanyId: null,
            SponsorshipStartDate: new DateTime(1996, 6, 19),
            SponsorshipEndDate: new DateTime(1996, 7, 3),
            SponsorshipNotes: "A nut for a jar of tuna",
            CreatedById: 99
        );

        var result = () => input.Adapt<CreateSponsorshipInputDataModel>(Config);

        result.Should().Throw<InvalidOperationException>()
            .WithMessage("Business rule violation: only one of SponsorId or CompanyId must be null");
    }

    [Fact]
    public void Thrown_Exception_When_Both_Company_And_Sponsor_Present()
    {
        var input = new CreateSponsorshipInputModel(
            StudentId: 314,
            SponsorId: 123,
            CompanyId: 124,
            SponsorshipStartDate: new DateTime(1996, 6, 19),
            SponsorshipEndDate: new DateTime(1996, 7, 3),
            SponsorshipNotes: "A nut for a jar of tuna",
            CreatedById: 99
        );

        var result = () => input.Adapt<CreateSponsorshipInputDataModel>(Config);

        result.Should().Throw<InvalidOperationException>()
            .WithMessage("Business rule violation: only one of SponsorId or CompanyId must be null");
    }

}