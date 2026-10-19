using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class SponsorRepositoryNotesTests
{
    [Theory]
    [InlineData("Updated notes", false)]
    [InlineData("Updated notes", true)]
    [InlineData(null, false)]
    [InlineData(null, true)]
    public async Task UpdateSponsorAsync_PersistsNotesAndClearing_WithOrWithoutOtherEdits(
        string? notes, bool changeName)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var factory = new TestDbContextFactory(connection);
        var sponsorId = await SeedAsync(factory);
        var repository = new SponsorRepository(factory);

        var affected = await repository.UpdateSponsorAsync(new UpdateSponsorInputDataModel
        {
            SponsorId = sponsorId,
            SponsorFirstName = "First",
            SponsorLastName = changeName ? "Changed" : "Last",
            SponsorEmail = "sponsor@example.test",
            SponsorNotes = notes,
            UpdatedById = 1,
        });

        affected.Should().Be(1);
        await using var verification = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var saved = await verification.Set<Sponsor>().SingleAsync(TestContext.Current.CancellationToken);
        saved.Notes.Should().Be(notes);
        saved.LastName.Should().Be(changeName ? "Changed" : "Last");
        saved.LastUpdatedById.Should().Be(1);
        saved.LastUpdatedOnUtc.Should().NotBeNull();
        saved.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateSponsorAsync_DoesNotChangeNotesForInactiveOrMissingSponsor(bool missing)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var factory = new TestDbContextFactory(connection);
        var sponsorId = await SeedAsync(factory);
        if (!missing)
        {
            await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
            var sponsor = await db.Set<Sponsor>().SingleAsync(TestContext.Current.CancellationToken);
            sponsor.DisabledById = 1;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var affected = await new SponsorRepository(factory).UpdateSponsorAsync(new UpdateSponsorInputDataModel
        {
            SponsorId = missing ? sponsorId + 100 : sponsorId,
            SponsorNotes = "Must not persist",
            UpdatedById = 1,
        });

        affected.Should().Be(0);
        await using var verification = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await verification.Set<Sponsor>().SingleAsync(TestContext.Current.CancellationToken))
            .Notes.Should().Be("Original notes");
    }

    private static async Task<int> SeedAsync(TestDbContextFactory factory)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        db.Users.Add(new FonbecWebUser
        {
            Id = 1, UserName = "local-test", FirstName = "Test", LastName = "Admin", CreatedOnUtc = DateTime.UtcNow,
        });
        db.Set<Chapter>().Add(new Chapter { Id = 1, Name = "Test", CreatedById = 1 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await new SponsorRepository(factory).CreateSponsorAsync(new CreateSponsorInputDataModel
        {
            ChapterId = 1,
            SponsorFirstName = "First",
            SponsorLastName = "Last",
            SponsorEmail = "sponsor@example.test",
            SponsorNotes = "Original notes",
            CreatedById = 1,
        });
        return await db.Set<Sponsor>().Select(sponsor => sponsor.Id).SingleAsync(TestContext.Current.CancellationToken);
    }

    private sealed class TestDbContextFactory(SqliteConnection connection) : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<FonbecWebDbContext>().UseSqlite(connection).Options);

        public Task<FonbecWebDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
