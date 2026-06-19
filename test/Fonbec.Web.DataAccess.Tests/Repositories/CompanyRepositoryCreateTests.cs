using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class CompanyRepositoryCreateTests
{
    [Fact]
    public async Task CreateCompanyAsync_Does_Not_Persist_When_Sponsor_Is_Missing()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, sponsorId: 1);
        var repository = new CompanyRepository(factory);

        var result = await repository.CreateCompanyAsync(new CreateCompanyInputDataModel
        {
            CompanyName = "Acme Corp",
            CreatedById = 1,
            SponsorIds = [1, 999],
        });

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        result.MissingSponsorIds.Should().BeEquivalentTo([999]);
        result.CompanyId.Should().Be(0);
        db.Set<Company>().Should().BeEmpty();
        var sponsor = await db.Set<Sponsor>()
            .SingleAsync(s => s.Id == 1, TestContext.Current.CancellationToken);
        sponsor.CompanyId.Should().BeNull();
    }

    [Fact]
    public async Task CreateCompanyAsync_Links_Sponsors_Atomically()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, sponsorId: 1, secondSponsorId: 2);
        var repository = new CompanyRepository(factory);

        var result = await repository.CreateCompanyAsync(new CreateCompanyInputDataModel
        {
            CompanyName = "Acme Corp",
            CreatedById = 1,
            SponsorIds = [1, 2],
        });

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        result.CompanyId.Should().BeGreaterThan(0);
        db.Set<Company>().Should().ContainSingle(c => c.Name == "Acme Corp");
        int sponsorsInCompany = await db.Set<Sponsor>()
            .Where(s => s.CompanyId == result.CompanyId)
            .CountAsync(TestContext.Current.CancellationToken);
        sponsorsInCompany.Should().Be(2);
    }

    [Fact]
    public async Task CreateCompanyAsync_Treats_Deleted_Sponsor_As_Unavailable()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, sponsorId: 1, isSponsorDeleted: true);
        var repository = new CompanyRepository(factory);

        var result = await repository.CreateCompanyAsync(new CreateCompanyInputDataModel
        {
            CompanyName = "Acme Corp",
            CreatedById = 1,
            SponsorIds = [1],
        });

        result.MissingSponsorIds.Should().BeEquivalentTo([1]);
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.Set<Company>().Should().BeEmpty();
    }

    [Fact]
    public async Task CreateCompanyAsync_Deduplicates_Sponsor_Ids()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, sponsorId: 1);
        var repository = new CompanyRepository(factory);

        var result = await repository.CreateCompanyAsync(new CreateCompanyInputDataModel
        {
            CompanyName = "Acme Corp",
            CreatedById = 1,
            SponsorIds = [1, 1],
        });

        result.CompanyId.Should().BeGreaterThan(0);
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var sponsor = await db.Set<Sponsor>()
            .SingleAsync(s => s.Id == 1, TestContext.Current.CancellationToken);
        sponsor.CompanyId.Should().Be(result.CompanyId);
    }

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private static async Task SeedAsync(
        TestDbContextFactory factory,
        int sponsorId = 1,
        int? secondSponsorId = null,
        bool isSponsorDeleted = false)
    {
        await using var db = await factory.CreateDbContextAsync();

        var user = new FonbecWebUser
        {
            Id = 1,
            UserName = "manager",
            NormalizedUserName = "MANAGER",
            Email = "manager@fonbec.test",
            NormalizedEmail = "MANAGER@FONBEC.TEST",
            FirstName = "Manager",
            LastName = "User",
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        var chapter = new Chapter
        {
            Id = 1,
            Name = "Chapter",
            CreatedById = 1,
            CreatedOnUtc = DateTime.UtcNow,
            IsActive = true,
        };

        db.Users.Add(user);
        db.Set<Chapter>().Add(chapter);

        db.Set<Sponsor>().Add(new Sponsor
        {
            Id = sponsorId,
            FirstName = "Alice",
            LastName = "Johnson",
            ChapterId = 1,
            CreatedById = 1,
            CreatedOnUtc = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = isSponsorDeleted,
        });

        if (secondSponsorId.HasValue)
        {
            db.Set<Sponsor>().Add(new Sponsor
            {
                Id = secondSponsorId.Value,
                FirstName = "Bob",
                LastName = "Brown",
                ChapterId = 1,
                CreatedById = 1,
                CreatedOnUtc = DateTime.UtcNow,
                IsActive = true,
            });
        }

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