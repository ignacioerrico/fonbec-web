using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Companies.Input;
using Fonbec.Web.Logic.Services;
using Mapster;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class CompanyServiceCreateTests
{
    private readonly ICompanyRepository _companyRepository;
    private readonly CompanyService _companyService;

    public CompanyServiceCreateTests()
    {
        _companyRepository = Substitute.For<ICompanyRepository>();
        _companyService = new CompanyService(_companyRepository);
        TypeAdapterConfig.GlobalSettings.Scan(typeof(CreateCompanyInputModel).Assembly);
    }

    [Fact]
    public async Task CreateCompanyAsync_Returns_Success_When_Company_Is_Created()
    {
        _companyRepository.CreateCompanyAsync(Arg.Any<CreateCompanyInputDataModel>())
            .Returns(new CreateCompanyRepositoryResult(CompanyId: 42));

        var input = new CreateCompanyInputModel(
            "Acme Corp",
            "acme@mail.com",
            "12345",
            "Notes",
            [],
            [],
            CreatedById: 1);

        var result = await _companyService.CreateCompanyAsync(input);

        result.AnyAffectedRows.Should().BeTrue();
        result.HasMissingSponsors.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCompanyAsync_Returns_Missing_Sponsors_With_Names()
    {
        _companyRepository.CreateCompanyAsync(Arg.Any<CreateCompanyInputDataModel>())
            .Returns(new CreateCompanyRepositoryResult(MissingSponsorIds: [5, 99]));

        var input = new CreateCompanyInputModel(
            "Acme Corp",
            "acme@mail.com",
            "12345",
            "Notes",
            [],
            [new SelectableModel<int>(5, "Alice Johnson")],
            CreatedById: 1);

        var result = await _companyService.CreateCompanyAsync(input);

        result.HasMissingSponsors.Should().BeTrue();
        result.AnyAffectedRows.Should().BeFalse();
        result.MissingSponsors.Should().ContainSingle(s => s.SponsorId == 5 && s.SponsorName == "Alice Johnson");
        result.MissingSponsors.Should().ContainSingle(s => s.SponsorId == 99 && s.SponsorName == "Padrino desconocido");
    }
}
