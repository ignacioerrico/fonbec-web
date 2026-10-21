using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Options;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class FacilitatorStudentHistoryRepositoryTests
{
    private const int FacilitatorId = 2;
    private const int OtherFacilitatorId = 9;

    [Fact]
    public async Task GetLettersHistoryAsync_Returns_Newest_First_With_Campaign_Recipient_And_Reason()
    {
        var factory = CreateDbContextFactory();
        var sameInstant = new DateTime(2026, 6, 12, 15, 0, 0, DateTimeKind.Utc);

        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            AddStudent(db, 10, FacilitatorId, "María", "González");
            AddStudent(db, 11, FacilitatorId, "Ana", "Becaria");
            AddPlan(db, 1, new DateTime(2026, 6, 1));
            AddPlan(db, 2, new DateTime(2026, 3, 1));
            AddSponsor(db, 7, "Luis", "Padrino");
            AddCompany(db, 8, "Acme");

            db.Set<RejectedReason>().Add(new RejectedReason
            {
                Id = 3,
                Code = "short",
                Description = "Carta muy corta",
            });

            AddLetter(db, documentId: 1, studentId: 10, planId: 2, sponsorId: 7,
                uploadedOn: new DateTime(2026, 3, 21),
                fileKind: FileKind.Text,
                status: DocumentStatus.Rejected,
                rejectedReasonId: 3,
                rejectionNotes: "falta el cierre",
                textContent: "Hola padrino");
            AddLetter(db, documentId: 2, studentId: 10, planId: 1, companyId: 8,
                uploadedOn: sameInstant,
                fileKind: FileKind.YouTube,
                status: DocumentStatus.Approved,
                youTubeVideoId: "abc123");
            AddLetter(db, documentId: 3, studentId: 10, planId: 1, sponsorId: 7,
                uploadedOn: sameInstant,
                fileKind: FileKind.Blob,
                status: DocumentStatus.Approved,
                pageCount: 2);
            AddLetter(db, documentId: 4, studentId: 11, planId: 1, sponsorId: 7,
                uploadedOn: new DateTime(2026, 6, 20),
                status: DocumentStatus.Approved);

            db.Set<ReportCard>().Add(new ReportCard
            {
                DocumentId = 30,
                StudentId = 10,
                ChapterId = 1,
                Period = new DateOnly(2026, 6, 1),
                Description = "No es una carta",
                Status = DocumentStatus.Approved,
                FileKind = FileKind.Blob,
                UploadedOn = new DateTime(2026, 6, 20),
                UploadedById = FacilitatorId,
                RowVersion = [1],
            });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var result = await CreateRepository(factory).GetLettersHistoryAsync(FacilitatorId, 10);

        result.Should().NotBeNull();
        result!.StudentName.Should().Be("María González");
        result.Items.Select(i => i.DocumentId).Should().Equal(3L, 2L, 1L);

        var file = result.Items.Single(i => i.DocumentId == 3);
        file.PlanStartsOn.Should().Be(new DateTime(2026, 6, 1));
        file.RecipientName.Should().Be("Luis Padrino");
        file.FileKind.Should().Be(FileKind.Blob);
        file.PageCount.Should().Be(2);

        var video = result.Items.Single(i => i.DocumentId == 2);
        video.RecipientName.Should().Be("Acme");
        video.FileKind.Should().Be(FileKind.YouTube);
        video.YouTubeVideoId.Should().Be("abc123");

        var rejected = result.Items.Single(i => i.DocumentId == 1);
        rejected.Status.Should().Be(DocumentStatus.Rejected);
        rejected.RejectionReason.Should().Be("Carta muy corta: falta el cierre");
        rejected.TextContent.Should().Be("Hola padrino");
        rejected.PlanStartsOn.Should().Be(new DateTime(2026, 3, 1));
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task GetLettersHistoryAsync_Returns_Null_When_Student_Is_Not_Available(
        bool otherOwner, bool inactive, bool deleted)
    {
        var factory = CreateDbContextFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            var ownerId = otherOwner ? OtherFacilitatorId : FacilitatorId;

            AddStudent(db, 10, ownerId, "María", "González", isDeleted: deleted);
            AddPlan(db, 1, new DateTime(2026, 6, 1));
            AddSponsor(db, 7, "Luis", "Padrino");
            AddLetter(db, documentId: 1, studentId: 10, planId: 1, sponsorId: 7,
                uploadedOn: new DateTime(2026, 6, 1),
                status: DocumentStatus.Approved);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            if (inactive)
            {
                var student = await db.Set<Student>().SingleAsync(s => s.Id == 10, TestContext.Current.CancellationToken);
                student.DisabledById = 1;
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
        }

        var result = await CreateRepository(factory).GetLettersHistoryAsync(FacilitatorId, 10);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetReportCardsHistoryAsync_Returns_Newest_Period_First_With_Level_And_Reason()
    {
        var factory = CreateDbContextFactory();
        var samePeriod = new DateOnly(2026, 6, 1);

        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            AddStudent(db, 10, FacilitatorId, "María", "González", universityStartYear: new DateTime(2024, 3, 1));
            AddStudent(db, 11, FacilitatorId, "Ana", "Becaria");

            db.Set<RejectedReason>().Add(new RejectedReason
            {
                Id = 4,
                Code = "blurry",
                Description = "Imagen borrosa",
            });

            AddReportCard(db, documentId: 1, studentId: 10, period: new DateOnly(2026, 3, 1),
                description: "Primer cuatrimestre",
                uploadedOn: new DateTime(2026, 4, 1),
                fileKind: FileKind.Blob,
                status: DocumentStatus.Rejected,
                rejectedReasonId: 4,
                rejectionNotes: "falta el sello",
                pageCount: 1);
            AddReportCard(db, documentId: 2, studentId: 10, period: samePeriod,
                description: "Mismo período, id menor",
                uploadedOn: new DateTime(2026, 7, 2),
                fileKind: FileKind.YouTube,
                status: DocumentStatus.Approved,
                youTubeVideoId: "libreta1");
            AddReportCard(db, documentId: 3, studentId: 10, period: samePeriod,
                description: "Mismo período, id mayor",
                uploadedOn: new DateTime(2026, 7, 1),
                fileKind: FileKind.Blob,
                status: DocumentStatus.Approved,
                pageCount: 2);
            AddReportCard(db, documentId: 8, studentId: 11, period: new DateOnly(2026, 8, 1),
                description: "De otro becario",
                uploadedOn: new DateTime(2026, 8, 1),
                status: DocumentStatus.Approved);

            db.Set<Letter>().Add(new Letter
            {
                DocumentId = 21,
                StudentId = 10,
                ChapterId = 1,
                PlanId = 1,
                SponsorId = 1,
                Status = DocumentStatus.Approved,
                FileKind = FileKind.Text,
                UploadedOn = new DateTime(2026, 6, 1),
                UploadedById = FacilitatorId,
                RowVersion = [1],
            });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var repository = CreateRepository(factory);
        var maria = await repository.GetReportCardsHistoryAsync(FacilitatorId, 10);

        maria.Should().NotBeNull();
        maria!.StudentName.Should().Be("María González");
        maria.EducationLevel.Should().Be(EducationLevel.University);
        maria.Items.Select(i => i.DocumentId).Should().Equal(3L, 2L, 1L);

        var latest = maria.Items[0];
        latest.Period.Should().Be(samePeriod);
        latest.Description.Should().Be("Mismo período, id mayor");
        latest.PageCount.Should().Be(2);

        var video = maria.Items.Single(i => i.DocumentId == 2);
        video.FileKind.Should().Be(FileKind.YouTube);
        video.YouTubeVideoId.Should().Be("libreta1");

        var rejected = maria.Items.Single(i => i.DocumentId == 1);
        rejected.RejectionReason.Should().Be("Imagen borrosa: falta el sello");

        var ana = await repository.GetReportCardsHistoryAsync(FacilitatorId, 11);
        ana.Should().NotBeNull();
        ana!.EducationLevel.Should().Be(EducationLevel.PrimarySchool);
        ana.Items.Should().ContainSingle(i => i.DocumentId == 8);
    }

    [Fact]
    public async Task GetReportCardsHistoryAsync_Returns_Null_For_Another_Facilitators_Student()
    {
        var factory = CreateDbContextFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            AddStudent(db, 10, OtherFacilitatorId, "María", "González");
            AddReportCard(db, documentId: 1, studentId: 10, period: new DateOnly(2026, 6, 1),
                description: "Boletín", uploadedOn: new DateTime(2026, 6, 1),
                status: DocumentStatus.Approved);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var result = await CreateRepository(factory).GetReportCardsHistoryAsync(FacilitatorId, 10);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Facilitator_Blob_Context_Is_Limited_To_The_Document_Type_And_Owner()
    {
        var factory = CreateDbContextFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            AddStudent(db, 10, FacilitatorId, "María", "González");
            AddPlan(db, 1, new DateTime(2026, 6, 1));
            AddSponsor(db, 7, "Luis", "Padrino");
            AddLetter(db, documentId: 5, studentId: 10, planId: 1, sponsorId: 7,
                uploadedOn: new DateTime(2026, 6, 1),
                status: DocumentStatus.Approved,
                pageCount: 1);
            AddReportCard(db, documentId: 6, studentId: 10, period: new DateOnly(2026, 6, 1),
                description: "Boletín",
                uploadedOn: new DateTime(2026, 6, 1),
                status: DocumentStatus.Approved,
                pageCount: 1);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var repository = CreateDocumentRepository(factory);

        var letter = await repository.GetFacilitatorLetterBlobContextAsync(FacilitatorId, 10, 5);
        letter.Should().NotBeNull();
        letter!.DocumentId.Should().Be(5);
        letter.DocumentType.Should().Be(DocumentType.Letter);
        letter.Pages.Should().ContainSingle(p => p.PageNumber == 1);

        var letterAsReportCard = await repository.GetFacilitatorReportCardBlobContextAsync(FacilitatorId, 10, 5);
        letterAsReportCard.Should().BeNull();

        var reportCard = await repository.GetFacilitatorReportCardBlobContextAsync(FacilitatorId, 10, 6);
        reportCard.Should().NotBeNull();
        reportCard!.DocumentType.Should().Be(DocumentType.ReportCard);
        reportCard.Pages.Should().ContainSingle(p => p.PageNumber == 1);

        var reportCardAsLetter = await repository.GetFacilitatorLetterBlobContextAsync(FacilitatorId, 10, 6);
        reportCardAsLetter.Should().BeNull();

        var otherFacilitator = await repository.GetFacilitatorLetterBlobContextAsync(OtherFacilitatorId, 10, 5);
        otherFacilitator.Should().BeNull();
    }

    private static void AddStudent(
        FonbecWebDbContext db,
        int studentId,
        int facilitatorId,
        string firstName,
        string lastName,
        bool isDeleted = false,
        DateTime? universityStartYear = null)
    {
        db.Set<Student>().Add(new Student
        {
            Id = studentId,
            FirstName = firstName,
            LastName = lastName,
            FacilitatorId = facilitatorId,
            ChapterId = 1,
            IsDeleted = isDeleted,
            UniversityStartYear = universityStartYear,
            CreatedById = 1,
            CreatedOnUtc = new DateTime(2026, 1, 1),
        });
    }

    private static void AddPlan(FonbecWebDbContext db, int planId, DateTime startsOn)
    {
        db.Set<PlannedDelivery>().Add(new PlannedDelivery
        {
            Id = planId,
            StartsOn = startsOn,
            CreatedById = 1,
        });
    }

    private static void AddSponsor(FonbecWebDbContext db, int sponsorId, string firstName, string lastName)
    {
        db.Set<Sponsor>().Add(new Sponsor
        {
            Id = sponsorId,
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName}@example.com",
            PublicAccessToken = Guid.NewGuid(),
            ChapterId = 1,
            CreatedById = 1,
        });
    }

    private static void AddCompany(FonbecWebDbContext db, int companyId, string name)
    {
        db.Set<Company>().Add(new Company
        {
            Id = companyId,
            Name = name,
            PublicAccessToken = Guid.NewGuid(),
            CreatedById = 1,
        });
    }

    private static void AddLetter(
        FonbecWebDbContext db,
        long documentId,
        int studentId,
        int planId,
        DateTime uploadedOn,
        DocumentStatus status,
        int? sponsorId = null,
        int? companyId = null,
        FileKind fileKind = FileKind.Blob,
        int? rejectedReasonId = null,
        string? rejectionNotes = null,
        string? textContent = null,
        string? youTubeVideoId = null,
        int pageCount = 0)
    {
        var letter = new Letter
        {
            DocumentId = documentId,
            StudentId = studentId,
            ChapterId = 1,
            PlanId = planId,
            SponsorId = sponsorId,
            CompanyId = companyId,
            FileKind = fileKind,
            Status = status,
            UploadedOn = uploadedOn,
            UploadedById = FacilitatorId,
            RejectedReasonId = rejectedReasonId,
            RejectionNotes = rejectionNotes,
            TextContent = textContent,
            YouTubeVideoId = youTubeVideoId,
            RowVersion = [1],
        };

        AddPages(letter, documentId, pageCount, "letters");
        db.Set<Letter>().Add(letter);
    }

    private static void AddReportCard(
        FonbecWebDbContext db,
        long documentId,
        int studentId,
        DateOnly period,
        string description,
        DateTime uploadedOn,
        DocumentStatus status,
        FileKind fileKind = FileKind.Blob,
        int? rejectedReasonId = null,
        string? rejectionNotes = null,
        string? youTubeVideoId = null,
        int pageCount = 0)
    {
        var reportCard = new ReportCard
        {
            DocumentId = documentId,
            StudentId = studentId,
            ChapterId = 1,
            Period = period,
            Description = description,
            FileKind = fileKind,
            Status = status,
            UploadedOn = uploadedOn,
            UploadedById = FacilitatorId,
            RejectedReasonId = rejectedReasonId,
            RejectionNotes = rejectionNotes,
            YouTubeVideoId = youTubeVideoId,
            RowVersion = [1],
        };

        AddPages(reportCard, documentId, pageCount, "report-cards");
        db.Set<ReportCard>().Add(reportCard);
    }

    private static void AddPages(Document document, long documentId, int pageCount, string folder)
    {
        for (var page = 1; page <= pageCount; page++)
        {
            var blobId = documentId * 10 + page;
            document.Pages.Add(new DocumentPage
            {
                DocumentPageId = blobId,
                DocumentId = documentId,
                PageNumber = page,
                OriginalBlobPathId = blobId,
                OriginalBlobPath = new BlobPath
                {
                    BlobPathId = blobId,
                    StoragePath = $"{folder}/{documentId}/page-{page}.jpg",
                    MimeType = "image/jpeg",
                },
            });
        }
    }

    private static FacilitatorRepository CreateRepository(TestDbContextFactory factory) =>
        new(factory, TimeProvider.System);

    private static DocumentRepository CreateDocumentRepository(TestDbContextFactory factory) =>
        new(factory, TimeProvider.System, Microsoft.Extensions.Options.Options.Create(new DocumentQueueOptions()));

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private sealed class TestDbContextFactory(string databaseName)
        : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() =>
            new(CreateOptions());

        public Task<FonbecWebDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());

        private DbContextOptions<FonbecWebDbContext> CreateOptions() =>
            new DbContextOptionsBuilder<FonbecWebDbContext>()
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
    }
}