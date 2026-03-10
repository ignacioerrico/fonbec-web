
using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsorships;
using Fonbec.Web.Logic.Models.Sponsorships;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Sponsorships;

public class SponsorshipsListViewModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_Correctly_From_AllSponsorshipsDataModel_For_Sponsor()
    {
        var now = DateTime.UtcNow;
        var dataModel = new AllSponsorshipsDataModel(Auditable)
        {
            StudentId  = 10,
            SponsorId = 1,
            CompanyId = null,
            SponsorshipStartDate = now,
            SponsorshipEndDate = now.AddYears(2),
            SponsorshipNotes = "Some notes",
            SponsorshipState = "Some state",
            SponsorFullName = "Sponsor",
            StudentFullName = "Jane Smith",
            CompanyName = null,
        };

        var viewModel = dataModel.Adapt<SponsorshipsListViewModel>(Config);

        viewModel.StudentId.Should().Be(10);
        viewModel.SponsorId.Should().Be(1);
        viewModel.CompanyId.Should().BeNull();
        viewModel.SponsorshipStartDate.Should().Be(now);
        viewModel.SponsorshipEndDate.Should().Be(now.AddYears(2));
        viewModel.SponsorFullName.Should().Be("Sponsor");
        viewModel.StudentFullName.Should().Be("Jane Smith");
        viewModel.CompanyName.Should().BeEmpty();
        viewModel.SponsorshipNotes.Should().Be("Some notes");
        viewModel.SponsorshipState.Should().Be("Some state");
        viewModel.SponsorOrCompanyName.Should().Be("Sponsor");
        viewModel.IsCompany.Should().BeFalse();
    }

    [Fact]
    public void Maps_All_Fields_Correctly_From_AllSponsorshipsDataModel_For_Company()
    {
        var now = DateTime.UtcNow;
        var dataModel = new AllSponsorshipsDataModel(Auditable)
        {
            StudentId = 10,
            SponsorId = null,
            CompanyId = 1,
            SponsorshipStartDate = now,
            SponsorshipEndDate = now.AddYears(2),
            SponsorshipNotes = "Some notes",
            SponsorshipState = "Some state",
            SponsorFullName = null,
            StudentFullName = "Jane Smith",
            CompanyName = "Company",
        };

        var viewModel = dataModel.Adapt<SponsorshipsListViewModel>(Config);

        viewModel.StudentId.Should().Be(10);
        viewModel.SponsorId.Should().BeNull();
        viewModel.CompanyId.Should().Be(1);
        viewModel.SponsorshipStartDate.Should().Be(now);
        viewModel.SponsorshipEndDate.Should().Be(now.AddYears(2));
        viewModel.SponsorFullName.Should().BeEmpty();
        viewModel.StudentFullName.Should().Be("Jane Smith");
        viewModel.CompanyName.Should().Be("Company");
        viewModel.SponsorOrCompanyName.Should().Be("Company");
        viewModel.IsCompany.Should().BeTrue();
    }

    [Fact]
    public void Maps_Nullable_Fields_To_Empty_Or_Default_For_Sponsorship()
    {
        var dataModel = new AllSponsorshipsDataModel(Auditable)
        {
            SponsorshipNotes = null,
            SponsorFullName = null,
            CompanyName = null
        };

        var viewModel = dataModel.Adapt<SponsorshipsListViewModel>(Config);

        viewModel.SponsorshipNotes.Should().BeEmpty();
        viewModel.SponsorFullName.Should().BeEmpty();
        viewModel.CompanyName.Should().BeEmpty();
    }
}
