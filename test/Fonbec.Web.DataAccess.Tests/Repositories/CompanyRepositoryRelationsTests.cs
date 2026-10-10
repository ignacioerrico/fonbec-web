using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class CompanyRepositoryRelationsTests
{
    [Fact]
    public async Task UpdateCompanyRelationsAsync_Updates_Contacts_And_Linked_Sponsors()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory);
        var repository = new CompanyRepository(factory);

        var result = await repository.UpdateCompanyRelationsAsync(new UpdateCompanyRelationsInputDataModel
        {
            CompanyId = 1,
            UpdatedById = 1,
            Contacts =
            [
                new UpdateCompanyContactInputDataModel
                {
                    Id = 1,
                    FirstName = "Johnny",
                    LastName = "Doe",
                    Email = "johnny@acme.test",
                },
                new UpdateCompanyContactInputDataModel
                {
                    FirstName = "Pedro",
                    LastName = "Ruiz",
                },
            ],
            SponsorIds = [2],
        });

        result.CompanyFound.Should().BeTrue();
        result.AffectedRows.Should().BeGreaterThan(0);
        result.MissingSponsorIds.Should().BeNull();

        await using var db = await factory.CreateDbContextAsync(Token);
        var contacts = await db.Set<PointOfContact>().OrderBy(p => p.Id).ToListAsync(Token);
        contacts.Should().ContainSingle(p => p.Id == 1 && p.FirstName == "Johnny" && p.Email == "johnny@acme.test" && p.IsActive);
        contacts.Should().ContainSingle(p => p.Id == 2 && !p.IsActive && p.DisabledById == 1);
        contacts.Should().ContainSingle(p => p.FirstName == "Pedro" && p.IsActive && p.CompanyId == 1);

        var sponsors = await db.Set<Sponsor>().OrderBy(s => s.Id).ToListAsync(Token);
        sponsors.Single(s => s.Id == 1).CompanyId.Should().BeNull();
        sponsors.Single(s => s.Id == 2).CompanyId.Should().Be(1);
        sponsors.Single(s => s.Id == 3).CompanyId.Should().Be(2);
    }

    [Fact]
    public async Task GetCompanyRelationsAsync_Returns_Active_Contacts_And_Linked_Sponsors()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory);
        var repository = new CompanyRepository(factory);

        var relations = await repository.GetCompanyRelationsAsync(1);

        relations.Should().NotBeNull();
        relations!.CompanyName.Should().Be("Acme");
        relations.Contacts.Select(contact => contact.FirstName).Should().Equal("Jane", "John");
        relations.Sponsors.Select(sponsor => sponsor.FullName).Should().Equal("Alice Johnson");

        var available = await repository.GetSponsorsAvailableToLinkAsync();
        available.Select(sponsor => sponsor.FullName).Should().Equal("Bob Brown");
    }

    [Fact]
    public async Task UpdateCompanyRelationsAsync_Rejects_Unknown_Contacts_Without_Changes()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory);
        var repository = new CompanyRepository(factory);

        var result = await repository.UpdateCompanyRelationsAsync(new UpdateCompanyRelationsInputDataModel
        {
            CompanyId = 1,
            UpdatedById = 1,
            Contacts = [new UpdateCompanyContactInputDataModel { Id = 99, FirstName = "Nope" }],
            SponsorIds = [],
        });

        result.CompanyFound.Should().BeTrue();
        result.HasUnknownContacts.Should().BeTrue();
        result.AffectedRows.Should().Be(0);

        await using var db = await factory.CreateDbContextAsync(Token);
        (await db.Set<PointOfContact>().SingleAsync(p => p.Id == 1, Token)).IsActive.Should().BeTrue();
        (await db.Set<Sponsor>().SingleAsync(s => s.Id == 1, Token)).CompanyId.Should().Be(1);
    }

    [Fact]
    public async Task UpdateCompanyRelationsAsync_Rejects_Sponsors_Already_Linked_Elsewhere()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory);
        var repository = new CompanyRepository(factory);

        var result = await repository.UpdateCompanyRelationsAsync(new UpdateCompanyRelationsInputDataModel
        {
            CompanyId = 1,
            UpdatedById = 1,
            Contacts =
            [
                new UpdateCompanyContactInputDataModel { Id = 1, FirstName = "John", LastName = "Doe" },
                new UpdateCompanyContactInputDataModel { Id = 2, FirstName = "Jane", LastName = "Smith" },
            ],
            SponsorIds = [1, 3],
        });

        result.CompanyFound.Should().BeTrue();
        result.MissingSponsorIds.Should().Equal(3);
        result.AffectedRows.Should().Be(0);

        await using var db = await factory.CreateDbContextAsync(Token);
        (await db.Set<Sponsor>().SingleAsync(s => s.Id == 1, Token)).CompanyId.Should().Be(1);
        (await db.Set<Sponsor>().SingleAsync(s => s.Id == 3, Token)).CompanyId.Should().Be(2);
        (await db.Set<PointOfContact>().SingleAsync(p => p.Id == 2, Token)).IsActive.Should().BeTrue();
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private static async Task SeedAsync(TestDbContextFactory factory)
    {
        await using var db = await factory.CreateDbContextAsync(Token);

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

        db.Set<Chapter>().Add(new Chapter
        {
            Id = 1,
            Name = "Chapter",
            CreatedById = 1,
            CreatedOnUtc = DateTime.UtcNow,
            IsActive = true,
        });

        db.Set<Company>().AddRange(
            new Company { Id = 1, Name = "Acme", CreatedById = 1, CreatedOnUtc = DateTime.UtcNow, IsActive = true, PublicAccessToken = Guid.NewGuid() },
            new Company { Id = 2, Name = "Other", CreatedById = 1, CreatedOnUtc = DateTime.UtcNow, IsActive = true, PublicAccessToken = Guid.NewGuid() });

        db.Set<PointOfContact>().AddRange(
            new PointOfContact { Id = 1, FirstName = "John", LastName = "Doe", CompanyId = 1, CreatedById = 1, CreatedOnUtc = DateTime.UtcNow, IsActive = true },
            new PointOfContact { Id = 2, FirstName = "Jane", LastName = "Smith", CompanyId = 1, CreatedById = 1, CreatedOnUtc = DateTime.UtcNow, IsActive = true });

        db.Set<Sponsor>().AddRange(
            Sponsor(1, "Alice", "Johnson", companyId: 1),
            Sponsor(2, "Bob", "Brown", companyId: null),
            Sponsor(3, "Carol", "Diaz", companyId: 2));

        await db.SaveChangesAsync(Token);
    }

    private static Sponsor Sponsor(int id, string firstName, string lastName, int? companyId) =>
        new()
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Gender = Gender.Unknown,
            ChapterId = 1,
            CompanyId = companyId,
            Email = $"{firstName.ToLowerInvariant()}@fonbec.test",
            PublicAccessToken = Guid.NewGuid(),
            CreatedById = 1,
            CreatedOnUtc = DateTime.UtcNow,
            IsActive = true,
        };

    private sealed class TestDbContextFactory(string databaseName) : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<FonbecWebDbContext>()
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        public Task<FonbecWebDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}