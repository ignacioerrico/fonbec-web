using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Facilitators;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Facilitators;

public class FacilitatorStudentsListViewModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_Correctly_From_FacilitatorStudentsDataModel()
    {
        var dataModel = new FacilitatorStudentsDataModel
        {
            StudentId = 10,
            StudentFirstName = "Jhon",
            StudentLastName = "Cena",
            StudentNickName = "The Champ",
        };

        var viewModel = dataModel.Adapt<FacilitatorStudentsListViewModel>(Config);

        viewModel.StudentId.Should().Be(10);
        viewModel.StudentFirstName.Should().Be("Jhon");
        viewModel.StudentLastName.Should().Be("Cena");
        viewModel.StudentNickName.Should().Be("The Champ");
    }

    [Fact]
    public void Maps_Nullable_Fields_To_Empty_Or_Default()
    {
        var dataModel = new FacilitatorStudentsDataModel
        {
            StudentNickName = null,
        };

        var viewModel = dataModel.Adapt<FacilitatorStudentsListViewModel>(Config);

        viewModel.StudentNickName.Should().BeNull();
    }

    [Fact]
    public void IsIdenticalTo_Returns_True_When_All_Fields_Match()
    {
        var vm1 = new FacilitatorStudentsListViewModel
        {
            StudentFirstName = "First Name",
            StudentLastName = "Last Name",
            StudentNickName = "Nick",
            EducationLevel = EducationLevel.SecondarySchool,
            Sponsors =
            [
                new DashboardSponsorViewModel { SponsorshipId = 1, SponsorId = 10, CompanyId = null, RecipientName = "Sponsor A", IsCompany = false },
            ],
        };

        var vm2 = new FacilitatorStudentsListViewModel
        {
            StudentFirstName = "First Name",
            StudentLastName = "Last Name",
            StudentNickName = "Nick",
            EducationLevel = EducationLevel.SecondarySchool,
            Sponsors =
            [
                new DashboardSponsorViewModel { SponsorshipId = 1, SponsorId = 10, CompanyId = null, RecipientName = "Sponsor A", IsCompany = false },
            ],
        };

        vm1.IsIdenticalTo(vm2).Should().BeTrue();
    }

    [Fact]
    public void IsIdenticalTo_Returns_False_When_EducationLevel_Differs()
    {
        var vm1 = new FacilitatorStudentsListViewModel
        {
            StudentFirstName = "First Name",
            StudentLastName = "Last Name",
            EducationLevel = EducationLevel.PrimarySchool,
        };

        var vm2 = new FacilitatorStudentsListViewModel
        {
            StudentFirstName = "First Name",
            StudentLastName = "Last Name",
            EducationLevel = EducationLevel.University,
        };

        vm1.IsIdenticalTo(vm2).Should().BeFalse();
    }

    [Fact]
    public void IsIdenticalTo_Returns_False_When_Sponsors_Differ()
    {
        var vm1 = new FacilitatorStudentsListViewModel
        {
            StudentFirstName = "First Name",
            StudentLastName = "Last Name",
            Sponsors =
            [
                new DashboardSponsorViewModel { SponsorshipId = 1, SponsorId = 10 },
            ],
        };

        var vm2 = new FacilitatorStudentsListViewModel
        {
            StudentFirstName = "First Name",
            StudentLastName = "Last Name",
            Sponsors =
            [
                new DashboardSponsorViewModel { SponsorshipId = 2, SponsorId = 20 },
            ],
        };

        vm1.IsIdenticalTo(vm2).Should().BeFalse();
    }

    [Fact]
    public void IsIdenticalTo_Returns_False_When_Sponsor_Count_Differs()
    {
        var vm1 = new FacilitatorStudentsListViewModel
        {
            StudentFirstName = "First Name",
            StudentLastName = "Last Name",
            Sponsors =
            [
                new DashboardSponsorViewModel { SponsorshipId = 1, SponsorId = 10 },
            ],
        };

        var vm2 = new FacilitatorStudentsListViewModel
        {
            StudentFirstName = "First Name",
            StudentLastName = "Last Name",
            Sponsors =
            [
                new DashboardSponsorViewModel { SponsorshipId = 1, SponsorId = 10 },
                new DashboardSponsorViewModel { SponsorshipId = 2, SponsorId = 20 },
            ],
        };

        vm1.IsIdenticalTo(vm2).Should().BeFalse();
    }
}
