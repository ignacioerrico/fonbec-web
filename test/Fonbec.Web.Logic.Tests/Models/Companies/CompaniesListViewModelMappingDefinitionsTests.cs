using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Companies;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.Logic.Models.Companies;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Companies;

public class CompaniesListViewModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_Company_From_DataModel_To_ViewModel()
    {
        var dataModel = new AllCompaniesDataModel(Auditable)
        {
            CompanyId = 314,
            CompanyName = "CompanyNamedsadssad",
            CompanyEmail = "Company Maaail",
            CompanyPhoneNumber = "12345",
            Notes = "Some notes",
        };

        // Act
        var viewModel = dataModel.Adapt<CompaniesListViewModel>(Config);

        // Assert
        viewModel.CompanyId.Should().Be(314);
        viewModel.CompanyName.Should().Be("CompanyNamedsadssad");
        viewModel.CompanyEmail.Should().Be("Company Maaail");
        viewModel.CompanyPhoneNumber.Should().Be("12345");
        viewModel.CompanyNotes.Should().Be("Some notes");
    }

    [Fact]
    public void Maps_Nullable_Fields_To_Empty_Strings()
    {
        var dataModel = new AllCompaniesDataModel(Auditable)
        {
            CompanyEmail = null,
            CompanyPhoneNumber = null,
            Notes = null,
        };

        var viewModel = dataModel.Adapt<CompaniesListViewModel>(Config);

        viewModel.CompanyEmail.Should().BeEmpty();
        viewModel.CompanyPhoneNumber.Should().BeEmpty();
        viewModel.CompanyNotes.Should().BeEmpty();
    }

    [Fact]
    public void Maps_Null_Collections_To_Empty_Lists()
    {
        var dataModel = new AllCompaniesDataModel(Auditable)
        {
            CompanyPointsOfContact = null,
            CompanySponsors = null,
        };

        var viewModel = dataModel.Adapt<CompaniesListViewModel>(Config);

        viewModel.CompanyPointsOfContact.Should().BeEmpty();
        viewModel.CompanySponsors.Should().BeEmpty();
    }

    [Fact]
    public void IsIdenticalTo_Compares_Name_Email_Phone_Notes()
    {
        var companyListViewModel1 = new CompaniesListViewModel
        {
            CompanyId = 314,
            CompanyName = "Company",
            CompanyEmail = "companymaaail",
            CompanyPhoneNumber = "12345",
            CompanyNotes = "Notes"
        };
        var companyListViewModel2 = new CompaniesListViewModel
        {
            CompanyId = 314,
            CompanyName = "Company",
            CompanyEmail = "companymaaail",
            CompanyPhoneNumber = "12345",
            CompanyNotes = "Notes"
        };

        var result = companyListViewModel1.IsIdenticalTo(companyListViewModel2);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsIdenticalTo_Returns_False_When_Notes_Differ()
    {
        var original = new CompaniesListViewModel { CompanyName = "Company", CompanyNotes = "Notes" };
        var modified = new CompaniesListViewModel { CompanyName = "Company", CompanyNotes = "Different" };

        original.IsIdenticalTo(modified).Should().BeFalse();
    }

    [Fact]
    public void POCs_And_Sponsors_Are_Mapped_From_DataModel_To_ViewModel()
    {
        var dataModel = new AllCompaniesDataModel(Auditable)
        {
            CompanyId = 314,
            CompanyName = "CompanyNamedsadssad",
            CompanyEmail = "Company Maaail",
            CompanyPhoneNumber = "12345",
            CompanyPointsOfContact =
            [
                new PointOfContact { FirstName = "John", LastName = "Doe", CompanyId = 314 },
                new PointOfContact { FirstName = "Jane", LastName = "Smith", CompanyId = 314 }
            ],
            CompanySponsors =
            [
                new Sponsor { FirstName = "Alice", LastName = "Johnson", ChapterId = 1 },
                new Sponsor { FirstName = "Bob", LastName = "Brown", ChapterId = 1 }
            ]
        };

        var viewModel = dataModel.Adapt<CompaniesListViewModel>(Config);

        viewModel.CompanyPointsOfContact.Should().ContainInOrder("John Doe", "Jane Smith");
        viewModel.CompanySponsors.Should().ContainInOrder("Alice Johnson", "Bob Brown");
    }
}
