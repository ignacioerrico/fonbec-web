using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class FacilitatorRepositoryGetActiveSponsoredStudentsTests
{
    private const int FacilitatorId = 2;
    private const int CompanyId = 40;
    private const int SponsorId = 20;
    private static readonly DateTime UtcNow = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetActiveSponsoredStudentsAsync_Returns_Student_With_Current_Sponsorship()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, studentId: 10, sponsorshipStart: UtcNow.AddMonths(-1), sponsorshipEnd: UtcNow.AddMonths(1));
        var repository = new FacilitatorRepository(factory);

        var students = await repository.GetActiveSponsoredStudentsAsync(FacilitatorId);

        students.Should().ContainSingle(s => s.StudentId == 10 && s.StudentFirstName == "Ana");
    }

    [Fact]
    public async Task GetActiveSponsoredStudentsAsync_Returns_Student_With_Open_Ended_Sponsorship()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, studentId: 10, sponsorshipStart: UtcNow.AddMonths(-1), sponsorshipEnd: null);
        var repository = new FacilitatorRepository(factory);

        var students = await repository.GetActiveSponsoredStudentsAsync(FacilitatorId);

        students.Should().ContainSingle(s => s.StudentId == 10);
    }

    [Fact]
    public async Task GetActiveSponsoredStudentsAsync_Returns_Student_With_Current_Company_Sponsorship()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, studentId: 10, useCompanySponsorship: true);
        var repository = new FacilitatorRepository(factory);

        var students = await repository.GetActiveSponsoredStudentsAsync(FacilitatorId);

        students.Should().ContainSingle(s => s.StudentId == 10);
    }

    [Fact]
    public async Task GetActiveSponsoredStudentsAsync_Maps_Student_NickName()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, studentId: 10, studentNickName: "Anita");
        var repository = new FacilitatorRepository(factory);

        var students = await repository.GetActiveSponsoredStudentsAsync(FacilitatorId);

        students.Should().ContainSingle(s => s.StudentNickName == "Anita");
    }

    [Fact]
    public async Task GetActiveSponsoredStudentsAsync_Orders_By_FirstName_Then_LastName()
    {
        var factory = CreateDbContextFactory();
        await SeedTwoStudentsAsync(factory);
        var repository = new FacilitatorRepository(factory);

        var students = await repository.GetActiveSponsoredStudentsAsync(FacilitatorId);

        students.Should().HaveCount(2);
        students[0].StudentFirstName.Should().Be("Ana");
        students[1].StudentFirstName.Should().Be("Zoe");
    }

    [Fact]
    public async Task GetActiveSponsoredStudentsAsync_Excludes_Student_With_Expired_Sponsorship()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, studentId: 10, sponsorshipStart: UtcNow.AddYears(-2), sponsorshipEnd: UtcNow.AddMonths(-1));
        var repository = new FacilitatorRepository(factory);

        var students = await repository.GetActiveSponsoredStudentsAsync(FacilitatorId);

        students.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveSponsoredStudentsAsync_Excludes_Student_With_Future_Sponsorship()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, studentId: 10, sponsorshipStart: UtcNow.AddMonths(1), sponsorshipEnd: null);
        var repository = new FacilitatorRepository(factory);

        var students = await repository.GetActiveSponsoredStudentsAsync(FacilitatorId);

        students.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveSponsoredStudentsAsync_Excludes_Inactive_Student()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, studentId: 10, disableStudent: true);
        var repository = new FacilitatorRepository(factory);

        var students = await repository.GetActiveSponsoredStudentsAsync(FacilitatorId);

        students.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveSponsoredStudentsAsync_Excludes_Deleted_Student()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, studentId: 10, markStudentDeleted: true);
        var repository = new FacilitatorRepository(factory);

        var students = await repository.GetActiveSponsoredStudentsAsync(FacilitatorId);

        students.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveSponsoredStudentsAsync_Excludes_Student_Of_Another_Facilitator()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, studentId: 10, facilitatorId: 99);
        var repository = new FacilitatorRepository(factory);

        var students = await repository.GetActiveSponsoredStudentsAsync(FacilitatorId);

        students.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveSponsoredStudentsAsync_Excludes_Student_With_Inactive_Sponsor()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, studentId: 10, disableSponsor: true);
        var repository = new FacilitatorRepository(factory);

        var students = await repository.GetActiveSponsoredStudentsAsync(FacilitatorId);

        students.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveSponsoredStudentsAsync_Excludes_Student_With_Deleted_Sponsor()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, studentId: 10, markSponsorDeleted: true);
        var repository = new FacilitatorRepository(factory);

        var students = await repository.GetActiveSponsoredStudentsAsync(FacilitatorId);

        students.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveSponsoredStudentsAsync_Excludes_Student_With_Inactive_Company()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, studentId: 10, useCompanySponsorship: true, disableCompany: true);
        var repository = new FacilitatorRepository(factory);

        var students = await repository.GetActiveSponsoredStudentsAsync(FacilitatorId);

        students.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveSponsoredStudentsAsync_Excludes_Student_With_Disabled_Sponsorship()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory, studentId: 10, disableSponsorship: true);
        var repository = new FacilitatorRepository(factory);

        var students = await repository.GetActiveSponsoredStudentsAsync(FacilitatorId);

        students.Should().BeEmpty();
    }

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private static async Task SeedTwoStudentsAsync(TestDbContextFactory factory)
    {
        await SeedAsync(
            factory,
            studentId: 10,
            studentFirstName: "Zoe",
            studentLastName: "Zulu",
            sponsorshipId: 30);

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

    private static async Task SeedAsync(
        TestDbContextFactory factory,
        int studentId,
        int facilitatorId = FacilitatorId,
        DateTime? sponsorshipStart = null,
        DateTime? sponsorshipEnd = null,
        bool disableStudent = false,
        bool disableSponsor = false,
        bool disableCompany = false,
        bool disableSponsorship = false,
        bool markStudentDeleted = false,
        bool markSponsorDeleted = false,
        bool useCompanySponsorship = false,
        string? studentNickName = null,
        string studentFirstName = "Ana",
        string studentLastName = "Becaria",
        int sponsorshipId = 30)
    {
        sponsorshipStart ??= UtcNow.AddMonths(-1);
        sponsorshipEnd ??= UtcNow.AddMonths(1);

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
            NickName = studentNickName,
            Gender = Gender.Unknown,
            ChapterId = 1,
            FacilitatorId = facilitatorId,
            CreatedById = 1,
            CreatedOnUtc = UtcNow,
        });

        db.Set<Sponsorship>().Add(new Sponsorship
        {
            Id = sponsorshipId,
            StudentId = studentId,
            SponsorId = useCompanySponsorship ? null : SponsorId,
            CompanyId = useCompanySponsorship ? CompanyId : null,
            StartDate = sponsorshipStart.Value,
            EndDate = sponsorshipEnd,
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

        if (disableSponsor)
        {
            var sponsor = await db.Set<Sponsor>().SingleAsync(s => s.Id == SponsorId);
            sponsor.DisabledById = 1;
            await db.SaveChangesAsync();
        }

        if (markSponsorDeleted)
        {
            var sponsor = await db.Set<Sponsor>().SingleAsync(s => s.Id == SponsorId);
            sponsor.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        if (disableCompany)
        {
            var company = await db.Set<Company>().SingleAsync(c => c.Id == CompanyId);
            company.DisabledById = 1;
            await db.SaveChangesAsync();
        }

        if (disableSponsorship)
        {
            var sponsorship = await db.Set<Sponsorship>().SingleAsync(s => s.Id == sponsorshipId);
            sponsorship.DisabledById = 1;
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
