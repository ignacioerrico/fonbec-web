using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsorships;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.Logic.Models.Sponsorships;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Sponsorships;

public class SponsorshipsListViewModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_Correctly_From_AllSponsorshipsDataModel_To_ViewModel()
    {
        var now = DateTime.UtcNow;
        var dataModel = new AllSponsorshipsDataModel
        {
            StudentFullName = "Student FullName",
            Sponsorships = new List<AllSponsorshipsSponsorshipsDataModel>
            {
                new(Auditable)
                {
                    Sponsor = new Sponsor
                    {
                        FirstName = "Sponsor1 FirstName",
                        LastName = "Sponsor1 LastName",
                    },
                    SponsorshipStartDate = now,
                    SponsorshipEndDate = now.AddYears(2),
                },
                new(Auditable)
                {
                    Sponsor = new Sponsor
                    {
                        FirstName = "Sponsor2 FirstName",
                        LastName = "Sponsor2 LastName",
                    },
                    SponsorshipStartDate = now,
                },
                new(Auditable)
                {
                    Company = new Company
                    {
                        Name = "Company3 Name",
                    },
                    SponsorshipStartDate = now,
                    SponsorshipEndDate = now.AddYears(1),
                },
            },
        };

        var result = dataModel.Adapt<SponsorshipsListViewModel>(Config);

        result.StudentFullName.Should().Be("Student FullName");
        result.Sponsorships.Should().HaveCount(3);

        result.Sponsorships[0].IsSponsoredByCompany.Should().BeFalse();
        result.Sponsorships[0].SponsorshipFullName.Should().Be("Sponsor1 FirstName Sponsor1 LastName");
        result.Sponsorships[0].SponsorshipStartDate.Should().Be(now);
        result.Sponsorships[0].SponsorshipStartDateString.Should().Be(now.ToString("MM/yyyy"));
        result.Sponsorships[0].SponsorshipEndDate.Should().Be(now.AddYears(2));
        result.Sponsorships[0].SponsorshipEndDateString.Should().Be(now.AddYears(2).ToString("MM/yyyy"));

        result.Sponsorships[1].IsSponsoredByCompany.Should().BeFalse();
        result.Sponsorships[1].SponsorshipFullName.Should().Be("Sponsor2 FirstName Sponsor2 LastName");
        result.Sponsorships[1].SponsorshipStartDate.Should().Be(now);
        result.Sponsorships[1].SponsorshipStartDateString.Should().Be(now.ToString("MM/yyyy"));
        result.Sponsorships[1].SponsorshipEndDate.Should().BeNull();
        result.Sponsorships[1].SponsorshipEndDateString.Should().Be("—");

        result.Sponsorships[2].IsSponsoredByCompany.Should().BeTrue();
        result.Sponsorships[2].SponsorshipFullName.Should().Be("Company3 Name");
        result.Sponsorships[2].SponsorshipStartDate.Should().Be(now);
        result.Sponsorships[2].SponsorshipStartDateString.Should().Be(now.ToString("MM/yyyy"));
        result.Sponsorships[2].SponsorshipEndDate.Should().Be(now.AddYears(1));
        result.Sponsorships[2].SponsorshipEndDateString.Should().Be(now.AddYears(1).ToString("MM/yyyy"));
    }
}
