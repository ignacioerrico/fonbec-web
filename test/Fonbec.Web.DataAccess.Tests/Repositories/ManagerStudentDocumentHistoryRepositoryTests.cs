using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Options;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class ManagerStudentDocumentHistoryRepositoryTests
{
    private const int ChapterId = 1;
    private const int OtherChapterId = 2;

    [Fact]
    public async Task GetStudentDocumentHistoryAsync_Returns_Every_Type_Newest_First()
    {
        var factory = CreateDbContextFactory();
        var sameInstant = new DateTime(2026, 6, 12, 15, 0, 0, DateTimeKind.Utc);

        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            AddStudent(db, 10, ChapterId, "María", "González");
            AddStudent(db, 11, ChapterId, "Ana", "Becaria");

            db.Set<RejectedReason>().Add(new RejectedReason
            {
                Id = 3,
                Code = "blurry",
                Description = "Imagen borrosa",
            });

            db.Set<Sponsor>().Add(new Sponsor
            {
                Id = 4,
                FirstName = "Ana",
                LastName = "Pérez",
                Email = "ana@ejemplo.com",
                ChapterId = ChapterId,
                CreatedById = 1,
                PublicAccessToken = Guid.NewGuid(),
            });

            db.Set<Company>().Add(new Company
            {
                Id = 8,
                Name = "Acme SA",
                CreatedById = 1,
                PublicAccessToken = Guid.NewGuid(),
            });

            db.Set<Letter>().Add(new Letter
            {
                DocumentId = 1,
                StudentId = 10,
                ChapterId = ChapterId,
                PlanId = 1,
                SponsorId = 4,
                FileKind = FileKind.Text,
                Status = DocumentStatus.Approved,
                UploadedOn = new DateTime(2026, 3, 21),
                UploadedById = 1,
                TextContent = "Querido padrino",
                RowVersion = [1],
            });

            db.Set<Letter>().Add(new Letter
            {
                DocumentId = 2,
                StudentId = 10,
                ChapterId = ChapterId,
                PlanId = 1,
                CompanyId = 8,
                FileKind = FileKind.Blob,
                Status = DocumentStatus.ReviewPending,
                UploadedOn = sameInstant,
                UploadedById = 1,
                RowVersion = [1],
            });

            db.Set<ReportCard>().Add(new ReportCard
            {
                DocumentId = 3,
                StudentId = 10,
                ChapterId = ChapterId,
                Period = new DateOnly(2026, 6, 1),
                Description = "Boletín 1º trimestre",
                FileKind = FileKind.YouTube,
                Status = DocumentStatus.Approved,
                UploadedOn = sameInstant,
                UploadedById = 1,
                YouTubeVideoId = "abc123",
                RowVersion = [1],
            });

            AddOther(db, documentId: 4, studentId: 10, uploadedOn: new DateTime(2026, 5, 3),
                description: "Foto del acto escolar",
                status: DocumentStatus.Rejected,
                rejectedReasonId: 3,
                rejectionNotes: "falta el sello",
                pageCount: 2);

            AddOther(db, documentId: 9, studentId: 11, uploadedOn: new DateTime(2026, 7, 1),
                description: "De otro becario");

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var result = await CreateRepository(factory).GetStudentDocumentHistoryAsync(ChapterId, 10);

        result.Should().NotBeNull();
        result!.StudentName.Should().Be("María González");
        result.Items.Select(i => i.DocumentId).Should().Equal(3L, 2L, 4L, 1L);

        var letter = result.Items.Single(i => i.DocumentId == 1);
        letter.DocumentType.Should().Be(DocumentType.Letter);
        letter.RecipientName.Should().Be("Ana Pérez");
        letter.TextContent.Should().Be("Querido padrino");
        letter.Description.Should().BeNull();

        var companyLetter = result.Items.Single(i => i.DocumentId == 2);
        companyLetter.RecipientName.Should().Be("Acme SA");
        companyLetter.Status.Should().Be(DocumentStatus.ReviewPending);

        var reportCard = result.Items.Single(i => i.DocumentId == 3);
        reportCard.DocumentType.Should().Be(DocumentType.ReportCard);
        reportCard.Description.Should().Be("Boletín 1º trimestre");
        reportCard.ReportCardPeriod.Should().Be(new DateOnly(2026, 6, 1));
        reportCard.YouTubeVideoId.Should().Be("abc123");

        var rejected = result.Items.Single(i => i.DocumentId == 4);
        rejected.DocumentType.Should().Be(DocumentType.Other);
        rejected.RejectionReason.Should().Be("Imagen borrosa: falta el sello");
        rejected.PageCount.Should().Be(2);
    }

    [Fact]
    public async Task GetStudentDocumentHistoryAsync_Returns_Null_For_Another_Chapter()
    {
        var factory = CreateDbContextFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            AddStudent(db, 10, OtherChapterId, "María", "González");
            AddOther(db, documentId: 1, studentId: 10, uploadedOn: new DateTime(2026, 6, 1));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var result = await CreateRepository(factory).GetStudentDocumentHistoryAsync(ChapterId, 10);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStudentDocumentHistoryAsync_Returns_Null_When_Student_Is_Deleted()
    {
        var factory = CreateDbContextFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            AddStudent(db, 10, ChapterId, "María", "González", isDeleted: true);
            AddOther(db, documentId: 1, studentId: 10, uploadedOn: new DateTime(2026, 6, 1));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var result = await CreateRepository(factory).GetStudentDocumentHistoryAsync(ChapterId, 10);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStudentDocumentHistoryAsync_Includes_An_Inactive_Student()
    {
        var factory = CreateDbContextFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            AddStudent(db, 10, ChapterId, "María", "González");
            AddOther(db, documentId: 1, studentId: 10, uploadedOn: new DateTime(2026, 6, 1),
                description: "Sigue visible");
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var student = await db.Set<Student>().SingleAsync(s => s.Id == 10, TestContext.Current.CancellationToken);
            student.DisabledById = 1;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var result = await CreateRepository(factory).GetStudentDocumentHistoryAsync(ChapterId, 10);

        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle(i => i.DocumentId == 1 && i.Description == "Sigue visible");
    }

    [Fact]
    public async Task GetManagerStudentDocumentBlobContextAsync_Requires_The_Student_And_The_Document()
    {
        var factory = CreateDbContextFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            AddStudent(db, 10, ChapterId, "María", "González");
            AddStudent(db, 11, OtherChapterId, "Zoe", "Otra");
            AddOther(db, documentId: 5, studentId: 10, uploadedOn: new DateTime(2026, 6, 1), pageCount: 1);
            AddOther(db, documentId: 6, studentId: 11, uploadedOn: new DateTime(2026, 6, 1), pageCount: 1);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var repository = CreateRepository(factory);

        var owned = await repository.GetManagerStudentDocumentBlobContextAsync(ChapterId, 10, 5);
        owned.Should().NotBeNull();
        owned!.DocumentId.Should().Be(5);
        owned.Pages.Should().ContainSingle(p => p.PageNumber == 1);

        var otherStudent = await repository.GetManagerStudentDocumentBlobContextAsync(ChapterId, 10, 6);
        otherStudent.Should().BeNull();

        var otherChapter = await repository.GetManagerStudentDocumentBlobContextAsync(OtherChapterId, 10, 5);
        otherChapter.Should().BeNull();
    }

    private static void AddStudent(
        FonbecWebDbContext db,
        int studentId,
        int chapterId,
        string firstName,
        string lastName,
        bool isDeleted = false)
    {
        db.Set<Student>().Add(new Student
        {
            Id = studentId,
            FirstName = firstName,
            LastName = lastName,
            FacilitatorId = 2,
            ChapterId = chapterId,
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
        DocumentStatus status = DocumentStatus.Approved,
        int? rejectedReasonId = null,
        string? rejectionNotes = null,
        int pageCount = 0)
    {
        var document = new OtherDocument
        {
            DocumentId = documentId,
            StudentId = studentId,
            ChapterId = ChapterId,
            Description = description,
            FileKind = FileKind.Blob,
            Status = status,
            UploadedOn = uploadedOn,
            UploadedById = 1,
            RejectedReasonId = rejectedReasonId,
            RejectionNotes = rejectionNotes,
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

    private static DocumentRepository CreateRepository(TestDbContextFactory factory) =>
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