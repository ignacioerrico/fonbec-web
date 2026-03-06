using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;
using Fonbec.Web.Logic.Models.Sponsorships.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Sponsorships.Input;

public class CreateSponsorshipInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_Correctly_When_All_Values_Present()
    {
        var input = new CreateSponsorshipInputModel(
            StudentId: 314,
            SponsorId: 512,
            SponsorshipStartDate: new DateTime(1996, 6, 19),
            SponsorshipEndDate: new DateTime(1996, 7, 3),
            SponsorshipNotes: "A nut for a jar of tuna",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorshipInputDataModel>(Config);

        result.StudentId.Should().Be(314);
        result.SponsorId.Should().Be(512);
        result.SponsorshipStartDate.Should().BeSameDateAs(new DateTime(1996, 6, 19));
        result.SponsorshipEndDate.Should().BeSameDateAs(new DateTime(1996, 7, 3));
        result.SponsorshipNotes.Should().Be("A nut for a jar of tuna");
        result.CreatedById.Should().Be(99);
    }

    [Fact]
    public void InputModel_SponsorshipNotes_IsTrimmed()
    {
        var input = new CreateSponsorshipInputModel(
            StudentId: 314,
            SponsorId: 512,
            SponsorshipStartDate: new DateTime(1996, 6, 19),
            SponsorshipEndDate: new DateTime(1996, 7, 3),
            SponsorshipNotes: "   A nut for a jar of tuna    ",
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorshipInputDataModel>(Config);

        result.SponsorshipNotes.Should().Be("A nut for a jar of tuna");
    }
}