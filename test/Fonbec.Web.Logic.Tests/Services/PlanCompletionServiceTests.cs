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
    private const int TriggeredByUserId = 42;

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

    // Scenario 1: Auto-complete when last letter is approved.
    [Fact]
    public async Task Completes_When_All_Required_Approved()
    {
        SetupProgress(isCompleted: false,
            Row(StudentId, DocumentStatus.Approved),
            Row(StudentId + 1, DocumentStatus.Approved),
            Row(StudentId + 2, DocumentStatus.Approved));

        var result = await _service.EvaluateAndUpdateAsync(PlanId, ChapterId, TriggeredByUserId, TestContext.Current.CancellationToken);

        result.IsComplete.Should().BeTrue();
        result.WasComplete.Should().BeFalse();
        result.StatusChanged.Should().BeTrue();
        result.TotalRequired.Should().Be(3);
        result.ApprovedCount.Should().Be(3);
        await _plannedDeliveryRepository.Received(1).SetPlanCompletedAsync(PlanId, true, TriggeredByUserId);
    }

    // Scenario 2: No change when letters still pending.
    [Fact]
    public async Task Does_Not_Complete_When_Slots_Pending()
    {
        SetupProgress(isCompleted: false,
            Row(StudentId, DocumentStatus.Approved),
            Row(StudentId + 1, DocumentStatus.Approved),
            Row(StudentId + 2, DocumentStatus.Approved),
            Row(StudentId + 3, DocumentStatus.Approved),
            Row(StudentId + 4, null));

        var result = await _service.EvaluateAndUpdateAsync(PlanId, ChapterId, TriggeredByUserId, TestContext.Current.CancellationToken);

        result.IsComplete.Should().BeFalse();
        result.StatusChanged.Should().BeFalse();
        result.TotalRequired.Should().Be(5);
        result.ApprovedCount.Should().Be(4);
        await _plannedDeliveryRepository.DidNotReceive()
            .SetPlanCompletedAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<int>());
    }

    // Scenario 3: Idempotent when already complete.
    [Fact]
    public async Task Is_Idempotent_When_Already_Completed()
    {
        SetupProgress(isCompleted: true,
            Row(StudentId, DocumentStatus.Approved),
            Row(StudentId + 1, DocumentStatus.Approved));

        var result = await _service.EvaluateAndUpdateAsync(PlanId, ChapterId, TriggeredByUserId, TestContext.Current.CancellationToken);

        result.IsComplete.Should().BeTrue();
        result.WasComplete.Should().BeTrue();
        result.StatusChanged.Should().BeFalse();
        await _plannedDeliveryRepository.DidNotReceive()
            .SetPlanCompletedAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<int>());
    }

    // Scenario 4: Do not auto-complete when no letters required.
    [Fact]
    public async Task Does_Not_Complete_When_No_Required_Slots()
    {
        SetupProgress(isCompleted: false);

        var result = await _service.EvaluateAndUpdateAsync(PlanId, ChapterId, TriggeredByUserId, TestContext.Current.CancellationToken);

        result.IsComplete.Should().BeFalse();
        result.TotalRequired.Should().Be(0);
        result.StatusChanged.Should().BeFalse();
        await _plannedDeliveryRepository.DidNotReceive()
            .SetPlanCompletedAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<int>());
    }

    // Scenario 5 (minimum scope): required count increased, one slot missing -> reopen.
    [Fact]
    public async Task Reopens_When_New_Slot_Is_Missing()
    {
        SetupProgress(isCompleted: true,
            Row(StudentId, DocumentStatus.Approved),
            Row(StudentId + 1, DocumentStatus.Approved),
            Row(StudentId + 2, null));

        var result = await _service.EvaluateAndUpdateAsync(PlanId, ChapterId, TriggeredByUserId, TestContext.Current.CancellationToken);

        result.IsComplete.Should().BeFalse();
        result.WasComplete.Should().BeTrue();
        result.StatusChanged.Should().BeTrue();
        await _plannedDeliveryRepository.Received(1).SetPlanCompletedAsync(PlanId, false, TriggeredByUserId);
    }

    // Scenario 6: Reopen when a required slot has a rejected letter without replacement.
    [Fact]
    public async Task Reopens_When_Slot_Rejected()
    {
        SetupProgress(isCompleted: true,
            Row(StudentId, DocumentStatus.Approved),
            Row(StudentId + 1, DocumentStatus.Rejected));

        var result = await _service.EvaluateAndUpdateAsync(PlanId, ChapterId, TriggeredByUserId, TestContext.Current.CancellationToken);

        result.IsComplete.Should().BeFalse();
        result.StatusChanged.Should().BeTrue();
        await _plannedDeliveryRepository.Received(1).SetPlanCompletedAsync(PlanId, false, TriggeredByUserId);
    }

    // Scenario 7: Re-complete after rejection and successful re-upload.
    [Fact]
    public async Task Re_Completes_After_Replacement_Approved()
    {
        SetupProgress(isCompleted: false,
            Row(StudentId, DocumentStatus.Approved),
            Row(StudentId + 1, DocumentStatus.Approved));

        var result = await _service.EvaluateAndUpdateAsync(PlanId, ChapterId, TriggeredByUserId, TestContext.Current.CancellationToken);

        result.IsComplete.Should().BeTrue();
        result.StatusChanged.Should().BeTrue();
        await _plannedDeliveryRepository.Received(1).SetPlanCompletedAsync(PlanId, true, TriggeredByUserId);
    }

    // Scenario 8b: Exempt students excluded from required slots; remaining non-exempt approved -> complete.
    [Fact]
    public async Task Completes_When_Remaining_Students_Are_Exempt()
    {
        SetupProgress(isCompleted: false,
            Row(StudentId, DocumentStatus.Approved),
            ExemptRow(StudentId + 1),
            ExemptRow(StudentId + 2));

        var result = await _service.EvaluateAndUpdateAsync(PlanId, ChapterId, TriggeredByUserId, TestContext.Current.CancellationToken);

        result.IsComplete.Should().BeTrue();
        result.TotalRequired.Should().Be(1);
        result.ApprovedCount.Should().Be(1);
        result.StatusChanged.Should().BeTrue();
        await _plannedDeliveryRepository.Received(1).SetPlanCompletedAsync(PlanId, true, TriggeredByUserId);
    }

    // Scenario 8c: Revoking an exemption reintroduces an unapproved slot -> reopen.
    [Fact]
    public async Task Reopens_When_Reintroduced_Slot_Unapproved()
    {
        SetupProgress(isCompleted: true,
            Row(StudentId, DocumentStatus.Approved),
            Row(StudentId + 1, null));

        var result = await _service.EvaluateAndUpdateAsync(PlanId, ChapterId, TriggeredByUserId, TestContext.Current.CancellationToken);

        result.IsComplete.Should().BeFalse();
        result.StatusChanged.Should().BeTrue();
        await _plannedDeliveryRepository.Received(1).SetPlanCompletedAsync(PlanId, false, TriggeredByUserId);
    }

    // Scenario 8d: All students exempt (TotalRequired = 0) does not auto-complete.
    [Fact]
    public async Task Does_Not_Complete_When_All_Students_Exempt()
    {
        SetupProgress(isCompleted: false,
            ExemptRow(StudentId),
            ExemptRow(StudentId + 1));

        var result = await _service.EvaluateAndUpdateAsync(PlanId, ChapterId, TriggeredByUserId, TestContext.Current.CancellationToken);

        result.IsComplete.Should().BeFalse();
        result.TotalRequired.Should().Be(0);
        result.StatusChanged.Should().BeFalse();
        await _plannedDeliveryRepository.DidNotReceive()
            .SetPlanCompletedAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<int>());
    }

    // Scenario 9: Evaluation uses same counts as the progress UI.
    [Fact]
    public async Task Counts_Match_Progress_Summary()
    {
        SetupProgress(isCompleted: false,
            Row(StudentId, DocumentStatus.Approved),
            Row(StudentId + 1, DocumentStatus.Pending),
            Row(StudentId + 2, null),
            ExemptRow(StudentId + 3));

        var result = await _service.EvaluateAndUpdateAsync(PlanId, ChapterId, TriggeredByUserId, TestContext.Current.CancellationToken);

        result.TotalRequired.Should().Be(3);
        result.ApprovedCount.Should().Be(1);
        result.IsComplete.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_Empty_Result_When_Plan_Not_Found()
    {
        _progressRepository.GetProgressAsync(PlanId, ChapterId)
            .Returns((LetterPlanProgressQueryResultDataModel?)null);

        var result = await _service.EvaluateAndUpdateAsync(PlanId, ChapterId, TriggeredByUserId, TestContext.Current.CancellationToken);

        result.IsComplete.Should().BeFalse();
        result.StatusChanged.Should().BeFalse();
        await _plannedDeliveryRepository.DidNotReceive()
            .SetPlanCompletedAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<int>());
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