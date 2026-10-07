using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Options;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class FacilitatorOtherDocumentsRepositoryTests
{
    private const int FacilitatorId = 2;
    private const int OtherFacilitatorId = 9;

    [Fact]
    public async Task GetOtherDocumentCountsAsync_Counts_Other_Documents_Per_Student()
    {
        var factory = CreateDbContextFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            AddStudent(db, 10, FacilitatorId, "María", "González");
            AddStudent(db, 11, FacilitatorId, "Ana", "Becaria");
            AddStudent(db, 12, FacilitatorId, "Zoe", "Otra");

            AddOther(db, documentId: 1, studentId: 10, uploadedOn: new DateTime(2026, 3, 1));
            AddOther(db, documentId: 2, studentId: 10, uploadedOn: new DateTime(2026, 4, 1));
            AddOther(db, documentId: 3, studentId: 10, uploadedOn: new DateTime(2026, 5, 1));
            AddOther(db, documentId: 4, studentId: 11, uploadedOn: new DateTime(2026, 5, 1));
            AddOther(db, documentId: 5, studentId: 12, uploadedOn: new DateTime(2026, 5, 1));

            db.Set<ReportCard>().Add(new ReportCard
            {
                DocumentId = 20,
                StudentId = 10,
                ChapterId = 1,
                Period = new DateOnly(2026, 6, 1),
                Description = "Boletín",
                Status = DocumentStatus.Approved,
                UploadedOn = new DateTime(2026, 6, 1),
                UploadedById = FacilitatorId,
                RowVersion = [1],
            });

            db.Set<Letter>().Add(new Letter
            {
                DocumentId = 21,
                StudentId = 10,
                ChapterId = 1,
                PlanId = 1,
                SponsorId = 1,
                Status = DocumentStatus.Approved,
                UploadedOn = new DateTime(2026, 6, 1),
                UploadedById = FacilitatorId,
                RowVersion = [1],
            });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var result = await CreateRepository(factory).GetOtherDocumentCountsAsync([10, 11]);

        result.Should().Equal(new Dictionary<int, int>
        {
            [10] = 3,
            [11] = 1,
        });
        result.Should().NotContainKey(12);
    }

    [Fact]
    public async Task GetOtherDocumentCountsAsync_Returns_Empty_When_No_Students()
    {
        var factory = CreateDbContextFactory();

        var result = await CreateRepository(factory).GetOtherDocumentCountsAsync([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOtherDocumentsHistoryAsync_Returns_Newest_First_With_Description_Kind_Status_And_Reason()
    {
        var factory = CreateDbContextFactory();
        var sameInstant = new DateTime(2026, 6, 12, 15, 0, 0, DateTimeKind.Utc);

        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            AddStudent(db, 10, FacilitatorId, "María", "González");
            AddStudent(db, 11, FacilitatorId, "Ana", "Becaria");

            db.Set<RejectedReason>().Add(new RejectedReason
            {
                Id = 3,
                Code = "blurry",
                Description = "Imagen borrosa",
            });

            AddOther(db, documentId: 1, studentId: 10, uploadedOn: new DateTime(2026, 3, 21),
                description: "Foto del acto escolar",
                fileKind: FileKind.Blob,
                status: DocumentStatus.Rejected,
                rejectedReasonId: 3,
                rejectionNotes: "falta el sello",
                pageCount: 1);
            AddOther(db, documentId: 2, studentId: 10, uploadedOn: new DateTime(2026, 5, 3),
                description: "Nota de la familia",
                fileKind: FileKind.Text,
                status: DocumentStatus.ReviewPending,
                textContent: "Texto de la nota");
            AddOther(db, documentId: 3, studentId: 10, uploadedOn: sameInstant,
                description: "Mismo instante, id menor",
                fileKind: FileKind.YouTube,
                status: DocumentStatus.Approved,
                youTubeVideoId: "abc123");
            AddOther(db, documentId: 4, studentId: 10, uploadedOn: sameInstant,
                description: "Mismo instante, id mayor",
                fileKind: FileKind.Blob,
                status: DocumentStatus.Approved);
            AddOther(db, documentId: 8, studentId: 11, uploadedOn: new DateTime(2026, 6, 1),
                description: "De otro becario");

            db.Set<ReportCard>().Add(new ReportCard
            {
                DocumentId = 30,
                StudentId = 10,
                ChapterId = 1,
                Period = new DateOnly(2026, 6, 1),
                Description = "No es otro documento",
                Status = DocumentStatus.Approved,
                UploadedOn = new DateTime(2026, 6, 20),
                UploadedById = FacilitatorId,
                RowVersion = [1],
            });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var result = await CreateRepository(factory).GetOtherDocumentsHistoryAsync(FacilitatorId, 10);

        result.Should().NotBeNull();
        result!.StudentName.Should().Be("María González");
        result.Items.Select(i => i.DocumentId).Should().Equal(4L, 3L, 2L, 1L);

        var youtube = result.Items.Single(i => i.DocumentId == 3);
        youtube.Description.Should().Be("Mismo instante, id menor");
        youtube.FileKind.Should().Be(FileKind.YouTube);
        youtube.Status.Should().Be(DocumentStatus.Approved);
        youtube.YouTubeVideoId.Should().Be("abc123");

        var text = result.Items.Single(i => i.DocumentId == 2);
        text.FileKind.Should().Be(FileKind.Text);
        text.TextContent.Should().Be("Texto de la nota");
        text.PageCount.Should().Be(0);

        var rejected = result.Items.Single(i => i.DocumentId == 1);
        rejected.Status.Should().Be(DocumentStatus.Rejected);
        rejected.RejectionReason.Should().Be("Imagen borrosa: falta el sello");
        rejected.PageCount.Should().Be(1);
        rejected.Description.Should().Be("Foto del acto escolar");
    }

    [Fact]
    public async Task GetOtherDocumentsHistoryAsync_Returns_Null_For_Another_Facilitators_Student()
    {
        var factory = CreateDbContextFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            AddStudent(db, 10, OtherFacilitatorId, "María", "González");
            AddOther(db, documentId: 1, studentId: 10, uploadedOn: new DateTime(2026, 6, 1));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var result = await CreateRepository(factory).GetOtherDocumentsHistoryAsync(FacilitatorId, 10);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOtherDocumentsHistoryAsync_Returns_Null_When_Student_Is_Inactive()
    {
        var factory = CreateDbContextFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            AddStudent(db, 10, FacilitatorId, "María", "González");
            AddOther(db, documentId: 1, studentId: 10, uploadedOn: new DateTime(2026, 6, 1));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var student = await db.Set<Student>().SingleAsync(s => s.Id == 10, TestContext.Current.CancellationToken);
            student.DisabledById = 1;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var result = await CreateRepository(factory).GetOtherDocumentsHistoryAsync(FacilitatorId, 10);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOtherDocumentsHistoryAsync_Returns_Null_When_Student_Is_Deleted()
    {
        var factory = CreateDbContextFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            AddStudent(db, 10, FacilitatorId, "María", "González", isDeleted: true);
            AddOther(db, documentId: 1, studentId: 10, uploadedOn: new DateTime(2026, 6, 1));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var result = await CreateRepository(factory).GetOtherDocumentsHistoryAsync(FacilitatorId, 10);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFacilitatorOtherDocumentBlobContextAsync_Returns_Context_Only_For_The_Facilitators_Student()
    {
        var factory = CreateDbContextFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            AddStudent(db, 10, FacilitatorId, "María", "González");
            AddOther(db, documentId: 5, studentId: 10, uploadedOn: new DateTime(2026, 6, 1), pageCount: 1);

            db.Set<ReportCard>().Add(new ReportCard
            {
                DocumentId = 6,
                StudentId = 10,
                ChapterId = 1,
                Period = new DateOnly(2026, 6, 1),
                Description = "Boletín",
                Status = DocumentStatus.Approved,
                UploadedOn = new DateTime(2026, 6, 1),
                UploadedById = FacilitatorId,
                RowVersion = [1],
            });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var repository = CreateDocumentRepository(factory);

        var owned = await repository.GetFacilitatorOtherDocumentBlobContextAsync(FacilitatorId, 10, 5);
        owned.Should().NotBeNull();
        owned!.DocumentId.Should().Be(5);
        owned.DocumentType.Should().Be(DocumentType.Other);
        owned.Pages.Should().ContainSingle(p => p.PageNumber == 1);

        var otherFacilitator = await repository.GetFacilitatorOtherDocumentBlobContextAsync(OtherFacilitatorId, 10, 5);
        otherFacilitator.Should().BeNull();

        var reportCard = await repository.GetFacilitatorOtherDocumentBlobContextAsync(FacilitatorId, 10, 6);
        reportCard.Should().BeNull();
    }

    private static void AddStudent(
        FonbecWebDbContext db,
        int studentId,
        int facilitatorId,
        string firstName,
        string lastName,
        bool isDeleted = false)
    {
        db.Set<Student>().Add(new Student
        {
            Id = studentId,
            FirstName = firstName,
            LastName = lastName,
            FacilitatorId = facilitatorId,
            ChapterId = 1,
            IsDeleted = isDeleted,
            CreatedById = 1,
            CreatedOnUtc = new DateTime(2026, 1, 1),
        });
    }

    private static void AddOther(
        FonbecWebDbContext db,
        long documentId,
        int studentId,
        DateTime uploadedOn,
        string description = "Otro",
        FileKind fileKind = FileKind.Blob,
        DocumentStatus status = DocumentStatus.Approved,
        int? rejectedReasonId = null,
        string? rejectionNotes = null,
        string? textContent = null,
        string? youTubeVideoId = null,
        int pageCount = 0)
    {
        var document = new OtherDocument
        {
            DocumentId = documentId,
            StudentId = studentId,
            ChapterId = 1,
            Description = description,
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
                    StoragePath = $"other/{documentId}/page-{page}.jpg",
                    MimeType = "image/jpeg",
                },
            });
        }

        db.Set<OtherDocument>().Add(document);
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