using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Companies.Input;
using Fonbec.Web.Logic.Services;
using Mapster;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class CompanyServiceRelationsTests
{
    private readonly ICompanyRepository _companyRepository;
    private readonly CompanyService _companyService;

    public CompanyServiceRelationsTests()
    {
        _companyRepository = Substitute.For<ICompanyRepository>();
        _companyService = new CompanyService(_companyRepository);
        TypeAdapterConfig.GlobalSettings.Scan(typeof(UpdateCompanyRelationsInputModel).Assembly);
    }

    [Fact]
    public async Task UpdateCompanyRelationsAsync_Names_Missing_Sponsors()
    {
        _companyRepository
            .UpdateCompanyRelationsAsync(Arg.Any<UpdateCompanyRelationsInputDataModel>())
            .Returns(new UpdateCompanyRelationsRepositoryResult(CompanyFound: true, MissingSponsorIds: [3]));

        var result = await _companyService.UpdateCompanyRelationsAsync(new UpdateCompanyRelationsInputModel(
            CompanyId: 1,
            Contacts: [],
            Sponsors: [new SelectableModel<int>(3, "Carol Diaz")],
            UpdatedById: 1));

        result.CompanyFound.Should().BeTrue();
        result.AnyAffectedRows.Should().BeFalse();
        result.HasMissingSponsors.Should().BeTrue();
        result.MissingSponsors.Should().ContainSingle(sponsor =>
            sponsor.SponsorId == 3 && sponsor.SponsorName == "Carol Diaz");
    }
}