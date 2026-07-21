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
    private readonly ILetterExemptionService _letterExemptionService;
    private readonly FacilitatorService _facilitatorService;

    public FacilitatorServiceGetActiveSponsoredStudentsTests()
    {
        _facilitatorRepository = Substitute.For<IFacilitatorRepository>();
        _letterExemptionService = Substitute.For<ILetterExemptionService>();
        _letterExemptionService.GetExemptStudentIdsForPlanAsync(Arg.Any<int>()).Returns([]);
        _facilitatorService = new FacilitatorService(_facilitatorRepository, _letterExemptionService);
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
    public async Task GetStudentsDashboardAsync_Sets_Letter_Exempt_Flag_For_Current_Plan()
    {
        _facilitatorRepository.GetActiveSponsoredStudentsAsync(2)
            .Returns([
                new FacilitatorStudentsDataModel(Auditable) { StudentId = 10, StudentFirstName = "Ana", StudentLastName = "Becaria" },
                new FacilitatorStudentsDataModel(Auditable) { StudentId = 11, StudentFirstName = "Beto", StudentLastName = "Becario" },
            ]);
        _facilitatorRepository.GetCurrentPlanForFacilitatorAsync(2)
            .Returns(new CurrentPlanDataModel { PlanId = 7, StartsOn = new DateTime(2026, 6, 1) });
        _letterExemptionService.GetExemptStudentIdsForPlanAsync(7).Returns([10]);

        var result = await _facilitatorService.GetStudentsDashboardAsync(2);

        result.Students.Single(s => s.StudentId == 10).IsLetterExemptForCurrentPlan.Should().BeTrue();
        result.Students.Single(s => s.StudentId == 11).IsLetterExemptForCurrentPlan.Should().BeFalse();
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Does_Not_Query_Exemptions_When_No_Current_Plan()
    {
        _facilitatorRepository.GetActiveSponsoredStudentsAsync(2)
            .Returns([
                new FacilitatorStudentsDataModel(Auditable) { StudentId = 10, StudentFirstName = "Ana", StudentLastName = "Becaria" },
            ]);

        var result = await _facilitatorService.GetStudentsDashboardAsync(2);

        result.Students.Single().IsLetterExemptForCurrentPlan.Should().BeFalse();
        await _letterExemptionService.DidNotReceive().GetExemptStudentIdsForPlanAsync(Arg.Any<int>());
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
