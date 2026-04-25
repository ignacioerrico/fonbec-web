using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.DataAccess.DataModels.Companies;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.Logic.Models.Chapters;
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
        };

        // Act
        var viewModel = dataModel.Adapt<CompaniesListViewModel>(Config);

        // Assert
        viewModel.CompanyId.Should().Be(314);
        viewModel.CompanyName.Should().Be("CompanyNamedsadssad");
        viewModel.CompanyEmail.Should().Be("Company Maaail");
        viewModel.CompanyPhoneNumber.Should().Be("12345");
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
    public void POCs_And_Sponsors_Are_Mapped_From_DataModel_To_ViewModel()
    {
        var dataModel = new AllCompaniesDataModel(Auditable)
        {
            CompanyId = 314,
            CompanyName = "CompanyNamedsadssad",
            CompanyEmail = "Company Maaail",
            CompanyPhoneNumber = "12345",
            CompanyPOCs = new List<PointOfContact>
            {
                new PointOfContact { FirstName = "John", LastName = "Doe", CompanyId = 314 },
                new PointOfContact { FirstName = "Jane", LastName = "Smith", CompanyId = 314 }
            },
            CompanySponsors = new List<Sponsor>
            {
                new Sponsor { FirstName = "Alice", LastName = "Johnson", ChapterId = 1 },
                new Sponsor { FirstName = "Bob", LastName = "Brown", ChapterId = 1 }
            }
        };

        var viewModel = dataModel.Adapt<CompaniesListViewModel>(Config);

        viewModel.CompanyPOCs.Should().ContainInOrder("John", "Jane");
        viewModel.CompanySponsors.Should().ContainInOrder("Alice Johnson", "Bob Brown");
    }
}