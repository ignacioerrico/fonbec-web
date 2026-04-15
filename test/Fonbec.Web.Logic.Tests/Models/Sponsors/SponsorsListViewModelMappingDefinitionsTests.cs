using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsors;
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
            SponsorId = 10,
            SponsorFirstName = "Joseph",
            SponsorLastName = "Wilson",
            SponsorNickName = "jwilson",
            SponsorPhoneNumber = "1234567890",
            SponsorEmail = "jose@email.com",
            SponsorChapterName = "Cordoba",
        };

        var viewModel = dataModel.Adapt<SponsorsListViewModel>(Config);

        viewModel.SponsorId.Should().Be(10);
        viewModel.SponsorFirstName.Should().Be("Joseph");
        viewModel.SponsorLastName.Should().Be("Wilson");
        viewModel.SponsorNickName.Should().Be("jwilson");
        viewModel.SponsorPhoneNumber.Should().Be("1234567890");
        viewModel.SponsorEmail.Should().Be("jose@email.com");
        viewModel.SponsorChapterName.Should().Be("Cordoba");
    }

    [Fact]
    public void Maps_Nullable_Fields_To_Empty_Or_Default()
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