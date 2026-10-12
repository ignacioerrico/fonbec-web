using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class FacilitatorRepositoryGetUploadContextTests
{
    private const int ChapterId = 1;
    private const int OtherChapterId = 2;
    private const int FacilitatorId = 1;
    private const int StudentId = 10;
    private const int OtherStudentId = 11;
    private const int SponsorId = 20;
    private const int OtherSponsorId = 21;
    private const int CompanyId = 40;
    private const int OtherCompanyId = 41;
    private const int PlanId = 7;
    private const int CompletedPlanId = 8;
    private const int OtherChapterPlanId = 9;
    private const int GlobalPlanId = 10;
    private static readonly DateTime PlanStartsOn = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetUploadContextAsync_Resolves_Sponsor_Of_Student_On_Open_Plan()
    {
        var factory = await CreateSeededFactoryAsync();
        var repository = new FacilitatorRepository(factory, TimeProvider.System);

        var context = await repository.GetUploadContextAsync(StudentId, PlanId, SponsorId, null);

        context.Should().NotBeNull();
        context!.PlanStartsOn.Should().Be(PlanStartsOn);
        context.SponsorFirstName.Should().Be("Padrino");
        context.SponsorLastName.Should().Be("Uno");
    }

    [Fact]
    public async Task GetUploadContextAsync_Resolves_Company_Of_Student_On_Open_Plan()
    {
        var factory = await CreateSeededFactoryAsync();
        var repository = new FacilitatorRepository(factory, TimeProvider.System);

        var context = await repository.GetUploadContextAsync(StudentId, PlanId, null, CompanyId);

        context!.CompanyName.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetUploadContextAsync_Does_Not_Resolve_Sponsor_Of_Another_Student()
    {
        var factory = await CreateSeededFactoryAsync();
        var repository = new FacilitatorRepository(factory, TimeProvider.System);

        var context = await repository.GetUploadContextAsync(StudentId, PlanId, OtherSponsorId, null);

        context!.SponsorFirstName.Should().BeNull();
    }

    [Fact]
    public async Task GetUploadContextAsync_Does_Not_Resolve_Company_Of_Another_Student()
    {
        var factory = await CreateSeededFactoryAsync();
        var repository = new FacilitatorRepository(factory, TimeProvider.System);

        var context = await repository.GetUploadContextAsync(StudentId, PlanId, null, OtherCompanyId);

        context!.CompanyName.Should().BeNull();
    }

    [Fact]
    public async Task GetUploadContextAsync_Does_Not_Resolve_Inactive_Sponsorship()
    {
        var factory = await CreateSeededFactoryAsync();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            var sponsorship = await db.Set<Sponsorship>().SingleAsync(sp => sp.StudentId == StudentId && sp.SponsorId == SponsorId, TestContext.Current.CancellationToken);
            sponsorship.DisabledById = FacilitatorId;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var repository = new FacilitatorRepository(factory, TimeProvider.System);

        var context = await repository.GetUploadContextAsync(StudentId, PlanId, SponsorId, null);

        context!.SponsorFirstName.Should().BeNull();
    }

    [Fact]
    public async Task GetUploadContextAsync_Does_Not_Resolve_Sponsorship_Ended_Before_Plan()
    {
        var factory = await CreateSeededFactoryAsync();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            var sponsorship = await db.Set<Sponsorship>().SingleAsync(sp => sp.StudentId == StudentId && sp.SponsorId == SponsorId, TestContext.Current.CancellationToken);
            sponsorship.EndDate = PlanStartsOn.AddDays(-1);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var repository = new FacilitatorRepository(factory, TimeProvider.System);

        var context = await repository.GetUploadContextAsync(StudentId, PlanId, SponsorId, null);

        context!.SponsorFirstName.Should().BeNull();
    }

    [Theory]
    [InlineData(CompletedPlanId)]
    [InlineData(OtherChapterPlanId)]
    [InlineData(999)]
    public async Task GetUploadContextAsync_Does_Not_Resolve_Plan_Or_Recipient_When_Plan_Is_Not_Open_For_Chapter(int planId)
    {
        var factory = await CreateSeededFactoryAsync();
        var repository = new FacilitatorRepository(factory, TimeProvider.System);

        var context = await repository.GetUploadContextAsync(StudentId, planId, SponsorId, null);

        context!.PlanStartsOn.Should().BeNull();
        context.SponsorFirstName.Should().BeNull();
    }

    [Fact]
    public async Task GetUploadContextAsync_Resolves_Plan_Without_Chapter()
    {
        var factory = await CreateSeededFactoryAsync();
        var repository = new FacilitatorRepository(factory, TimeProvider.System);

        var context = await repository.GetUploadContextAsync(StudentId, GlobalPlanId, SponsorId, null);

        context!.PlanStartsOn.Should().Be(PlanStartsOn);
        context.SponsorFirstName.Should().Be("Padrino");
    }

    private static async Task<TestDbContextFactory> CreateSeededFactoryAsync()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        await using var db = await factory.CreateDbContextAsync();

        db.Users.Add(new FonbecWebUser
        {
            Id = FacilitatorId,
            UserName = "facilitator",
            NormalizedUserName = "FACILITATOR",
            Email = "facilitator@fonbec.test",
            NormalizedEmail = "FACILITATOR@FONBEC.TEST",
            FirstName = "Mediador",
            LastName = "Uno",
            ChapterId = ChapterId,
            SecurityStamp = Guid.NewGuid().ToString(),
        });

        db.Set<Chapter>().Add(new Chapter
        {
            Id = ChapterId,
            Name = "Chapter",
            CreatedById = FacilitatorId,
            CreatedOnUtc = PlanStartsOn,
            IsActive = true,
        });

        db.Set<Sponsor>().AddRange(
            NewSponsor(SponsorId, "Padrino", "Uno"),
            NewSponsor(OtherSponsorId, "Padrino", "Ajeno"));

        db.Set<Company>().AddRange(
            NewCompany(CompanyId, "Acme Corp"),
            NewCompany(OtherCompanyId, "Otra SA"));

        db.Set<Student>().AddRange(
            NewStudent(StudentId, "Ana"),
            NewStudent(OtherStudentId, "Beto"));

        db.Set<Sponsorship>().AddRange(
            NewSponsorship(1, StudentId, sponsorId: SponsorId),
            NewSponsorship(2, StudentId, companyId: CompanyId),
            NewSponsorship(3, OtherStudentId, sponsorId: OtherSponsorId),
            NewSponsorship(4, OtherStudentId, companyId: OtherCompanyId));

        db.Set<PlannedDelivery>().AddRange(
            NewPlan(PlanId, ChapterId, completed: false),
            NewPlan(CompletedPlanId, ChapterId, completed: true),
            NewPlan(OtherChapterPlanId, OtherChapterId, completed: false),
            NewPlan(GlobalPlanId, null, completed: false));

        await db.SaveChangesAsync();
        return factory;
    }

    private static Sponsor NewSponsor(int id, string firstName, string lastName) => new()
    {
        Id = id,
        FirstName = firstName,
        LastName = lastName,
        Email = $"sponsor{id}@fonbec.test",
        Gender = Gender.Unknown,
        ChapterId = ChapterId,
        IsActive = true,
        CreatedById = FacilitatorId,
        CreatedOnUtc = PlanStartsOn,
    };

    private static Company NewCompany(int id, string name) => new()
    {
        Id = id,
        Name = name,
        IsActive = true,
        CreatedById = FacilitatorId,
        CreatedOnUtc = PlanStartsOn,
    };

    private static Student NewStudent(int id, string firstName) => new()
    {
        Id = id,
        FirstName = firstName,
        LastName = "Becario",
        Gender = Gender.Unknown,
        ChapterId = ChapterId,
        FacilitatorId = FacilitatorId,
        IsActive = true,
        CreatedById = FacilitatorId,
        CreatedOnUtc = PlanStartsOn,
    };

    private static Sponsorship NewSponsorship(int id, int studentId, int? sponsorId = null, int? companyId = null) => new()
    {
        Id = id,
        StudentId = studentId,
        SponsorId = sponsorId,
        CompanyId = companyId,
        StartDate = PlanStartsOn.AddYears(-1),
        IsActive = true,
        CreatedById = FacilitatorId,
        CreatedOnUtc = PlanStartsOn,
    };

    private static PlannedDelivery NewPlan(int id, int? chapterId, bool completed) => new()
    {
        Id = id,
        ChapterId = chapterId,
        StartsOn = PlanStartsOn,
        Completed = completed,
        IsActive = true,
        CreatedById = FacilitatorId,
        CreatedOnUtc = PlanStartsOn,
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