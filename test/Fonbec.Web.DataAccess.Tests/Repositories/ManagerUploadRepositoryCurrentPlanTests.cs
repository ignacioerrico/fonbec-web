using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class ManagerUploadRepositoryCurrentPlanTests
{
    private const int ChapterId = 1;
    private static readonly DateTime UtcNow = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetCurrentPlanForChapterAsync_Returns_Open_Plan_That_Has_Not_Started_Yet()
    {
        var factory = CreateDbContextFactory();
        await SeedPlanAsync(factory, planId: 100, startsOn: new DateTime(2026, 10, 1), completed: true);
        await SeedPlanAsync(factory, planId: 101, startsOn: new DateTime(2026, 11, 1), completed: false);
        var repository = new ManagerUploadRepository(factory, TimeProvider.System);

        var plan = await repository.GetCurrentPlanForChapterAsync(ChapterId);

        plan.Should().NotBeNull();
        plan!.PlanId.Should().Be(101);
        plan.StartsOn.Should().Be(new DateTime(2026, 11, 1));
    }

    [Fact]
    public async Task GetCurrentPlanForChapterAsync_Returns_Null_When_Only_Completed_Plans_Exist()
    {
        var factory = CreateDbContextFactory();
        await SeedPlanAsync(factory, planId: 100, startsOn: new DateTime(2026, 10, 1), completed: true);
        var repository = new ManagerUploadRepository(factory, TimeProvider.System);

        var plan = await repository.GetCurrentPlanForChapterAsync(ChapterId);

        plan.Should().BeNull();
    }

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private static async Task SeedPlanAsync(
        TestDbContextFactory factory,
        int planId,
        DateTime startsOn,
        bool completed)
    {
        await using var db = await factory.CreateDbContextAsync();

        db.Set<PlannedDelivery>().Add(new PlannedDelivery
        {
            Id = planId,
            ChapterId = ChapterId,
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
