using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class CompanyRepositoryGetAllTests
{
    [Fact]
    public async Task GetAllCompaniesAsync_Excludes_Inactive_And_Deleted_PointsOfContact_And_Sponsors()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory);
        var repository = new CompanyRepository(factory);

        var companies = await repository.GetAllCompaniesAsync();

        companies.Should().ContainSingle(c => c.CompanyId == 1);
        var company = companies.Single(c => c.CompanyId == 1);

        company.CompanyPointsOfContact.Should().ContainSingle(p => p.FirstName == "John" && p.LastName == "Doe");
        company.CompanySponsors.Should().ContainSingle(s => s.FirstName == "Alice" && s.LastName == "Johnson");
    }

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private static async Task SeedAsync(TestDbContextFactory factory)
    {
        await using var db = await factory.CreateDbContextAsync();

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

        db.Set<Company>().Add(new Company
        {
            Id = 1,
            Name = "Acme Corp",
            CreatedById = 1,
            CreatedOnUtc = DateTime.UtcNow,
            IsActive = true,
        });

        db.Set<PointOfContact>().AddRange(
            new PointOfContact
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                CompanyId = 1,
                CreatedById = 1,
                CreatedOnUtc = DateTime.UtcNow,
                IsActive = true,
            },
            new PointOfContact
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Smith",
                CompanyId = 1,
                CreatedById = 1,
                CreatedOnUtc = DateTime.UtcNow,
                IsActive = false,
            });

        db.Set<Sponsor>().AddRange(
            new Sponsor
            {
                Id = 1,
                FirstName = "Alice",
                LastName = "Johnson",
                Gender = Gender.Unknown,
                ChapterId = 1,
                CompanyId = 1,
                CreatedById = 1,
                CreatedOnUtc = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false,
            },
            new Sponsor
            {
                Id = 2,
                FirstName = "Bob",
                LastName = "Brown",
                Gender = Gender.Unknown,
                ChapterId = 1,
                CompanyId = 1,
                CreatedById = 1,
                CreatedOnUtc = DateTime.UtcNow,
                IsActive = false,
                IsDeleted = false,
            },
            new Sponsor
            {
                Id = 3,
                FirstName = "Charlie",
                LastName = "Delta",
                Gender = Gender.Unknown,
                ChapterId = 1,
                CompanyId = 1,
                CreatedById = 1,
                CreatedOnUtc = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = true,
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
