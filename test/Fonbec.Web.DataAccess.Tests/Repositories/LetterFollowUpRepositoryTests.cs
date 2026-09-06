using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class LetterFollowUpRepositoryTests
{
    private const int ChapterId = 1;
    private const int OtherChapterId = 2;
    private const int ManagerId = 90;
    private static readonly DateTime ResolvedOn =
        new(2026, 9, 6, 3, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetOpenTasksAsync_Returns_Dual_Flags_And_Orders_Red_By_Priority()
    {
        var factory = CreateDbContextFactory();
        await SeedReviewAsync(
            factory, 1, ChapterId, DocumentStatus.Approved,
            hasRedFlag: true, RedFlagPriority.Low, hasGreenFlag: true);
        await SeedReviewAsync(
            factory, 2, ChapterId, DocumentStatus.Approved,
            hasRedFlag: true, RedFlagPriority.High, hasGreenFlag: false);
        await SeedReviewAsync(
            factory, 3, ChapterId, DocumentStatus.Approved,
            hasRedFlag: true, RedFlagPriority.Medium, hasGreenFlag: false);

        var result = await new LetterFollowUpRepository(factory)
            .GetOpenTasksAsync(ChapterId);

        result.RedFlags.Select(task => task.Priority).Should().Equal(
            RedFlagPriority.High,
            RedFlagPriority.Medium,
            RedFlagPriority.Low);
        result.GreenFlags.Should().ContainSingle(task =>
            task.AssessmentId == 1
            && task.Comment == "Comentario verde 1"
            && task.ReviewerFirstName == "Rita"
            && task.ReviewerEmail == "rita1@example.org");
    }

    [Fact]
    public async Task GetOpenTasksAsync_Orders_Same_Priority_By_Newest_Report_First()
    {
        var factory = CreateDbContextFactory();
        await SeedReviewAsync(
            factory, 1, ChapterId, DocumentStatus.Approved,
            hasRedFlag: true, RedFlagPriority.High, hasGreenFlag: true,
            reviewedOn: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
        await SeedReviewAsync(
            factory, 2, ChapterId, DocumentStatus.Approved,
            hasRedFlag: true, RedFlagPriority.High, hasGreenFlag: true,
            reviewedOn: new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc));

        var result = await new LetterFollowUpRepository(factory)
            .GetOpenTasksAsync(ChapterId);

        result.RedFlags.Select(task => task.AssessmentId).Should().Equal(2, 1);
        result.GreenFlags.Select(task => task.AssessmentId).Should().Equal(2, 1);
        result.RedFlags[0].ReportedOn.Should().Be(
            new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetOpenTasksAsync_Excludes_Resolved_NonApproved_And_OtherChapter_Tasks()
    {
        var factory = CreateDbContextFactory();
        await SeedReviewAsync(
            factory, 1, ChapterId, DocumentStatus.Approved,
            hasRedFlag: true, RedFlagPriority.High, hasGreenFlag: true,
            redResolved: true, greenResolved: true);
        await SeedReviewAsync(
            factory, 2, ChapterId, DocumentStatus.Pending,
            hasRedFlag: true, RedFlagPriority.High, hasGreenFlag: true);
        await SeedReviewAsync(
            factory, 3, OtherChapterId, DocumentStatus.Approved,
            hasRedFlag: true, RedFlagPriority.High, hasGreenFlag: true);

        var result = await new LetterFollowUpRepository(factory)
            .GetOpenTasksAsync(ChapterId);

        result.RedFlags.Should().BeEmpty();
        result.GreenFlags.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveRedFlagAsync_Sets_Audit_Without_Resolving_Green_Flag()
    {
        var factory = CreateDbContextFactory();
        await SeedReviewAsync(
            factory, 1, ChapterId, DocumentStatus.Approved,
            hasRedFlag: true, RedFlagPriority.High, hasGreenFlag: true);
        var repository = new LetterFollowUpRepository(factory);

        var result = await repository.ResolveRedFlagAsync(
            1, ChapterId, ManagerId, ResolvedOn);

        result.Should().BeTrue();
        await using var db = await factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken);
        var assessment = await db.Set<Assessment>().SingleAsync(
            TestContext.Current.CancellationToken);
        assessment.IsRedFlagResolved.Should().BeTrue();
        assessment.RedFlagResolvedById.Should().Be(ManagerId);
        assessment.RedFlagResolvedOn.Should().Be(ResolvedOn);
        assessment.IsGreenFlagResolved.Should().BeFalse();
        assessment.GreenFlagResolvedById.Should().BeNull();
    }

    [Fact]
    public async Task ResolveGreenFlagAsync_Denies_Other_Chapter()
    {
        var factory = CreateDbContextFactory();
        await SeedReviewAsync(
            factory, 1, OtherChapterId, DocumentStatus.Approved,
            hasRedFlag: false, priority: null, hasGreenFlag: true);

        var result = await new LetterFollowUpRepository(factory)
            .ResolveGreenFlagAsync(1, ChapterId, ManagerId, ResolvedOn);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetRedFlagPriorityAsync_Updates_Open_Task()
    {
        var factory = CreateDbContextFactory();
        await SeedReviewAsync(
            factory, 1, ChapterId, DocumentStatus.Approved,
            hasRedFlag: true, RedFlagPriority.Low, hasGreenFlag: false);
        var repository = new LetterFollowUpRepository(factory);

        var result = await repository.SetRedFlagPriorityAsync(
            1, ChapterId, RedFlagPriority.High);

        result.Should().BeTrue();
        await using var db = await factory.CreateDbContextAsync(
            TestContext.Current.CancellationToken);
        (await db.Set<Assessment>().SingleAsync(TestContext.Current.CancellationToken))
            .RedFlagPriority.Should().Be(RedFlagPriority.High);
    }

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private static async Task SeedReviewAsync(
        TestDbContextFactory factory,
        long assessmentId,
        int chapterId,
        DocumentStatus status,
        bool hasRedFlag,
        RedFlagPriority? priority,
        bool hasGreenFlag,
        bool redResolved = false,
        bool greenResolved = false,
        DateTime? reviewedOn = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        var id = checked((int)assessmentId);
        var facilitator = new FonbecWebUser
        {
            Id = 1000 + id,
            FirstName = "Ana",
            LastName = $"Mediadora {id}",
            Email = $"ana{id}@example.org",
            UserName = $"ana{id}@example.org",
        };
        var reviewer = new FonbecWebUser
        {
            Id = 3000 + id,
            FirstName = "Rita",
            LastName = $"Revisora {id}",
            Email = $"rita{id}@example.org",
            UserName = $"rita{id}@example.org",
        };
        var student = new Student
        {
            Id = 2000 + id,
            FirstName = $"Becario {id}",
            LastName = "Apellido",
            ChapterId = chapterId,
            FacilitatorId = facilitator.Id,
            Facilitator = facilitator,
            CreatedById = facilitator.Id,
            CreatedBy = facilitator,
            IsActive = true,
        };
        var assessment = new Assessment
        {
            AssessmentId = assessmentId,
            HasRedFlags = hasRedFlag,
            RedFlagPriority = priority,
            IssuesNotes = $"Comentario rojo {id}",
            HasGreenFlags = hasGreenFlag,
            Appraisal = $"Comentario verde {id}",
            IsRedFlagResolved = redResolved,
            IsGreenFlagResolved = greenResolved,
        };
        var letter = new Letter
        {
            DocumentId = assessmentId,
            DocumentType = DocumentType.Letter,
            ChapterId = chapterId,
            StudentId = student.Id,
            Student = student,
            PlanId = 1,
            UploadedById = facilitator.Id,
            UploadedBy = facilitator,
            UploadedOn = ResolvedOn.AddDays(-1),
            Status = status,
            RowVersion = [1],
        };
        var review = new LetterReview
        {
            LetterReviewId = assessmentId,
            DocumentId = letter.DocumentId,
            Document = letter,
            AssessmentId = assessmentId,
            Assessment = assessment,
            ReviewedById = reviewer.Id,
            ReviewedBy = reviewer,
            ReviewedOn = reviewedOn ?? ResolvedOn.AddHours(-1),
        };
        letter.Review = review;
        assessment.LetterReview = review;

        db.Set<LetterReview>().Add(review);
        await db.SaveChangesAsync();
    }

    private sealed class TestDbContextFactory(string databaseName)
        : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() =>
            new(CreateOptions());

        public Task<FonbecWebDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());

        private DbContextOptions<FonbecWebDbContext> CreateOptions() =>
            new DbContextOptionsBuilder<FonbecWebDbContext>()
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(warning =>
                    warning.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
    }
}