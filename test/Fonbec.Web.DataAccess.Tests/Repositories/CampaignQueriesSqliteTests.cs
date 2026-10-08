using FluentAssertions;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

// Runs the shared campaign queries against SQLite so a reusable expression that only
// works on the InMemory provider cannot slip through.
public sealed class CampaignQueriesSqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<FonbecWebDbContext> _factory;

    public CampaignQueriesSqliteTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        _connection.Open();

        _factory = new SqliteDbContextFactory(_connection);

        using var db = _factory.CreateDbContext();
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Facilitator_Student_And_Plan_Queries_Translate()
    {
        var repository = new FacilitatorRepository(_factory, TimeProvider.System);

        var students = await repository.GetActiveSponsoredStudentsAsync(1);
        var plan = await repository.GetCurrentPlanForFacilitatorAsync(1);

        students.Should().BeEmpty();
        plan.Should().BeNull();
    }

    [Fact]
    public async Task Letter_Plan_Slot_Count_Translates()
    {
        var repository = new LetterPlanProgressRepository(_factory);

        var count = await repository.CountRequiredSlotsAsync(1, new DateTime(2026, 11, 1));

        count.Should().Be(0);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class SqliteDbContextFactory(SqliteConnection connection)
        : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<FonbecWebDbContext>()
                .UseSqlite(connection)
                .Options);

        public Task<FonbecWebDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}