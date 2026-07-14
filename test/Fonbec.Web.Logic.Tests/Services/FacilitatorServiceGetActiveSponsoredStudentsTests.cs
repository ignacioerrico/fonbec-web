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
    public async Task GetStudentsDashboardAsync_Maps_Repository_Students()
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

        var result = await _facilitatorService.GetStudentsDashboardAsync(2);

        result.Students.Should().ContainSingle(vm =>
            vm.StudentId == 10
            && vm.StudentFirstName == "Ana"
            && vm.StudentNickName == "Anita");
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Maps_Current_Plan()
    {
        _facilitatorRepository.GetActiveSponsoredStudentsAsync(2)
            .Returns([]);
        _facilitatorRepository.GetCurrentPlanForFacilitatorAsync(2)
            .Returns(new CurrentPlanDataModel
            {
                PlanId = 7,
                StartsOn = new DateTime(2026, 6, 1),
            });

        var result = await _facilitatorService.GetStudentsDashboardAsync(2);

        result.CurrentPlanId.Should().Be(7);
        result.CurrentPlanLabel.Should().Be("Jun 2026");
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Has_No_Plan_Label_When_No_Current_Plan()
    {
        _facilitatorRepository.GetActiveSponsoredStudentsAsync(2)
            .Returns([]);

        var result = await _facilitatorService.GetStudentsDashboardAsync(2);

        result.CurrentPlanId.Should().BeNull();
        result.CurrentPlanLabel.Should().BeNull();
    }
}
