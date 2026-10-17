using FluentAssertions;
using Fonbec.Web.DataAccess;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class ChapterHomeMatchCountTests
{
    private static readonly DateTime AsOf = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CountsPeopleWithNoCurrentOrUpcomingMatch()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        await SeedAsync(factory);

        var students = new StudentRepository(factory, null!);
        var sponsors = new SponsorRepository(factory);

        var withoutSponsor = await students.CountStudentsWithoutSponsorAsync(1, AsOf);
        var withoutStudent = await sponsors.CountSponsorsWithoutStudentAsync(1, AsOf);

        withoutSponsor.Should().Be(5);
        withoutStudent.Should().Be(5);
    }

    private static async Task SeedAsync(TestDbContextFactory factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.Users.Add(new FonbecWebUser
        {
            Id = 1,
            UserName = "admin",
            NormalizedUserName = "ADMIN",
            Email = "admin@test.com",
            NormalizedEmail = "ADMIN@TEST.COM",
            FirstName = "Admin",
            LastName = "User",
            SecurityStamp = Guid.NewGuid().ToString(),
        });
        db.Set<Chapter>().AddRange(
            Chapter(1, "Norte"),
            Chapter(2, "Sur"));

        var lonely = Sponsor("Lonely");
        var finishedPadrino = Sponsor("Finished");
        var disabledLinkPadrino = Sponsor("DisabledLink");
        var inactivePadrino = Sponsor("Inactive");
        var deletedPadrino = Sponsor("Deleted");
        deletedPadrino.IsDeleted = true;
        var currentPadrino = Sponsor("Current");
        var upcomingPadrino = Sponsor("Upcoming");
        var padrinoOfInactiveStudent = Sponsor("OfInactive");
        var padrinoOfDeletedStudent = Sponsor("OfDeleted");
        var otherChapterPadrino = Sponsor("Other");
        otherChapterPadrino.ChapterId = 2;

        var company = new Company
        {
            Name = "Acme",
            PublicAccessToken = Guid.NewGuid(),
            CreatedById = 1,
            CreatedOnUtc = AsOf,
        };

        var ana = Student("Ana");
        var bruno = Student("Bruno");
        var carla = Student("Carla");
        var dora = Student("Dora");
        var eva = Student("Eva");
        var fran = Student("Fran");
        var gabi = Student("Gabi");
        var hugo = Student("Hugo");
        var inactiveStudent = Student("Inactive");
        var deletedStudent = Student("Deleted");
        deletedStudent.IsDeleted = true;
        var otherChapterStudent = Student("Other");
        otherChapterStudent.ChapterId = 2;

        var disabledSponsorship = Link(carla, disabledLinkPadrino, AsOf.AddMonths(-2), null);
        db.AddRange(
            lonely, finishedPadrino, disabledLinkPadrino, inactivePadrino, deletedPadrino,
            currentPadrino, upcomingPadrino, padrinoOfInactiveStudent, padrinoOfDeletedStudent,
            otherChapterPadrino, company,
            ana, bruno, carla, dora, eva, fran, gabi, hugo, inactiveStudent, deletedStudent, otherChapterStudent,
            Link(bruno, finishedPadrino, AsOf.AddMonths(-6), AsOf.AddMonths(-1)),
            disabledSponsorship,
            Link(dora, inactivePadrino, AsOf.AddMonths(-1), null),
            Link(eva, deletedPadrino, AsOf.AddMonths(-1), null),
            Link(fran, currentPadrino, AsOf.AddMonths(-1), null),
            Link(gabi, upcomingPadrino, AsOf.AddMonths(2), null),
            new Sponsorship
            {
                Student = hugo,
                Company = company,
                StartDate = AsOf.AddMonths(-1),
                CreatedById = 1,
                CreatedOnUtc = AsOf,
            },
            Link(inactiveStudent, padrinoOfInactiveStudent, AsOf.AddMonths(-1), null),
            Link(deletedStudent, padrinoOfDeletedStudent, AsOf.AddMonths(-1), null));
        await db.SaveChangesAsync();

        disabledSponsorship.DisabledById = 1;
        inactivePadrino.DisabledById = 1;
        inactiveStudent.DisabledById = 1;
        await db.SaveChangesAsync();
    }

    private static Chapter Chapter(int id, string name) => new()
    {
        Id = id,
        Name = name,
        CreatedById = 1,
        CreatedOnUtc = AsOf,
    };

    private static Sponsor Sponsor(string name) => new()
    {
        FirstName = name,
        LastName = "Padrino",
        Email = $"{name}@test.com",
        PublicAccessToken = Guid.NewGuid(),
        ChapterId = 1,
        CreatedById = 1,
        CreatedOnUtc = AsOf,
    };

    private static Student Student(string name) => new()
    {
        FirstName = name,
        LastName = "Becario",
        ChapterId = 1,
        FacilitatorId = 1,
        CreatedById = 1,
        CreatedOnUtc = AsOf,
    };

    private static Sponsorship Link(Student student, Sponsor sponsor, DateTime start, DateTime? end) => new()
    {
        Student = student,
        Sponsor = sponsor,
        StartDate = start,
        EndDate = end,
        CreatedById = 1,
        CreatedOnUtc = AsOf,
    };

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