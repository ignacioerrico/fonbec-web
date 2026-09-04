using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class FacilitatorRepositoryGetLatestReportCardsTests
{
    [Fact]
    public async Task GetLatestReportCardsAsync_Returns_ReportCard_For_Requested_Student()
    {
        var factory = CreateDbContextFactory();

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        db.Set<ReportCard>().Add(new ReportCard
        {
            DocumentId = 1,
            StudentId = 10,
            Period = new DateOnly(2026, 6, 1),
            Description = "June report card",
            Status = DocumentStatus.Approved,
            RowVersion = [1]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new FacilitatorRepository(factory, TimeProvider.System);
        var result = await repository.GetLatestReportCardsAsync([10], 3);

        result.Should().ContainSingle();
        result.Single().StudentId.Should().Be(10);
        result.Single().Description.Should().Be("June report card");
    }

    [Fact]
    public async Task GetLatestReportCardsAsync_Returns_Only_Latest_Three_ReportCards_For_Student()
    {
        var factory = CreateDbContextFactory();
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        db.Set<ReportCard>().AddRange(
            new ReportCard
            {
                DocumentId = 1,
                StudentId = 10,
                Period = new DateOnly(2026, 3, 1),
                Description = "March report",
                Status = DocumentStatus.Approved,
                RowVersion = [1]
            },
            new ReportCard
            {
                DocumentId = 2,
                StudentId = 10,
                Period = new DateOnly(2026, 4, 1),
                Description = "April report",
                Status = DocumentStatus.Approved,
                RowVersion = [1]
            },
            new ReportCard
            {
                DocumentId = 3,
                StudentId = 10,
                Period = new DateOnly(2026, 5, 1),
                Description = "May report",
                Status = DocumentStatus.Approved,
                RowVersion = [1]
            },
            new ReportCard
            {
                DocumentId = 4,
                StudentId = 10,
                Period = new DateOnly(2026, 6, 1),
                Description = "June report",
                Status = DocumentStatus.Approved,
                RowVersion = [1]
            }
        );

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new FacilitatorRepository(
            factory,
            TimeProvider.System);

        var result = await repository.GetLatestReportCardsAsync([10], 3);

        result.Should().HaveCount(3);

        result.Select(r => r.Period)
            .Should()
            .Equal(
                new DateOnly(2026, 6, 1),
                new DateOnly(2026, 5, 1),
                new DateOnly(2026, 4, 1));
    }

    [Fact]
    public async Task GetLatestReportCardsAsync_Returns_Only_Latest_Three_Per_Student()
    {
        // Arrange
        var factory = CreateDbContextFactory();

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        db.Set<ReportCard>().AddRange(
            // Student 10
            new ReportCard
            {
                DocumentId = 1,
                StudentId = 10,
                Period = new DateOnly(2026, 3, 1),
                Description = "March - Student 10",
                Status = DocumentStatus.Approved,
                RowVersion = [1]
            },
            new ReportCard
            {
                DocumentId = 2,
                StudentId = 10,
                Period = new DateOnly(2026, 4, 1),
                Description = "April - Student 10",
                Status = DocumentStatus.Approved,
                RowVersion = [1]
            },
            new ReportCard
            {
                DocumentId = 3,
                StudentId = 10,
                Period = new DateOnly(2026, 5, 1),
                Description = "May - Student 10",
                Status = DocumentStatus.Approved,
                RowVersion = [1]
            },
            new ReportCard
            {
                DocumentId = 4,
                StudentId = 10,
                Period = new DateOnly(2026, 6, 1),
                Description = "June - Student 10",
                Status = DocumentStatus.Approved,
                RowVersion = [1]
            },

            // Student 11
            new ReportCard
            {
                DocumentId = 5,
                StudentId = 11,
                Period = new DateOnly(2026, 3, 1),
                Description = "March - Student 11",
                Status = DocumentStatus.Approved,
                RowVersion = [1]
            },
            new ReportCard
            {
                DocumentId = 6,
                StudentId = 11,
                Period = new DateOnly(2026, 4, 1),
                Description = "April - Student 11",
                Status = DocumentStatus.Approved,
                RowVersion = [1]
            },
            new ReportCard
            {
                DocumentId = 7,
                StudentId = 11,
                Period = new DateOnly(2026, 5, 1),
                Description = "May - Student 11",
                Status = DocumentStatus.Approved,
                RowVersion = [1]
            },
            new ReportCard
            {
                DocumentId = 8,
                StudentId = 11,
                Period = new DateOnly(2026, 6, 1),
                Description = "June - Student 11",
                Status = DocumentStatus.Approved,
                RowVersion = [1]
            }
        );

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new FacilitatorRepository(
            factory,
            TimeProvider.System);

        var result = await repository.GetLatestReportCardsAsync([10, 11], 3);

        result.Should().HaveCount(6);

        result.Count(r => r.StudentId == 10).Should().Be(3);
        result.Count(r => r.StudentId == 11).Should().Be(3);
    }

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

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
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
    }
}