using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class ManagerUploadRepositoryGetUploadContextTests
{
    private const int ChapterId = 1;
    private const int ManagerId = 1;
    private const int FacilitatorId = 2;
    private const int StudentId = 10;
    private const int SponsorId = 20;
    private const int CompanyId = 40;
    private const int PlanId = 7;

    // Plan is July 2026. "Today" is August 2026. The sponsorship ran through July.
    private static readonly DateTime PlanStartsOn = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Now = new(2026, 8, 17, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetUploadContextAsync_Resolves_Sponsor_When_Sponsorship_Ended_After_Plan_Month()
    {
        var factory = await CreateSeededFactoryAsync(endDate: new DateTime(2026, 7, 31));
        var repository = new ManagerUploadRepository(factory, new FixedTimeProvider(Now));

        var progress = await new LetterPlanProgressRepository(factory).GetProgressAsync(PlanId, ChapterId);
        progress!.Rows.Should().ContainSingle(row => row.StudentId == StudentId && row.SponsorId == SponsorId);

        var context = await repository.GetUploadContextAsync(StudentId, PlanId, SponsorId, null);

        context.Should().NotBeNull();
        context!.PlanStartsOn.Should().Be(PlanStartsOn);
        context.SponsorFirstName.Should().Be("Padrino");
        context.SponsorLastName.Should().Be("Uno");

        // The upload page toasts and leaves when this returns null.
        var upload = await UploadService(factory).GetUploadContextAsync(
            ManagerId, ChapterId, StudentId, "carta", SponsorId, null, PlanId);

        upload.Should().NotBeNull();
        upload!.PlanId.Should().Be(PlanId);
        upload.SponsorId.Should().Be(SponsorId);
        upload.RecipientName.Should().Be("Padrino Uno");
    }

    [Fact]
    public async Task GetUploadContextAsync_Resolves_Company_When_Sponsorship_Ended_After_Plan_Month()
    {
        var factory = await CreateSeededFactoryAsync(endDate: new DateTime(2026, 7, 31), company: true);
        var repository = new ManagerUploadRepository(factory, new FixedTimeProvider(Now));

        var context = await repository.GetUploadContextAsync(StudentId, PlanId, null, CompanyId);

        context!.PlanStartsOn.Should().Be(PlanStartsOn);
        context.CompanyName.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetUploadContextAsync_Resolves_Sponsor_On_Sqlite_When_Sponsorship_Ended_After_Plan_Month()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        connection.CreateFunction("GETUTCDATE", () => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        var factory = new SqliteFactory(connection);
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        await SeedAsync(factory, endDate: new DateTime(2026, 7, 31));
        var repository = new ManagerUploadRepository(factory, new FixedTimeProvider(Now));

        var context = await repository.GetUploadContextAsync(StudentId, PlanId, SponsorId, null);

        context!.PlanStartsOn.Should().Be(PlanStartsOn);
        context.SponsorFirstName.Should().Be("Padrino");
    }

    [Fact]
    public async Task GetUploadContextAsync_Does_Not_Resolve_Sponsor_When_Sponsorship_Ended_Before_Plan_Month()
    {
        var factory = await CreateSeededFactoryAsync(endDate: new DateTime(2026, 6, 30));
        var repository = new ManagerUploadRepository(factory, new FixedTimeProvider(Now));

        var context = await repository.GetUploadContextAsync(StudentId, PlanId, SponsorId, null);

        context!.PlanStartsOn.Should().Be(PlanStartsOn);
        context.SponsorFirstName.Should().BeNull();
    }

    private static ManagerUploadService UploadService(IDbContextFactory<FonbecWebDbContext> factory)
    {
        var exemptions = Substitute.For<ILetterExemptionService>();
        exemptions.IsExemptAsync(StudentId, PlanId).Returns(false);
        return new ManagerUploadService(
            new ManagerUploadRepository(factory, new FixedTimeProvider(Now)),
            Substitute.For<IDocumentService>(),
            Substitute.For<IDocumentRepository>(),
            exemptions,
            new FixedTimeProvider(Now));
    }

    private static async Task<TestDbContextFactory> CreateSeededFactoryAsync(DateTime endDate, bool company = false)
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        await SeedAsync(factory, endDate, company);
        return factory;
    }

    private static async Task SeedAsync(IDbContextFactory<FonbecWebDbContext> factory, DateTime endDate, bool company = false)
    {
        await using var db = await factory.CreateDbContextAsync();

        db.Users.AddRange(
            new FonbecWebUser
            {
                Id = ManagerId,
                UserName = "manager",
                NormalizedUserName = "MANAGER",
                Email = "manager@fonbec.test",
                NormalizedEmail = "MANAGER@FONBEC.TEST",
                FirstName = "Coordinador",
                LastName = "Uno",
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
        await db.SaveChangesAsync();

        db.Set<Chapter>().Add(new Chapter
        {
            Id = ChapterId,
            Name = "Chapter",
            CreatedById = ManagerId,
            CreatedOnUtc = PlanStartsOn,
            IsActive = true,
        });
        await db.SaveChangesAsync();

        var manager = await db.Users.SingleAsync(u => u.Id == ManagerId);
        var facilitator = await db.Users.SingleAsync(u => u.Id == FacilitatorId);
        manager.ChapterId = ChapterId;
        facilitator.ChapterId = ChapterId;

        db.Set<Sponsor>().Add(new Sponsor
        {
            Id = SponsorId,
            FirstName = "Padrino",
            LastName = "Uno",
            Email = "sponsor@fonbec.test",
            Gender = Gender.Unknown,
            ChapterId = ChapterId,
            IsActive = true,
            CreatedById = ManagerId,
            CreatedOnUtc = PlanStartsOn,
        });

        db.Set<Company>().Add(new Company
        {
            Id = CompanyId,
            Name = "Acme Corp",
            IsActive = true,
            CreatedById = ManagerId,
            CreatedOnUtc = PlanStartsOn,
        });

        db.Set<Student>().Add(new Student
        {
            Id = StudentId,
            FirstName = "Ana",
            LastName = "Becario",
            Gender = Gender.Unknown,
            ChapterId = ChapterId,
            FacilitatorId = FacilitatorId,
            IsActive = true,
            CreatedById = ManagerId,
            CreatedOnUtc = PlanStartsOn,
        });

        db.Set<Sponsorship>().Add(new Sponsorship
        {
            Id = 1,
            StudentId = StudentId,
            SponsorId = company ? null : SponsorId,
            CompanyId = company ? CompanyId : null,
            StartDate = new DateTime(2026, 6, 1),
            EndDate = endDate,
            IsActive = true,
            CreatedById = ManagerId,
            CreatedOnUtc = PlanStartsOn,
        });

        db.Set<PlannedDelivery>().Add(new PlannedDelivery
        {
            Id = PlanId,
            ChapterId = ChapterId,
            StartsOn = PlanStartsOn,
            Completed = false,
            IsActive = true,
            CreatedById = ManagerId,
            CreatedOnUtc = PlanStartsOn,
        });

        await db.SaveChangesAsync();
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class SqliteFactory(SqliteConnection connection) : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<FonbecWebDbContext>().UseSqlite(connection).Options);

        public Task<FonbecWebDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

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