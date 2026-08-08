using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class FacilitatorRepositoryGetCurrentLetterStatusesTests
{
    private const int ChapterId = 1;
    private const int StudentId = 10;
    private const int OtherStudentId = 11;
    private const int SponsorId = 20;
    private const int CompanyId = 40;
    private const int PlanId = 7;
    private const int UploaderId = 1;
    private static readonly DateTime BaseUploadedOn = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetCurrentLetterStatusesAsync_Returns_Latest_Letter_For_Slot()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseDataAsync(factory);
        await SeedLetterAsync(factory, id: 1, sponsorId: SponsorId, status: DocumentStatus.Rejected, uploadedOn: BaseUploadedOn);
        await SeedLetterAsync(factory, id: 2, sponsorId: SponsorId, status: DocumentStatus.Approved, uploadedOn: BaseUploadedOn.AddDays(1));
        var repository = new FacilitatorRepository(factory, TimeProvider.System);

        var result = await repository.GetCurrentLetterStatusesAsync(PlanId, [StudentId]);

        result.Should().ContainSingle(l => l.SponsorId == SponsorId && l.Status == DocumentStatus.Approved);
    }

    [Fact]
    public async Task GetCurrentLetterStatusesAsync_Returns_Empty_When_No_Letter_Uploaded()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseDataAsync(factory);
        var repository = new FacilitatorRepository(factory, TimeProvider.System);

        var result = await repository.GetCurrentLetterStatusesAsync(PlanId, [StudentId]);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCurrentLetterStatusesAsync_Distinguishes_Sponsor_And_Company_Slots()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseDataAsync(factory);
        await SeedLetterAsync(factory, id: 1, sponsorId: SponsorId, status: DocumentStatus.Approved, uploadedOn: BaseUploadedOn);
        await SeedLetterAsync(factory, id: 2, companyId: CompanyId, status: DocumentStatus.Rejected, uploadedOn: BaseUploadedOn);
        var repository = new FacilitatorRepository(factory, TimeProvider.System);

        var result = await repository.GetCurrentLetterStatusesAsync(PlanId, [StudentId]);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(l => l.SponsorId == SponsorId && l.Status == DocumentStatus.Approved);
        result.Should().ContainSingle(l => l.CompanyId == CompanyId && l.Status == DocumentStatus.Rejected);
    }

    [Fact]
    public async Task GetCurrentLetterStatusesAsync_Ignores_Letters_For_Other_Plan()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseDataAsync(factory);
        await SeedLetterAsync(factory, id: 1, sponsorId: SponsorId, status: DocumentStatus.Approved, uploadedOn: BaseUploadedOn, planId: PlanId + 1);
        var repository = new FacilitatorRepository(factory, TimeProvider.System);

        var result = await repository.GetCurrentLetterStatusesAsync(PlanId, [StudentId]);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCurrentLetterStatusesAsync_Ignores_Letters_For_Student_Not_Requested()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseDataAsync(factory);
        await SeedLetterAsync(factory, id: 1, studentId: OtherStudentId, sponsorId: SponsorId, status: DocumentStatus.Approved, uploadedOn: BaseUploadedOn);
        var repository = new FacilitatorRepository(factory, TimeProvider.System);

        var result = await repository.GetCurrentLetterStatusesAsync(PlanId, [StudentId]);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCurrentLetterStatusesAsync_Maps_RejectionReason_From_RejectedReason_Description()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseDataAsync(factory);
        await SeedRejectedReasonAsync(factory, id: 5, description: "Foto borrosa");
        await SeedLetterAsync(factory, id: 1, sponsorId: SponsorId, status: DocumentStatus.Rejected, uploadedOn: BaseUploadedOn, rejectedReasonId: 5);
        var repository = new FacilitatorRepository(factory, TimeProvider.System);

        var result = await repository.GetCurrentLetterStatusesAsync(PlanId, [StudentId]);

        result.Single().RejectionReason.Should().Be("Foto borrosa");
    }

    [Fact]
    public async Task GetCurrentLetterStatusesAsync_Falls_Back_To_RejectionNotes_When_No_RejectedReason()
    {
        var factory = CreateDbContextFactory();
        await SeedBaseDataAsync(factory);
        await SeedLetterAsync(factory, id: 1, sponsorId: SponsorId, status: DocumentStatus.Rejected, uploadedOn: BaseUploadedOn, rejectionNotes: "no coincide el firmante");
        var repository = new FacilitatorRepository(factory, TimeProvider.System);

        var result = await repository.GetCurrentLetterStatusesAsync(PlanId, [StudentId]);

        result.Single().RejectionReason.Should().Be("no coincide el firmante");
    }

    private static TestDbContextFactory CreateDbContextFactory() => new(Guid.NewGuid().ToString());

    private static async Task SeedBaseDataAsync(TestDbContextFactory factory)
    {
        await using var db = await factory.CreateDbContextAsync();

        db.Users.Add(new FonbecWebUser
        {
            Id = UploaderId,
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
            CreatedById = UploaderId,
            CreatedOnUtc = BaseUploadedOn,
            IsActive = true,
        });

        db.Set<Sponsor>().Add(new Sponsor
        {
            Id = SponsorId,
            FirstName = "Padrino",
            LastName = "Activo",
            Email = "padrino@fonbec.test",
            Gender = Gender.Unknown,
            ChapterId = ChapterId,
            CreatedById = UploaderId,
            CreatedOnUtc = BaseUploadedOn,
        });

        db.Set<Company>().Add(new Company
        {
            Id = CompanyId,
            Name = "Acme Corp",
            CreatedById = UploaderId,
            CreatedOnUtc = BaseUploadedOn,
        });

        db.Set<Student>().Add(new Student
        {
            Id = StudentId,
            FirstName = "Ana",
            LastName = "Becaria",
            Gender = Gender.Unknown,
            ChapterId = ChapterId,
            FacilitatorId = UploaderId,
            CreatedById = UploaderId,
            CreatedOnUtc = BaseUploadedOn,
        });

        db.Set<Student>().Add(new Student
        {
            Id = OtherStudentId,
            FirstName = "Beto",
            LastName = "Becario",
            Gender = Gender.Unknown,
            ChapterId = ChapterId,
            FacilitatorId = UploaderId,
            CreatedById = UploaderId,
            CreatedOnUtc = BaseUploadedOn,
        });

        db.Set<PlannedDelivery>().Add(new PlannedDelivery
        {
            Id = PlanId,
            ChapterId = ChapterId,
            StartsOn = BaseUploadedOn.AddMonths(-1),
            Completed = false,
            CreatedById = UploaderId,
            CreatedOnUtc = BaseUploadedOn,
        });

        db.Set<PlannedDelivery>().Add(new PlannedDelivery
        {
            Id = PlanId + 1,
            ChapterId = ChapterId,
            StartsOn = BaseUploadedOn.AddMonths(-2),
            Completed = true,
            CreatedById = UploaderId,
            CreatedOnUtc = BaseUploadedOn,
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedRejectedReasonAsync(TestDbContextFactory factory, int id, string description)
    {
        await using var db = await factory.CreateDbContextAsync();

        db.Set<RejectedReason>().Add(new RejectedReason
        {
            Id = id,
            Code = $"REASON_{id}",
            Description = description,
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedLetterAsync(
        TestDbContextFactory factory,
        long id,
        DocumentStatus status,
        DateTime uploadedOn,
        int studentId = StudentId,
        int? sponsorId = null,
        int? companyId = null,
        int planId = PlanId,
        int? rejectedReasonId = null,
        string? rejectionNotes = null)
    {
        await using var db = await factory.CreateDbContextAsync();

        db.Set<Letter>().Add(new Letter
        {
            DocumentId = id,
            DocumentType = DocumentType.Letter,
            ChapterId = ChapterId,
            StudentId = studentId,
            SponsorId = sponsorId,
            CompanyId = companyId,
            PlanId = planId,
            FileKind = FileKind.Text,
            TextContent = "carta",
            DigitalImprovementStatus = DigitalImprovementStatus.NotApplicable,
            UploadedOn = uploadedOn,
            UploadedById = UploaderId,
            Status = status,
            RejectedReasonId = rejectedReasonId,
            RejectionNotes = rejectionNotes,
            RowVersion = Guid.NewGuid().ToByteArray(),
        });

        await db.SaveChangesAsync();
    }

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
