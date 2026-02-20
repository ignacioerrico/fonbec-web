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
            StudentId: 1,
            SponsorId: 1, 
            SponsorshipStartDate: new DateTime(2020, 1, 1),
            SponsorshipEndDate: new DateTime(2020, 1, 1),
            CreatedById: 99
        );

        var result = input.Adapt<CreateSponsorshipInputDataModel>(Config);

        result.StudentId.Should().Be( 1 );
        result.SponsorId.Should().Be( 1 );
        result.SponsorshipStartDate.Should().Be(new DateTime(2020, 1, 1));
        result.SponsorshipEndDate.Should().Be(new DateTime(2020, 1, 1));
        result.CreatedById.Should().Be(99);
    }

}