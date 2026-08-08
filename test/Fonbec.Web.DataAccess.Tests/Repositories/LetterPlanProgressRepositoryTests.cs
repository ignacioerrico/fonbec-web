using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class LetterPlanProgressRepositoryTests
{
    private const int ChapterId = 1;
    private const int OtherChapterId = 2;
    private const int PlanId = 100;
    private const int FacilitatorId = 2;
    private const int SponsorId = 20;
    private const int CompanyId = 40;
    private const int StudentId = 10;
    private static readonly DateTime UtcNow = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PlanStartsOn = new(2026, 3, 1);

    [Fact]
    public async Task GetProgressAsync_Returns_Null_When_Plan_Belongs_To_Other_Chapter()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseAsync(factory, chapterId: OtherChapterId);
        var repository = CreateRepository(factory);

        var result = await repository.GetProgressAsync(PlanId, ChapterId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProgressAsync_Returns_Required_Row_For_Active_Student_And_Sponsorship()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseAsync(factory);
        var repository = CreateRepository(factory);

        var result = await repository.GetProgressAsync(PlanId, ChapterId);

        result.Should().NotBeNull();
        result!.Rows.Should().ContainSingle();
        result.Rows[0].LetterStatus.Should().BeNull();
    }

    [Fact]
    public async Task GetProgressAsync_Excludes_Inactive_Student()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseAsync(factory, disableStudent: true);
        var repository = CreateRepository(factory);

        var result = await repository.GetProgressAsync(PlanId, ChapterId);

        result!.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProgressAsync_Excludes_Sponsorship_Not_In_Effect_At_Plan_Start()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseAsync(factory, sponsorshipEnd: PlanStartsOn.AddMonths(-1));
        var repository = CreateRepository(factory);

        var result = await repository.GetProgressAsync(PlanId, ChapterId);

        result!.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProgressAsync_Excludes_Sponsorship_Starting_After_Plan_Start()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseAsync(factory, sponsorshipStart: PlanStartsOn.AddMonths(1));
        var repository = CreateRepository(factory);

        var result = await repository.GetProgressAsync(PlanId, ChapterId);

        result!.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProgressAsync_Maps_Approved_Letter()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseAsync(factory);
        await SeedLetterAsync(factory, DocumentStatus.Approved, approvedOn: UtcNow);
        var repository = CreateRepository(factory);

        var result = await repository.GetProgressAsync(PlanId, ChapterId);

        result!.Rows.Should().ContainSingle(r => r.LetterStatus == DocumentStatus.Approved);
    }

    [Fact]
    public async Task GetProgressAsync_Uses_New_Letter_After_Rejection()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseAsync(factory);
        await SeedLetterAsync(factory, DocumentStatus.Rejected, documentId: 1, rejectedOn: UtcNow.AddDays(-2));
        await SeedLetterAsync(factory, DocumentStatus.Pending, documentId: 2);
        var repository = CreateRepository(factory);

        var result = await repository.GetProgressAsync(PlanId, ChapterId);

        result!.Rows.Should().ContainSingle(r => r.LetterStatus == DocumentStatus.Pending);
    }

    [Fact]
    public async Task GetProgressAsync_Shows_Company_Recipient_Name()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseAsync(factory, useCompanySponsorship: true);
        var repository = CreateRepository(factory);

        var result = await repository.GetProgressAsync(PlanId, ChapterId);

        result!.Rows.Should().ContainSingle(r => r.RecipientName == "Acme Corp" && r.IsCompanySponsorship);
    }

    [Fact]
    public async Task GetProgressAsync_Marks_Exempt_Student()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseAsync(factory);
        await SeedExemptionAsync(factory, "Viaje de estudios");
        var repository = CreateRepository(factory);

        var result = await repository.GetProgressAsync(PlanId, ChapterId);

        result!.Rows.Should().ContainSingle(r => r.IsExempt && r.ExemptionReason == "Viaje de estudios");
    }

    private static LetterPlanProgressRepository CreateRepository(TestDbContextFactory factory) =>
        new(factory);

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private static async Task SeedBaseAsync(
        TestDbContextFactory factory,
        int chapterId = ChapterId,
        bool disableStudent = false,
        DateTime? sponsorshipStart = null,
        DateTime? sponsorshipEnd = null,
        bool useCompanySponsorship = false)
    {
        await using var db = await factory.CreateDbContextAsync();

        db.Set<Chapter>().AddRange(
            new Chapter { Id = ChapterId, Name = "Córdoba", CreatedById = 1, CreatedOnUtc = UtcNow, IsActive = true },
            new Chapter { Id = OtherChapterId, Name = "Otra", CreatedById = 1, CreatedOnUtc = UtcNow, IsActive = true });

        db.Users.Add(new FonbecWebUser
        {
            Id = FacilitatorId,
            UserName = "facilitator",
            NormalizedUserName = "FACILITATOR",
            Email = "facilitator@fonbec.test",
            NormalizedEmail = "FACILITATOR@FONBEC.TEST",
            FirstName = "Ana",
            LastName = "Pérez",
            ChapterId = chapterId,
            SecurityStamp = Guid.NewGuid().ToString(),
        });

        db.Set<Company>().Add(new Company
        {
            Id = CompanyId,
            Name = "Acme Corp",
            CreatedById = 1,
            CreatedOnUtc = UtcNow,
            IsActive = true,
        });

        db.Set<Sponsor>().Add(new Sponsor
        {
            Id = SponsorId,
            FirstName = "María",
            LastName = "López",
            Email = "padrino@fonbec.test",
            Gender = Gender.Unknown,
            ChapterId = chapterId,
            CreatedById = 1,
            CreatedOnUtc = UtcNow,
            IsActive = true,
        });

        db.Set<PlannedDelivery>().Add(new PlannedDelivery
        {
            Id = PlanId,
            ChapterId = chapterId,
            StartsOn = PlanStartsOn,
            Completed = false,
            CreatedById = 1,
            CreatedOnUtc = UtcNow,
        });

        db.Set<Student>().Add(new Student
        {
            Id = StudentId,
            FirstName = "Juan",
            LastName = "García",
            Gender = Gender.Unknown,
            ChapterId = chapterId,
            FacilitatorId = FacilitatorId,
            CreatedById = 1,
            CreatedOnUtc = UtcNow,
            IsActive = true,
        });

        db.Set<Sponsorship>().Add(new Sponsorship
        {
            Id = 30,
            StudentId = StudentId,
            SponsorId = useCompanySponsorship ? null : SponsorId,
            CompanyId = useCompanySponsorship ? CompanyId : null,
            StartDate = sponsorshipStart ?? PlanStartsOn.AddMonths(-1),
            EndDate = sponsorshipEnd ?? PlanStartsOn.AddMonths(1),
            CreatedById = 1,
            CreatedOnUtc = UtcNow,
            IsActive = true,
        });

        await db.SaveChangesAsync();

        if (disableStudent)
        {
            var student = await db.Set<Student>().SingleAsync(s => s.Id == StudentId);
            student.DisabledById = 1;
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedLetterAsync(
        TestDbContextFactory factory,
        DocumentStatus status,
        long documentId = 1,
        DateTime? approvedOn = null,
        DateTime? rejectedOn = null)
    {
        await using var db = await factory.CreateDbContextAsync();

        db.Set<Letter>().Add(new Letter
        {
            DocumentId = documentId,
            DocumentType = DocumentType.Letter,
            ChapterId = ChapterId,
            StudentId = StudentId,
            SponsorId = SponsorId,
            PlanId = PlanId,
            FileKind = FileKind.Blob,
            UploadedOn = UtcNow,
            UploadedById = FacilitatorId,
            Status = status,
            ApprovedOn = approvedOn,
            RejectedOn = rejectedOn,
            RowVersion = [1, 0, 0, 0, 0, 0, 0, 0],
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedExemptionAsync(TestDbContextFactory factory, string reason)
    {
        await using var db = await factory.CreateDbContextAsync();

        db.Set<LetterExemption>().Add(new LetterExemption
        {
            StudentId = StudentId,
            PlannedDeliveryId = PlanId,
            ChapterId = ChapterId,
            Reason = reason,
            CreatedByFonbecUserId = 1,
            CreatedOnUtc = UtcNow,
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