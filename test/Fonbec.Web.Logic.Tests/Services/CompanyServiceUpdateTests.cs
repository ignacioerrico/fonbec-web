using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Companies.Input;
using Fonbec.Web.Logic.Services;
using Mapster;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class CompanyServiceUpdateTests
{
    private readonly ICompanyRepository _companyRepository;
    private readonly CompanyService _companyService;

    public CompanyServiceUpdateTests()
    {
        _companyRepository = Substitute.For<ICompanyRepository>();
        _companyService = new CompanyService(_companyRepository);
        TypeAdapterConfig.GlobalSettings.Scan(typeof(UpdateCompanyInputModel).Assembly);
    }

    [Fact]
    public async Task UpdateCompanyAsync_Returns_No_Affected_Rows_When_Name_Already_Exists()
    {
        _companyRepository
            .CompanyNameExistsAsync("Other Corp", 1)
            .Returns(true);

        var input = new UpdateCompanyInputModel(
            CompanyId: 1,
            CompanyUpdatedName: "Other Corp",
            CompanyUpdatedPhoneNumber: "123",
            CompanyUpdatedEmail: "acme@test.com",
            CompanyUpdatedNotes: "Notes",
            UpdatedById: 1);

        var result = await _companyService.UpdateCompanyAsync(input);

        result.AnyAffectedRows.Should().BeFalse();
        await _companyRepository.DidNotReceive().UpdateCompanyAsync(Arg.Any<UpdateCompanyInputDataModel>());
    }

    [Fact]
    public async Task UpdateCompanyAsync_Updates_Company_When_Name_Is_Available()
    {
        _companyRepository
            .CompanyNameExistsAsync("Renamed Corp", 1)
            .Returns(false);
        _companyRepository
            .UpdateCompanyAsync(Arg.Any<UpdateCompanyInputDataModel>())
            .Returns(1);

        var input = new UpdateCompanyInputModel(
            CompanyId: 1,
            CompanyUpdatedName: "Renamed Corp",
            CompanyUpdatedPhoneNumber: "123",
            CompanyUpdatedEmail: "acme@test.com",
            CompanyUpdatedNotes: "Notes",
            UpdatedById: 1);

        var result = await _companyService.UpdateCompanyAsync(input);

        result.AnyAffectedRows.Should().BeTrue();
        await _companyRepository.Received(1).UpdateCompanyAsync(Arg.Is<UpdateCompanyInputDataModel>(m =>
            m.CompanyId == 1 && m.CompanyUpdatedName == "Renamed Corp"));
    }
}
