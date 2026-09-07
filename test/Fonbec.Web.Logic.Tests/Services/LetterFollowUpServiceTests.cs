using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.LetterFollowUp;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.LetterFollowUp;
using Fonbec.Web.Logic.Services;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class LetterFollowUpServiceTests
{
    private const int ChapterId = 4;
    private const int ManagerId = 12;
    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 6, 3, 0, 0, TimeSpan.Zero);

    private readonly ILetterFollowUpRepository _repository =
        Substitute.For<ILetterFollowUpRepository>();

    private readonly LetterFollowUpService _service;

    public LetterFollowUpServiceTests()
    {
        _service = new LetterFollowUpService(
            _repository,
            new FixedTimeProvider(UtcNow));
    }

    [Fact]
    public async Task GetOpenTasksAsync_Maps_Dual_Flags_As_Independent_Tasks()
    {
        _repository.GetOpenTasksAsync(ChapterId).Returns(new LetterFollowUpQueryResultDataModel
        {
            RedFlags =
            [
                Task(10, "Juan", "García", "Problema", RedFlagPriority.High),
            ],
            GreenFlags =
            [
                Task(10, "Juan", "García", "Reconocimiento"),
            ],
        });

        var result = await _service.GetOpenTasksAsync(ChapterId);

        result.RedFlags.Should().ContainSingle(task =>
            task.AssessmentId == 10
            && task.Kind == LetterFollowUpTaskKind.RedFlag
            && task.Comment == "Problema"
            && task.ReviewerFullName == "Rita Revisora"
            && task.ReviewerEmail == "rita@example.org");
        result.GreenFlags.Should().ContainSingle(task =>
            task.AssessmentId == 10
            && task.Kind == LetterFollowUpTaskKind.GreenFlag
            && task.Comment == "Reconocimiento");
    }

    [Fact]
    public async Task GetOpenTasksAsync_Orders_Red_Flags_By_Descending_Priority()
    {
        _repository.GetOpenTasksAsync(ChapterId).Returns(new LetterFollowUpQueryResultDataModel
        {
            RedFlags =
            [
                Task(1, "Baja", "Becario", "Baja", RedFlagPriority.Low),
                Task(2, "Alta", "Becario", "Alta", RedFlagPriority.High),
                Task(3, "Media", "Becario", "Media", RedFlagPriority.Medium),
            ],
        });

        var result = await _service.GetOpenTasksAsync(ChapterId);

        result.RedFlags.Select(task => task.Priority).Should().Equal(
            RedFlagPriority.High,
            RedFlagPriority.Medium,
            RedFlagPriority.Low);
    }

    [Fact]
    public async Task GetOpenTasksAsync_Orders_Same_Priority_By_Newest_Report_First()
    {
        _repository.GetOpenTasksAsync(ChapterId).Returns(new LetterFollowUpQueryResultDataModel
        {
            RedFlags =
            [
                Task(1, "Vieja", "Becario", "Vieja", RedFlagPriority.High,
                    reportedOn: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)),
                Task(2, "Nueva", "Becario", "Nueva", RedFlagPriority.High,
                    reportedOn: new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc)),
            ],
            GreenFlags =
            [
                Task(3, "Vieja", "Becario", "Vieja",
                    reportedOn: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)),
                Task(4, "Nueva", "Becario", "Nueva",
                    reportedOn: new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc)),
            ],
        });

        var result = await _service.GetOpenTasksAsync(ChapterId);

        result.RedFlags.Select(task => task.AssessmentId).Should().Equal(2, 1);
        result.GreenFlags.Select(task => task.AssessmentId).Should().Equal(4, 3);
    }

    [Fact]
    public async Task GetOpenTasksAsync_Formats_Reported_Date_In_Spanish()
    {
        _repository.GetOpenTasksAsync(ChapterId).Returns(new LetterFollowUpQueryResultDataModel
        {
            RedFlags =
            [
                Task(1, "Juan", "García", "Problema", RedFlagPriority.High,
                    reportedOn: new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc)),
            ],
        });

        var result = await _service.GetOpenTasksAsync(ChapterId);

        result.RedFlags.Single().ReportedOnText.Should().Be("3 de agosto de 2026");
    }

    [Theory]
    [InlineData(LetterFollowUpTaskKind.RedFlag)]
    [InlineData(LetterFollowUpTaskKind.GreenFlag)]
    public async Task MarkTaskResolvedAsync_Writes_Audit_For_Requested_Kind(
        LetterFollowUpTaskKind kind)
    {
        const long assessmentId = 42;
        _repository.ResolveRedFlagAsync(
                assessmentId, ChapterId, ManagerId, UtcNow.UtcDateTime)
            .Returns(true);
        _repository.ResolveGreenFlagAsync(
                assessmentId, ChapterId, ManagerId, UtcNow.UtcDateTime)
            .Returns(true);

        var result = await _service.MarkTaskResolvedAsync(
            assessmentId, kind, ChapterId, ManagerId);

        result.Should().BeTrue();
        if (kind == LetterFollowUpTaskKind.RedFlag)
        {
            await _repository.Received(1).ResolveRedFlagAsync(
                assessmentId, ChapterId, ManagerId, UtcNow.UtcDateTime);
            await _repository.DidNotReceiveWithAnyArgs().ResolveGreenFlagAsync(
                default, default, default, default);
        }
        else
        {
            await _repository.Received(1).ResolveGreenFlagAsync(
                assessmentId, ChapterId, ManagerId, UtcNow.UtcDateTime);
            await _repository.DidNotReceiveWithAnyArgs().ResolveRedFlagAsync(
                default, default, default, default);
        }
    }

    [Fact]
    public async Task SetRedFlagPriorityAsync_Persists_Valid_Priority()
    {
        _repository.SetRedFlagPriorityAsync(42, ChapterId, RedFlagPriority.High)
            .Returns(true);

        var result = await _service.SetRedFlagPriorityAsync(
            42, ChapterId, RedFlagPriority.High);

        result.Should().BeTrue();
        await _repository.Received(1).SetRedFlagPriorityAsync(
            42, ChapterId, RedFlagPriority.High);
    }

    [Fact]
    public async Task SetRedFlagPriorityAsync_Rejects_Unknown_Priority()
    {
        var result = await _service.SetRedFlagPriorityAsync(
            42, ChapterId, (RedFlagPriority)99);

        result.Should().BeFalse();
        await _repository.DidNotReceiveWithAnyArgs().SetRedFlagPriorityAsync(
            default, default, default);
    }

    private static LetterFollowUpTaskDataModel Task(
        long assessmentId,
        string studentFirstName,
        string studentLastName,
        string comment,
        RedFlagPriority? priority = null,
        DateTime? reportedOn = null) =>
        new()
        {
            AssessmentId = assessmentId,
            StudentFirstName = studentFirstName,
            StudentLastName = studentLastName,
            FacilitatorFirstName = "Ana",
            FacilitatorLastName = "Pérez",
            FacilitatorEmail = "ana@example.org",
            ReviewerFirstName = "Rita",
            ReviewerLastName = "Revisora",
            ReviewerEmail = "rita@example.org",
            ReportedOn = reportedOn ?? new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc),
            Comment = comment,
            Priority = priority,
        };

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}