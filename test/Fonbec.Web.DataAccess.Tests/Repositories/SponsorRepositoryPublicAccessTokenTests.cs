using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class SponsorRepositoryPublicAccessTokenTests
{
    [Fact]
    public async Task Scenario27_SponsorPublicAccessToken_GeneratedOnCreate()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory);
        var repository = new SponsorRepository(factory);

        await repository.CreateSponsorAsync(new CreateSponsorInputDataModel
        {
            ChapterId = 1,
            SponsorFirstName = "New",
            SponsorLastName = "Sponsor",
            SponsorEmail = "new@sponsor.test",
            CreatedById = 1,
        });

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var sponsor = await db.Set<Sponsor>().OrderByDescending(s => s.Id).FirstAsync(TestContext.Current.CancellationToken);
        sponsor.PublicAccessToken.Should().NotBe(Guid.Empty);
        (await db.Set<Sponsor>().Select(s => s.PublicAccessToken).ToListAsync(TestContext.Current.CancellationToken))
            .Should().OnlyHaveUniqueItems();
    }

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private static async Task SeedAsync(TestDbContextFactory factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        var user = new FonbecWebUser
        {
            Id = 1,
            UserName = "admin",
            NormalizedUserName = "ADMIN",
            Email = "admin@test.com",
            NormalizedEmail = "ADMIN@TEST.COM",
            FirstName = "Admin",
            LastName = "User",
            SecurityStamp = Guid.NewGuid().ToString(),
        };
        db.Users.Add(user);
        db.Set<Chapter>().Add(new Chapter
        {
            Id = 1,
            Name = "Chapter",
            CreatedById = 1,
            CreatedOnUtc = DateTime.UtcNow,
            IsActive = true,
        });
        await db.SaveChangesAsync();
    }

    private sealed class TestDbContextFactory(string databaseName) : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() => new(CreateOptions());

        public Task<FonbecWebDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());

        private DbContextOptions<FonbecWebDbContext> CreateOptions() =>
            new DbContextOptionsBuilder<FonbecWebDbContext>()
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
    }
}