using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class FacilitatorRepositoryGetMisBecariosDashboardTests
{
    private const int FacilitatorId = 2;
    private const int CompanyId = 40;
    private const int SponsorId = 20;
    private static readonly DateTime UtcNow = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetMisBecariosDashboardAsync_Returns_Empty_When_No_Students()
    {
        var factory = CreateDbContextFactory();
        var repository = new FacilitatorRepository(factory);

        var dashboard = await repository.GetMisBecariosDashboardAsync(FacilitatorId);

        dashboard.Students.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMisBecariosDashboardAsync_Returns_Student_With_Active_Sponsorship()
    {
        var factory = CreateDbContextFactory();
        await SeedBasicAsync(factory);
        var repository = new FacilitatorRepository(factory);

        var dashboard = await repository.GetMisBecariosDashboardAsync(FacilitatorId);

        dashboard.Students.Should().ContainSingle(s => s.StudentId == 10 && s.StudentFirstName == "Ana");
    }

    [Fact]
    public async Task GetMisBecariosDashboardAsync_Includes_Sponsors()
    {
        var factory = CreateDbContextFactory();
        await SeedBasicAsync(factory);
        var repository = new FacilitatorRepository(factory);

        var dashboard = await repository.GetMisBecariosDashboardAsync(FacilitatorId);

        var student = dashboard.Students.Single(s => s.StudentId == 10);
        student.Sponsors.Should().ContainSingle(s => s.SponsorId == SponsorId);
    }

    [Fact]
    public async Task GetMisBecariosDashboardAsync_Includes_Company_Sponsor()
    {
        var factory = CreateDbContextFactory();
        await SeedBasicAsync(factory, useCompanySponsorship: true);
        var repository = new FacilitatorRepository(factory);

        var dashboard = await repository.GetMisBecariosDashboardAsync(FacilitatorId);

        var student = dashboard.Students.Single(s => s.StudentId == 10);
        student.Sponsors.Should().ContainSingle(s => s.CompanyId == CompanyId && s.IsCompany);
    }

    [Fact]
    public async Task GetMisBecariosDashboardAsync_Sets_EducationLevel_Primary()
    {
        var factory = CreateDbContextFactory();
        await SeedBasicAsync(factory);
        var repository = new FacilitatorRepository(factory);

        var dashboard = await repository.GetMisBecariosDashboardAsync(FacilitatorId);

        dashboard.Students.Single(s => s.StudentId == 10).EducationLevel.Should().Be(EducationLevel.PrimarySchool);
    }

    [Fact]
    public async Task GetMisBecariosDashboardAsync_Sets_EducationLevel_Secondary()
    {
        var factory = CreateDbContextFactory();
        await SeedBasicAsync(factory, secondarySchoolStart: UtcNow.AddYears(-2));
        var repository = new FacilitatorRepository(factory);

        var dashboard = await repository.GetMisBecariosDashboardAsync(FacilitatorId);

        dashboard.Students.Single(s => s.StudentId == 10).EducationLevel.Should().Be(EducationLevel.SecondarySchool);
    }

    [Fact]
    public async Task GetMisBecariosDashboardAsync_Sets_EducationLevel_University()
    {
        var factory = CreateDbContextFactory();
        await SeedBasicAsync(factory, universityStart: UtcNow.AddYears(-1));
        var repository = new FacilitatorRepository(factory);

        var dashboard = await repository.GetMisBecariosDashboardAsync(FacilitatorId);

        dashboard.Students.Single(s => s.StudentId == 10).EducationLevel.Should().Be(EducationLevel.University);
    }

    [Fact]
    public async Task GetMisBecariosDashboardAsync_Excludes_Inactive_Student()
    {
        var factory = CreateDbContextFactory();
        await SeedBasicAsync(factory, disableStudent: true);
        var repository = new FacilitatorRepository(factory);

        var dashboard = await repository.GetMisBecariosDashboardAsync(FacilitatorId);

        dashboard.Students.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMisBecariosDashboardAsync_Excludes_Deleted_Student()
    {
        var factory = CreateDbContextFactory();
        await SeedBasicAsync(factory, markStudentDeleted: true);
        var repository = new FacilitatorRepository(factory);

        var dashboard = await repository.GetMisBecariosDashboardAsync(FacilitatorId);

        dashboard.Students.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMisBecariosDashboardAsync_Excludes_Student_Of_Another_Facilitator()
    {
        var factory = CreateDbContextFactory();
        await SeedBasicAsync(factory, facilitatorId: 99);
        var repository = new FacilitatorRepository(factory);

        var dashboard = await repository.GetMisBecariosDashboardAsync(FacilitatorId);

        dashboard.Students.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMisBecariosDashboardAsync_Orders_By_FirstName_Then_LastName()
    {
        var factory = CreateDbContextFactory();
        await SeedTwoStudentsAsync(factory);
        var repository = new FacilitatorRepository(factory);

        var dashboard = await repository.GetMisBecariosDashboardAsync(FacilitatorId);

        dashboard.Students.Should().HaveCount(2);
        dashboard.Students[0].StudentFirstName.Should().Be("Ana");
        dashboard.Students[1].StudentFirstName.Should().Be("Zoe");
    }

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private static async Task SeedTwoStudentsAsync(TestDbContextFactory factory)
    {
        await SeedBasicAsync(factory, studentId: 10, studentFirstName: "Zoe", studentLastName: "Zulu", sponsorshipId: 30);

        await using var db = await factory.CreateDbContextAsync();

        db.Set<Student>().Add(new Student
        {
            Id = 11,
            FirstName = "Ana",
            LastName = "Alpha",
            Gender = Gender.Unknown,
            ChapterId = 1,
            FacilitatorId = FacilitatorId,
            CreatedById = 1,
            CreatedOnUtc = UtcNow,
        });

        db.Set<Sponsorship>().Add(new Sponsorship
        {
            Id = 31,
            StudentId = 11,
            SponsorId = SponsorId,
            StartDate = UtcNow.AddMonths(-1),
            EndDate = UtcNow.AddMonths(1),
            CreatedById = 1,
            CreatedOnUtc = UtcNow,
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedBasicAsync(
        TestDbContextFactory factory,
        int studentId = 10,
        int facilitatorId = FacilitatorId,
        bool disableStudent = false,
        bool markStudentDeleted = false,
        bool useCompanySponsorship = false,
        string studentFirstName = "Ana",
        string studentLastName = "Becaria",
        int sponsorshipId = 30,
        DateTime? secondarySchoolStart = null,
        DateTime? universityStart = null)
    {
        await using var db = await factory.CreateDbContextAsync();

        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new FonbecWebUser
                {
                    Id = 1,
                    UserName = "manager",
                    NormalizedUserName = "MANAGER",
                    Email = "manager@fonbec.test",
                    NormalizedEmail = "MANAGER@FONBEC.TEST",
                    FirstName = "Manager",
                    LastName = "User",
                    SecurityStamp = Guid.NewGuid().ToString(),
                },
                new FonbecWebUser
                {
                    Id = FacilitatorId,
                    UserName = "facilitator",
                    NormalizedUserName = "FACILITATOR",
                    Email = "facilitator@fonbec.test",
                    NormalizedEmail = "FACILITATOR@FONBEC.TEST",
                    FirstName = "Mediador",
                    LastName = "Uno",
                    SecurityStamp = Guid.NewGuid().ToString(),
                });
        }

        if (!await db.Set<Chapter>().AnyAsync())
        {
            db.Set<Chapter>().Add(new Chapter
            {
                Id = 1,
                Name = "Chapter",
                CreatedById = 1,
                CreatedOnUtc = UtcNow,
                IsActive = true,
            });
        }

        if (!await db.Set<Company>().AnyAsync())
        {
            db.Set<Company>().Add(new Company
            {
                Id = CompanyId,
                Name = "Acme Corp",
                CreatedById = 1,
                CreatedOnUtc = UtcNow,
            });
        }

        if (!await db.Set<Sponsor>().AnyAsync())
        {
            db.Set<Sponsor>().Add(new Sponsor
            {
                Id = SponsorId,
                FirstName = "Padrino",
                LastName = "Activo",
                Email = "padrino@fonbec.test",
                Gender = Gender.Unknown,
                ChapterId = 1,
                CreatedById = 1,
                CreatedOnUtc = UtcNow,
            });
        }

        db.Set<Student>().Add(new Student
        {
            Id = studentId,
            FirstName = studentFirstName,
            LastName = studentLastName,
            Gender = Gender.Unknown,
            ChapterId = 1,
            FacilitatorId = facilitatorId,
            SecondarySchoolStartYear = secondarySchoolStart,
            UniversityStartYear = universityStart,
            CreatedById = 1,
            CreatedOnUtc = UtcNow,
        });

        db.Set<Sponsorship>().Add(new Sponsorship
        {
            Id = sponsorshipId,
            StudentId = studentId,
            SponsorId = useCompanySponsorship ? null : SponsorId,
            CompanyId = useCompanySponsorship ? CompanyId : null,
            StartDate = UtcNow.AddMonths(-1),
            EndDate = UtcNow.AddMonths(1),
            CreatedById = 1,
            CreatedOnUtc = UtcNow,
        });

        await db.SaveChangesAsync();

        if (disableStudent)
        {
            var student = await db.Set<Student>().SingleAsync(s => s.Id == studentId);
            student.DisabledById = 1;
            await db.SaveChangesAsync();
        }

        if (markStudentDeleted)
        {
            var student = await db.Set<Student>().SingleAsync(s => s.Id == studentId);
            student.IsDeleted = true;
            await db.SaveChangesAsync();
        }
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
