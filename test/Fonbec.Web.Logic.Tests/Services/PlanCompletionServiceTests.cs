using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.LetterPlanProgress;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class PlanCompletionServiceTests
{
    private const int PlanId = 100;
    private const int ChapterId = 1;
    private const int StudentId = 10;
    private const int CompletedByUserId = 42;

    private readonly ILetterPlanProgressRepository _progressRepository =
        Substitute.For<ILetterPlanProgressRepository>();

    private readonly IPlannedDeliveryRepository _plannedDeliveryRepository =
        Substitute.For<IPlannedDeliveryRepository>();

    private readonly PlanCompletionService _service;

    public PlanCompletionServiceTests()
    {
        _service = new PlanCompletionService(_progressRepository, _plannedDeliveryRepository);
        _plannedDeliveryRepository
            .SetPlanCompletedAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<int>())
            .Returns(true);
    }

    [Fact]
    public async Task GetReadiness_Is_Ready_When_All_Required_Approved()
    {
        SetupProgress(isCompleted: false,
            Row(StudentId, DocumentStatus.Approved),
            Row(StudentId + 1, DocumentStatus.Approved));

        var result = await _service.GetReadinessAsync(PlanId, ChapterId, TestContext.Current.CancellationToken);

        result.PlanFound.Should().BeTrue();
        result.IsReadyToComplete.Should().BeTrue();
        result.IsCompleted.Should().BeFalse();
        result.TotalRequired.Should().Be(2);
        result.ApprovedCount.Should().Be(2);
    }

    [Fact]
    public async Task GetReadiness_Is_Not_Ready_When_Slots_Pending()
    {
        SetupProgress(isCompleted: false,
            Row(StudentId, DocumentStatus.Approved),
            Row(StudentId + 1, null));

        var result = await _service.GetReadinessAsync(PlanId, ChapterId, TestContext.Current.CancellationToken);

        result.IsReadyToComplete.Should().BeFalse();
        result.TotalRequired.Should().Be(2);
        result.ApprovedCount.Should().Be(1);
    }

    [Fact]
    public async Task GetReadiness_Is_Not_Ready_When_No_Slots()
    {
        SetupProgress(isCompleted: false);

        var result = await _service.GetReadinessAsync(PlanId, ChapterId, TestContext.Current.CancellationToken);

        result.IsReadyToComplete.Should().BeFalse();
        result.SlotCount.Should().Be(0);
    }

    [Fact]
    public async Task GetReadiness_Is_Ready_When_All_Students_Exempt()
    {
        SetupProgress(isCompleted: false,
            ExemptRow(StudentId),
            ExemptRow(StudentId + 1));

        var result = await _service.GetReadinessAsync(PlanId, ChapterId, TestContext.Current.CancellationToken);

        result.IsReadyToComplete.Should().BeTrue();
        result.SlotCount.Should().Be(2);
        result.TotalRequired.Should().Be(0);
    }

    [Fact]
    public async Task GetReadiness_Is_Ready_When_Remaining_Students_Are_Exempt()
    {
        SetupProgress(isCompleted: false,
            Row(StudentId, DocumentStatus.Approved),
            ExemptRow(StudentId + 1));

        var result = await _service.GetReadinessAsync(PlanId, ChapterId, TestContext.Current.CancellationToken);

        result.IsReadyToComplete.Should().BeTrue();
        result.TotalRequired.Should().Be(1);
    }

    [Fact]
    public async Task CompletePlan_Succeeds_When_Ready()
    {
        SetupProgress(isCompleted: false,
            Row(StudentId, DocumentStatus.Approved),
            ExemptRow(StudentId + 1));

        var result = await _service.CompletePlanAsync(PlanId, ChapterId, CompletedByUserId, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        result.StatusChanged.Should().BeTrue();
        await _plannedDeliveryRepository.Received(1).SetPlanCompletedAsync(PlanId, true, CompletedByUserId);
    }

    [Fact]
    public async Task CompletePlan_Is_Idempotent_When_Already_Completed()
    {
        SetupProgress(isCompleted: true,
            Row(StudentId, DocumentStatus.Approved));

        var result = await _service.CompletePlanAsync(PlanId, ChapterId, CompletedByUserId, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        result.AlreadyCompleted.Should().BeTrue();
        result.StatusChanged.Should().BeFalse();
        await _plannedDeliveryRepository.DidNotReceive()
            .SetPlanCompletedAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<int>());
    }

    [Fact]
    public async Task CompletePlan_Rejects_When_Pending()
    {
        SetupProgress(isCompleted: false,
            Row(StudentId, DocumentStatus.Approved),
            Row(StudentId + 1, null));

        var result = await _service.CompletePlanAsync(PlanId, ChapterId, CompletedByUserId, TestContext.Current.CancellationToken);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(PlanCompletionService.PlanNotReady);
        await _plannedDeliveryRepository.DidNotReceive()
            .SetPlanCompletedAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<int>());
    }

    [Fact]
    public async Task CompletePlan_Never_Reopens()
    {
        SetupProgress(isCompleted: true,
            Row(StudentId, DocumentStatus.Approved),
            Row(StudentId + 1, null));

        var result = await _service.CompletePlanAsync(PlanId, ChapterId, CompletedByUserId, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        result.AlreadyCompleted.Should().BeTrue();
        await _plannedDeliveryRepository.DidNotReceive()
            .SetPlanCompletedAsync(Arg.Any<int>(), false, Arg.Any<int>());
    }

    [Fact]
    public async Task CompletePlan_Returns_Error_When_Plan_Not_Found()
    {
        _progressRepository.GetProgressAsync(PlanId, ChapterId)
            .Returns((LetterPlanProgressQueryResultDataModel?)null);

        var result = await _service.CompletePlanAsync(PlanId, ChapterId, CompletedByUserId, TestContext.Current.CancellationToken);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(PlanCompletionService.PlanNotFound);
    }

    private void SetupProgress(bool isCompleted, params LetterPlanProgressRowDataModel[] rows)
    {
        _progressRepository.GetProgressAsync(PlanId, ChapterId).Returns(new LetterPlanProgressQueryResultDataModel
        {
            PlanStartsOn = new DateTime(2026, 3, 1),
            IsPlanCompleted = isCompleted,
            Rows = rows.ToList(),
        });
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
