using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class LetterExemptionRepositoryTests
{
    private const int StudentId = 10;
    private const int PlanId = 100;
    private const int ChapterId = 1;
    private static readonly DateTime UtcNow = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task IsActiveExemptionAsync_Returns_True_For_Active_Exemption()
    {
        var factory = CreateDbContextFactory();
        await SeedExemptionAsync(factory, studentId: StudentId, planId: PlanId, isRevoked: false);
        var repository = new LetterExemptionRepository(factory);

        var result = await repository.IsActiveExemptionAsync(StudentId, PlanId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsActiveExemptionAsync_Returns_False_When_Revoked()
    {
        var factory = CreateDbContextFactory();
        await SeedExemptionAsync(factory, studentId: StudentId, planId: PlanId, isRevoked: true);
        var repository = new LetterExemptionRepository(factory);

        var result = await repository.IsActiveExemptionAsync(StudentId, PlanId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsActiveExemptionAsync_Returns_False_For_Other_Plan()
    {
        var factory = CreateDbContextFactory();
        await SeedExemptionAsync(factory, studentId: StudentId, planId: PlanId, isRevoked: false);
        var repository = new LetterExemptionRepository(factory);

        var result = await repository.IsActiveExemptionAsync(StudentId, PlanId + 1);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveExemptStudentIdsForPlanAsync_Returns_Only_Active_For_Plan()
    {
        var factory = CreateDbContextFactory();
        await SeedExemptionAsync(factory, id: 1, studentId: 10, planId: PlanId, isRevoked: false);
        await SeedExemptionAsync(factory, id: 2, studentId: 11, planId: PlanId, isRevoked: true);
        await SeedExemptionAsync(factory, id: 3, studentId: 12, planId: PlanId + 1, isRevoked: false);
        await SeedExemptionAsync(factory, id: 4, studentId: 13, planId: PlanId, isRevoked: false);
        var repository = new LetterExemptionRepository(factory);

        var result = await repository.GetActiveExemptStudentIdsForPlanAsync(PlanId);

        result.Should().BeEquivalentTo([10, 13]);
    }

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private static async Task SeedExemptionAsync(
        TestDbContextFactory factory,
        int studentId,
        int planId,
        bool isRevoked,
        int id = 1)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.Set<LetterExemption>().Add(new LetterExemption
        {
            Id = id,
            StudentId = studentId,
            PlannedDeliveryId = planId,
            ChapterId = ChapterId,
            Reason = "Motivo",
            CreatedByFonbecUserId = 1,
            CreatedOnUtc = UtcNow,
            IsRevoked = isRevoked,
            RevokedByFonbecUserId = isRevoked ? 1 : null,
            RevokedOnUtc = isRevoked ? UtcNow : null,
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