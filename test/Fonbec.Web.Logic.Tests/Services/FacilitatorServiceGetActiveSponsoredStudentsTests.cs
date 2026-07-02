using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Tests.Models;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class FacilitatorServiceGetActiveSponsoredStudentsTests : MappingTestBase
{
    private readonly IFacilitatorRepository _facilitatorRepository;
    private readonly FacilitatorService _facilitatorService;

    public FacilitatorServiceGetActiveSponsoredStudentsTests()
    {
        _facilitatorRepository = Substitute.For<IFacilitatorRepository>();
        _facilitatorService = new FacilitatorService(_facilitatorRepository);
    }

    [Fact]
    public async Task GetActiveSponsoredStudentsAsync_Maps_Repository_Results()
    {
        _facilitatorRepository.GetActiveSponsoredStudentsAsync(2)
            .Returns([
                new FacilitatorStudentsDataModel(Auditable)
                {
                    StudentId = 10,
                    StudentFirstName = "Ana",
                    StudentLastName = "Becaria",
                    StudentNickName = "Anita",
                },
            ]);

        var result = await _facilitatorService.GetActiveSponsoredStudentsAsync(2);

        result.Should().ContainSingle(vm =>
            vm.StudentId == 10
            && vm.StudentFirstName == "Ana"
            && vm.StudentNickName == "Anita");
    }
}