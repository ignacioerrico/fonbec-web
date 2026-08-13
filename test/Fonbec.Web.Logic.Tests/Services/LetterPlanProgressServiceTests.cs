using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.LetterPlanProgress;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.LetterPlanProgress;
using Fonbec.Web.Logic.Services;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class LetterPlanProgressServiceTests
{
    private const int PlanId = 100;
    private const int ChapterId = 1;
    private const int StudentId = 10;
    private const int ManagerId = 5;

    private readonly ILetterPlanProgressRepository _progressRepository = Substitute.For<ILetterPlanProgressRepository>();
    private readonly ILetterExemptionRepository _exemptionRepository = Substitute.For<ILetterExemptionRepository>();
    private readonly IPlanCompletionService _planCompletionService = Substitute.For<IPlanCompletionService>();
    private readonly IStudentRepository _studentRepository = Substitute.For<IStudentRepository>();
    private readonly LetterPlanProgressService _service;

    public LetterPlanProgressServiceTests()
    {
        _service = new LetterPlanProgressService(
            _progressRepository,
            _exemptionRepository,
            _planCompletionService,
            _studentRepository,
            TimeProvider.System);
    }

    [Fact]
    public async Task GetProgressAsync_Returns_Null_When_Repository_Returns_Null()
    {
        _progressRepository.GetProgressAsync(PlanId, ChapterId).Returns((LetterPlanProgressQueryResultDataModel?)null);

        var result = await _service.GetProgressAsync(PlanId, ChapterId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProgressAsync_Computes_Summary_From_Rows()
    {
        _progressRepository.GetProgressAsync(PlanId, ChapterId).Returns(new LetterPlanProgressQueryResultDataModel
        {
            PlanStartsOn = new DateTime(2026, 3, 1),
            Rows =
            [
                Row(StudentId, DocumentStatus.Approved),
                Row(StudentId + 1, DocumentStatus.Pending),
                Row(StudentId + 2, null),
                ExemptRow(StudentId + 3),
            ],
        });

        var result = await _service.GetProgressAsync(PlanId, ChapterId);

        result.Should().NotBeNull();
        result!.Summary.TotalRequired.Should().Be(3);
        result.Summary.Approved.Should().Be(1);
        result.Summary.InProgress.Should().Be(1);
        result.Summary.MissingOrRejected.Should().Be(1);
        result.Summary.CompletionPercent.Should().Be(33m);
    }

    [Fact]
    public async Task GetProgressAsync_Maps_Status_Labels()
    {
        _progressRepository.GetProgressAsync(PlanId, ChapterId).Returns(new LetterPlanProgressQueryResultDataModel
        {
            PlanStartsOn = new DateTime(2026, 3, 1),
            Rows = [Row(StudentId, DocumentStatus.PendingImprovement)],
        });

        var result = await _service.GetProgressAsync(PlanId, ChapterId);

        result!.Rows.Should().ContainSingle(r =>
            r.Status == LetterPlanDisplayStatus.PendingImprovement
            && r.StatusLabel == "Pendiente de mejora digital");
    }

    [Fact]
    public async Task ExemptStudentAsync_Returns_False_When_Reason_Is_Empty()
    {
        var result = await _service.ExemptStudentAsync(PlanId, StudentId, ChapterId, ManagerId, "  ");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExemptStudentAsync_Creates_Exemption_When_Valid()
    {
        _studentRepository.GetStudentChapterIdAsync(StudentId).Returns(ChapterId);
        _progressRepository.GetProgressAsync(PlanId, ChapterId).Returns(new LetterPlanProgressQueryResultDataModel
        {
            PlanStartsOn = new DateTime(2026, 3, 1),
            Rows = [Row(StudentId, null)],
        });
        _exemptionRepository.CreateExemptionAsync(
                StudentId, PlanId, ChapterId, "Motivo", ManagerId, Arg.Any<DateTime>())
            .Returns(true);

        var result = await _service.ExemptStudentAsync(PlanId, StudentId, ChapterId, ManagerId, "Motivo");

        result.Should().BeTrue();
    }

    private static LetterPlanProgressRowDataModel Row(int studentId, DocumentStatus? status) =>
        new()
        {
            StudentId = studentId,
            StudentFirstName = "Juan",
            StudentLastName = "García",
            FacilitatorFirstName = "Ana",
            FacilitatorLastName = "Pérez",
            SponsorshipId = 30,
            SponsorId = 20,
            RecipientName = "María López",
            LetterStatus = status,
        };

    private static LetterPlanProgressRowDataModel ExemptRow(int studentId) =>
        new()
        {
            StudentId = studentId,
            StudentFirstName = "Exento",
            StudentLastName = "Becario",
            FacilitatorFirstName = "Ana",
            FacilitatorLastName = "Pérez",
            SponsorshipId = 31,
            SponsorId = 20,
            RecipientName = "María López",
            IsExempt = true,
            ExemptionReason = "Motivo",
        };
}