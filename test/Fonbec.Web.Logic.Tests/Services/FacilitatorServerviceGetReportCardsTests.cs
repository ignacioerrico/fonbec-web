using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Tests.Models;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class FacilitatorServiceReportCardTests : MappingTestBase
{
    private readonly IFacilitatorRepository _facilitatorRepository;
    private readonly ILetterExemptionService _letterExemptionService;
    private readonly FacilitatorService _facilitatorService;

    public FacilitatorServiceReportCardTests()
    {
        _facilitatorRepository = Substitute.For<IFacilitatorRepository>();
        _letterExemptionService = Substitute.For<ILetterExemptionService>();

        _letterExemptionService
            .GetActiveExemptionReasonsForPlanAsync(Arg.Any<int>())
            .Returns(new Dictionary<int, string>());

        _facilitatorRepository
            .GetLatestReportCardsAsync(
                 Arg.Any<List<int>>(),Arg.Any<int>())
                .Returns([]);

        _facilitatorService = new FacilitatorService(
            _facilitatorRepository,
            _letterExemptionService);
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Maps_Report_Cards_To_Student()
    {
        _facilitatorRepository.GetActiveSponsoredStudentsAsync(2)
            .Returns([
                new FacilitatorStudentsDataModel(Auditable)
                {
                    StudentId = 10,
                    StudentFirstName = "Ana",
                    StudentLastName = "Becaria",
                },
            ]);

        _facilitatorRepository.GetLatestReportCardsAsync(Arg.Any<List<int>>(), Arg.Any<int>())
            .Returns([
                new FacilitatorReportsDataModel
                {
                    StudentId = 10,
                    Period = new DateOnly(2026, 6, 1),
                    Description = "June report card",
                },
            ]);

        var result = await _facilitatorService.GetStudentsDashboardAsync(2);

        var student = result.Students.Single();

        student.ReportCardChip.Should().ContainSingle();

        var reportCard = student.ReportCardChip.Single();

        reportCard.Period.Should().Be(new DateOnly(2026, 6, 1));
        reportCard.Description.Should().Be("June report card");
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Assigns_Report_Cards_To_Correct_Students()
    {
        _facilitatorRepository.GetActiveSponsoredStudentsAsync(2)
            .Returns([
                new FacilitatorStudentsDataModel(Auditable)
                {
                    StudentId = 10,
                    StudentFirstName = "Ana",
                    StudentLastName = "Becaria",
                },
                new FacilitatorStudentsDataModel(Auditable)
                {
                    StudentId = 11,
                    StudentFirstName = "Beto",
                    StudentLastName = "Becario",
                },
            ]);

        _facilitatorRepository.GetLatestReportCardsAsync(Arg.Any<List<int>>(), Arg.Any<int>())
            .Returns([
                new FacilitatorReportsDataModel
                {
                    StudentId = 10,
                    Period = new DateOnly(2026, 6, 1),
                    Description = "Ana report card",
                },
                new FacilitatorReportsDataModel
                {
                    StudentId = 11,
                    Period = new DateOnly(2026, 6, 1),
                    Description = "Beto report card",
                },
            ]);

        var result = await _facilitatorService.GetStudentsDashboardAsync(2);

        var ana = result.Students.Single(s => s.StudentId == 10);
        var beto = result.Students.Single(s => s.StudentId == 11);

        ana.ReportCardChip.Should().ContainSingle();
        ana.ReportCardChip.Single().Description.Should().Be("Ana report card");

        beto.ReportCardChip.Should().ContainSingle();
        beto.ReportCardChip.Single().Description.Should().Be("Beto report card");
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Returns_Empty_Report_Cards_When_Student_Has_None()
    {
        _facilitatorRepository.GetActiveSponsoredStudentsAsync(2)
            .Returns([
                new FacilitatorStudentsDataModel(Auditable)
                {
                    StudentId = 10,
                    StudentFirstName = "Ana",
                    StudentLastName = "Becaria",
                },
            ]);

        _facilitatorRepository.GetLatestReportCardsAsync(Arg.Any<List<int>>(), Arg.Any<int>())
            .Returns([]);

        var result = await _facilitatorService.GetStudentsDashboardAsync(2);

        result.Students.Single()
            .ReportCardChip.Should().BeEmpty();
    }
}