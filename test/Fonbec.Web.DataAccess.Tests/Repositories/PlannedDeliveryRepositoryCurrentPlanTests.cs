using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class PlannedDeliveryRepositoryCurrentPlanTests
{
    private const int ChapterId = 1;
    private const int OtherChapterId = 2;
    private static readonly DateTime UtcNow = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetCurrentPlanAsync_Returns_Incomplete_Plan_For_Chapter()
    {
        var factory = CreateDbContextFactory();
        await SeedPlanAsync(factory, planId: 100, chapterId: ChapterId, completed: false, startsOn: UtcNow.AddMonths(-1));
        await SeedPlanAsync(factory, planId: 101, chapterId: ChapterId, completed: true, startsOn: UtcNow.AddMonths(-2));
        var repository = new PlannedDeliveryRepository(factory);

        var current = await repository.GetCurrentPlanAsync(ChapterId);

        current.Should().NotBeNull();
        current!.PlannedDeliveryId.Should().Be(100);
        current.IsPlannedDeliveryCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetCurrentPlanAsync_Returns_Null_When_Only_Completed_Plans_Exist()
    {
        var factory = CreateDbContextFactory();
        await SeedPlanAsync(factory, planId: 100, chapterId: ChapterId, completed: true, startsOn: UtcNow);
        var repository = new PlannedDeliveryRepository(factory);

        var current = await repository.GetCurrentPlanAsync(ChapterId);

        current.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentPlanAsync_Ignores_Other_Chapter()
    {
        var factory = CreateDbContextFactory();
        await SeedPlanAsync(factory, planId: 100, chapterId: OtherChapterId, completed: false, startsOn: UtcNow);
        var repository = new PlannedDeliveryRepository(factory);

        var current = await repository.GetCurrentPlanAsync(ChapterId);

        current.Should().BeNull();
    }

    [Fact]
    public async Task GetCompletedPlansAsync_Returns_Only_Completed_For_Chapter()
    {
        var factory = CreateDbContextFactory();
        await SeedPlanAsync(factory, planId: 100, chapterId: ChapterId, completed: true, startsOn: UtcNow.AddMonths(-1));
        await SeedPlanAsync(factory, planId: 101, chapterId: ChapterId, completed: false, startsOn: UtcNow);
        await SeedPlanAsync(factory, planId: 102, chapterId: OtherChapterId, completed: true, startsOn: UtcNow.AddMonths(-2));
        var repository = new PlannedDeliveryRepository(factory);

        var completed = await repository.GetCompletedPlansAsync(ChapterId);

        completed.Should().ContainSingle(p => p.PlannedDeliveryId == 100);
    }

    [Fact]
    public async Task GetLatestCompletedPlanAsync_Returns_Most_Recent_Completed()
    {
        var factory = CreateDbContextFactory();
        await SeedPlanAsync(factory, planId: 100, chapterId: ChapterId, completed: true, startsOn: UtcNow.AddMonths(-2));
        await SeedPlanAsync(factory, planId: 101, chapterId: ChapterId, completed: true, startsOn: UtcNow.AddMonths(-1));
        await SeedPlanAsync(factory, planId: 102, chapterId: ChapterId, completed: false, startsOn: UtcNow);
        var repository = new PlannedDeliveryRepository(factory);

        var latest = await repository.GetLatestCompletedPlanAsync(ChapterId);

        latest.Should().NotBeNull();
        latest!.PlannedDeliveryId.Should().Be(101);
    }

    [Fact]
    public async Task HasIncompletePlanAsync_Reflects_Current_Plan()
    {
        var factory = CreateDbContextFactory();
        await SeedPlanAsync(factory, planId: 100, chapterId: ChapterId, completed: false, startsOn: UtcNow);
        var repository = new PlannedDeliveryRepository(factory);

        (await repository.HasIncompletePlanAsync(ChapterId)).Should().BeTrue();
        (await repository.HasIncompletePlanAsync(OtherChapterId)).Should().BeFalse();
    }

    [Fact]
    public async Task MarkPlanCompletedAsync_Sets_Completed()
    {
        var factory = CreateDbContextFactory();
        await SeedPlanAsync(factory, planId: 100, chapterId: ChapterId, completed: false, startsOn: UtcNow);
        var repository = new PlannedDeliveryRepository(factory);

        var marked = await repository.MarkPlanCompletedAsync(100);

        marked.Should().BeTrue();
        (await repository.GetCurrentPlanAsync(ChapterId)).Should().BeNull();
        (await repository.GetCompletedPlansAsync(ChapterId)).Should().ContainSingle(p => p.PlannedDeliveryId == 100);
    }

    [Fact]
    public async Task MarkPlanCompletedAsync_Is_Idempotent_When_Already_Completed()
    {
        var factory = CreateDbContextFactory();
        await SeedPlanAsync(factory, planId: 100, chapterId: ChapterId, completed: true, startsOn: UtcNow);
        var repository = new PlannedDeliveryRepository(factory);

        var marked = await repository.MarkPlanCompletedAsync(100);

        marked.Should().BeFalse();
    }

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private static async Task SeedPlanAsync(
        TestDbContextFactory factory,
        int planId,
        int chapterId,
        bool completed,
        DateTime startsOn)
    {
        await using var db = await factory.CreateDbContextAsync();

        if (!await db.Set<Chapter>().AnyAsync(c => c.Id == chapterId))
        {
            db.Set<Chapter>().Add(new Chapter
            {
                Id = chapterId,
                Name = $"Chapter {chapterId}",
                CreatedById = 1,
                CreatedOnUtc = UtcNow,
                IsActive = true,
            });
        }

        if (!await db.Users.AnyAsync())
        {
            db.Users.Add(new FonbecWebUser
            {
                Id = 1,
                UserName = "manager",
                NormalizedUserName = "MANAGER",
                Email = "manager@fonbec.test",
                NormalizedEmail = "MANAGER@FONBEC.TEST",
                FirstName = "Manager",
                LastName = "User",
                SecurityStamp = Guid.NewGuid().ToString(),
            });
        }

        db.Set<PlannedDelivery>().Add(new PlannedDelivery
        {
            Id = planId,
            ChapterId = chapterId,
            StartsOn = startsOn,
            Completed = completed,
            CreatedById = 1,
            CreatedOnUtc = UtcNow,
        });

        await db.SaveChangesAsync();
    }

    private sealed class TestDbContextFactory(string databaseName) : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() =>
            new(CreateOptions());

        public Task<FonbecWebDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());

        private DbContextOptions<FonbecWebDbContext> CreateOptions() =>
            new DbContextOptionsBuilder<FonbecWebDbContext>()
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
    }
}