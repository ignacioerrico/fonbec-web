using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class CompanyRepositoryGetAllTests
{
    [Fact]
    public async Task GetAllCompaniesAsync_Excludes_Inactive_And_Deleted_PointsOfContact_And_Sponsors()
    {
        using var factory = CreateDbContextFactory();
        await SeedAsync(factory);
        var repository = new CompanyRepository(factory);

        var companies = await repository.GetAllCompaniesAsync();

        companies.Should().ContainSingle(c => c.CompanyId == 1);
        var company = companies.Single(c => c.CompanyId == 1);

        company.CompanyPointsOfContact.Should().ContainSingle(p => p.FirstName == "John" && p.LastName == "Doe");
        company.CompanySponsors.Should().ContainSingle(s => s.FirstName == "Alice" && s.LastName == "Johnson");
    }

    [Fact]
    public async Task GetAllCompaniesAsync_Includes_Students_Sponsored_Directly_Or_Through_Active_Sponsors()
    {
        using var factory = CreateDbContextFactory();
        await SeedAsync(factory);
        await SeedSponsorshipsAsync(factory);
        var repository = new CompanyRepository(factory);

        var companies = await repository.GetAllCompaniesAsync();

        var students = companies.Single(c => c.CompanyId == 1).SponsoredStudents;
        students.Select(s => (s.Name, s.SponsorName)).Should().Equal(
            ("Ana Perez", null),
            ("Ana Perez", "Alice Johnson"),
            ("Luis Gomez", "Alice Johnson"),
            ("Luis Gomez", "Alice Johnson"));
        students[2].EndDate.Should().NotBeNull();
        students[3].StartDate.Should().BeAfter(DateTime.UtcNow);
        students.Should().NotContain(s => s.Name == "Inactive Student");
    }

    private static TestDbContextFactory CreateDbContextFactory() =>
        new();

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
            CreatedOnUtc = DateTime.UtcNow,
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
            PublicAccessToken = Guid.NewGuid(),
        });

        var jane = new PointOfContact
        {
            Id = 2,
            FirstName = "Jane",
            LastName = "Smith",
            CompanyId = 1,
            CreatedById = 1,
            CreatedOnUtc = DateTime.UtcNow,
        };
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
            jane);

        var bob = new Sponsor
        {
            Id = 2,
            FirstName = "Bob",
            LastName = "Brown",
            Gender = Gender.Unknown,
            ChapterId = 1,
            CompanyId = 1,
            CreatedById = 1,
            CreatedOnUtc = DateTime.UtcNow,
            IsDeleted = false,
            PublicAccessToken = Guid.NewGuid(),
            Email = "bob@fonbec.test",
        };
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
                PublicAccessToken = Guid.NewGuid(),
                Email = "alice@fonbec.test",
            },
            bob,
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
                PublicAccessToken = Guid.NewGuid(),
                Email = "charlie@fonbec.test",
            });

        await db.SaveChangesAsync();

        // New rows are stored as active. Disabling is what clears IsActive.
        jane.DisabledById = 1;
        bob.DisabledById = 1;
        await db.SaveChangesAsync();
    }

    private static async Task SeedSponsorshipsAsync(TestDbContextFactory factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;

        var inactiveStudent = new Student
        {
            Id = 12,
            FirstName = "Inactive",
            LastName = "Student",
            Gender = Gender.Unknown,
            ChapterId = 1,
            FacilitatorId = 1,
            CreatedById = 1,
            CreatedOnUtc = now,
        };
        db.Set<Student>().AddRange(
            new Student
            {
                Id = 10,
                FirstName = "Luis",
                LastName = "Gomez",
                Gender = Gender.Unknown,
                ChapterId = 1,
                FacilitatorId = 1,
                CreatedById = 1,
                CreatedOnUtc = now,
                IsActive = true,
            },
            new Student
            {
                Id = 11,
                FirstName = "Ana",
                LastName = "Perez",
                Gender = Gender.Unknown,
                ChapterId = 1,
                FacilitatorId = 1,
                CreatedById = 1,
                CreatedOnUtc = now,
                IsActive = true,
            },
            inactiveStudent);

        db.Set<Sponsorship>().AddRange(
            new Sponsorship
            {
                Id = 1,
                CompanyId = 1,
                StudentId = 11,
                StartDate = now.AddMonths(-2),
                IsActive = true,
                CreatedById = 1,
                CreatedOnUtc = now,
            },
            new Sponsorship
            {
                Id = 2,
                SponsorId = 1,
                StudentId = 11,
                StartDate = now.AddMonths(-1),
                IsActive = true,
                CreatedById = 1,
                CreatedOnUtc = now,
            },
            new Sponsorship
            {
                Id = 3,
                SponsorId = 1,
                StudentId = 10,
                StartDate = now.AddYears(1),
                IsActive = true,
                CreatedById = 1,
                CreatedOnUtc = now,
            },
            new Sponsorship
            {
                Id = 4,
                SponsorId = 2,
                StudentId = 10,
                StartDate = now.AddMonths(-6),
                EndDate = now.AddMonths(-1),
                IsActive = true,
                CreatedById = 1,
                CreatedOnUtc = now,
            },
            new Sponsorship
            {
                Id = 5,
                SponsorId = 1,
                StudentId = 12,
                StartDate = now.AddMonths(-3),
                IsActive = true,
                CreatedById = 1,
                CreatedOnUtc = now,
            },
            new Sponsorship
            {
                Id = 6,
                SponsorId = 1,
                StudentId = 10,
                StartDate = now.AddYears(-3),
                CreatedById = 1,
                CreatedOnUtc = now,
            },
            new Sponsorship
            {
                Id = 7,
                SponsorId = 1,
                StudentId = 10,
                StartDate = now.AddYears(-2),
                EndDate = now.AddMonths(-3),
                IsActive = true,
                CreatedById = 1,
                CreatedOnUtc = now,
            });

        await db.SaveChangesAsync();

        inactiveStudent.DisabledById = 1;
        var inactiveSponsorship = await db.Set<Sponsorship>().SingleAsync(sp => sp.Id == 6);
        inactiveSponsorship.DisabledById = 1;
        await db.SaveChangesAsync();
    }

    private sealed class TestDbContextFactory : IDbContextFactory<FonbecWebDbContext>, IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:;Foreign Keys=False");

        public TestDbContextFactory()
        {
            _connection.Open();
            _connection.CreateFunction("GETUTCDATE", () => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            using var db = CreateDbContext();
            db.Database.EnsureCreated();
        }

        public FonbecWebDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<FonbecWebDbContext>()
                .UseSqlite(_connection)
                .Options);

        public Task<FonbecWebDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());

        public void Dispose() => _connection.Dispose();
    }
}
