using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsors;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Sponsors;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Sponsors;

public class SponsorsListViewModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_Correctly_From_AllSponsorsDataModel()
    {
        var dataModel = new AllSponsorsDataModel(Auditable)
        {
            SponsorId = 314,
            SponsorFirstName = "Mario Alberto",
            SponsorLastName = "Delucci",
            SponsorNickName = "Albert",
            SponsorGender = Gender.Male,
            IsSponsorActive = true,
            SponsorEmail = "albert@email.com",
            SponsorPhoneNumber = "555-1234",
        };

        var viewModel = dataModel.Adapt<SponsorsListViewModel>(Config);

        viewModel.SponsorId.Should().Be(314);
        viewModel.SponsorFirstName.Should().Be("Mario Alberto");
        viewModel.SponsorLastName.Should().Be("Delucci");
        viewModel.SponsorNickName.Should().Be("Albert");
        viewModel.SponsorGender.Should().Be(Gender.Male);
        viewModel.IsSponsorActive.Should().BeTrue();
        viewModel.SponsorEmail.Should().Be("albert@email.com");
        viewModel.SponsorPhoneNumber.Should().Be("555-1234");
    }

    [Fact]
    public void Maps_Nullable_Fields_To_Empty()
    {
        var dataModel = new AllSponsorsDataModel(Auditable)
        {
            SponsorNickName = null,
            SponsorPhoneNumber = null,
        };

        var viewModel = dataModel.Adapt<SponsorsListViewModel>(Config);

        viewModel.SponsorNickName.Should().BeEmpty();
        viewModel.SponsorPhoneNumber.Should().BeEmpty();
    }
}