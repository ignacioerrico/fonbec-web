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
        var newestStart = new DateTime(2027, 1, 1);
        var middleStart = new DateTime(2026, 6, 1);
        var oldestStart = new DateTime(2025, 3, 1);
        var middleEnd = new DateTime(2026, 12, 31);
        var newestEnd = new DateTime(2027, 12, 31);

        var dataModel = new AllSponsorshipsDataModel
        {
            StudentFullName = "Student FullName",
            Sponsorships =
            [
                new(Auditable)
                {
                    Sponsor = new Sponsor
                    {
                        FirstName = "Sponsor2 FirstName",
                        LastName = "Sponsor2 LastName",
                    },
                    SponsorshipStartDate = newestStart,
                    SponsorshipEndDate = newestEnd,
                },
                new(Auditable)
                {
                    Sponsor = new Sponsor
                    {
                        FirstName = "Sponsor1 FirstName",
                        LastName = "Sponsor1 LastName",
                    },
                    SponsorshipStartDate = oldestStart,
                    LockedPlanStartsOn = [new DateTime(2025, 3, 1)],
                },
                new(Auditable)
                {
                    Company = new Company
                    {
                        Name = "Company3 Name",
                    },
                    SponsorshipStartDate = middleStart,
                    SponsorshipEndDate = middleEnd,
                },
            ],
        };

        var result = dataModel.Adapt<SponsorshipsListViewModel>(Config);

        result.StudentFullName.Should().Be("Student FullName");
        result.Sponsorships.Should().HaveCount(3);

        result.Sponsorships[0].IsSponsoredByCompany.Should().BeFalse();
        result.Sponsorships[0].SponsorshipFullName.Should().Be("Sponsor1 FirstName Sponsor1 LastName");
        result.Sponsorships[0].SponsorshipStartDate.Should().Be(oldestStart);
        result.Sponsorships[0].SponsorshipStartDateString.Should().Be("Marzo de 2025");
        result.Sponsorships[0].SponsorshipEndDate.Should().BeNull();
        result.Sponsorships[0].SponsorshipEndDateString.Should().Be("—");
        result.Sponsorships[0].LockedPlanStartsOn.Should().Equal(new DateTime(2025, 3, 1));
        result.Sponsorships[0].LockedPlanMonthLabels.Should().Equal("Marzo de 2025");

        result.Sponsorships[1].IsSponsoredByCompany.Should().BeTrue();
        result.Sponsorships[1].SponsorshipFullName.Should().Be("Company3 Name");
        result.Sponsorships[1].SponsorshipStartDate.Should().Be(middleStart);
        result.Sponsorships[1].SponsorshipStartDateString.Should().Be("Junio de 2026");
        result.Sponsorships[1].SponsorshipEndDate.Should().Be(middleEnd);
        result.Sponsorships[1].SponsorshipEndDateString.Should().Be("Diciembre de 2026");

        result.Sponsorships[2].IsSponsoredByCompany.Should().BeFalse();
        result.Sponsorships[2].SponsorshipFullName.Should().Be("Sponsor2 FirstName Sponsor2 LastName");
        result.Sponsorships[2].SponsorshipStartDate.Should().Be(newestStart);
        result.Sponsorships[2].SponsorshipStartDateString.Should().Be("Enero de 2027");
        result.Sponsorships[2].SponsorshipEndDate.Should().Be(newestEnd);
        result.Sponsorships[2].SponsorshipEndDateString.Should().Be("Diciembre de 2027");
    }

    [Fact]
    public void Maps_Unknown_StudentFullName_To_Empty_String()
    {
        var dataModel = new AllSponsorshipsDataModel { StudentFullName = null };

        var result = dataModel.Adapt<SponsorshipsListViewModel>(Config);

        result.StudentFullName.Should().BeEmpty();
        result.Sponsorships.Should().BeEmpty();
    }
}
