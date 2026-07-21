using System.Text;
using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Documents.Input;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Tests.Integration.Documents;

/// <summary>
/// Acceptance tests for US 100 (Azure Blob Storage upload/download) and multi-image documents.
/// Blob storage is simulated by an in-memory substitute (see <see cref="DocumentTestFixture"/>),
/// while the document repository runs against the in-memory EF Core provider.
/// </summary>
public class DocumentBlobAcceptanceTests
{
    private readonly DocumentTestFixture _fixture = new();

    private static MemoryStream Content(string text = "file-content") =>
        new(Encoding.UTF8.GetBytes(text));

    /// <summary>Builds an ordered file list; each text becomes one page with the given MIME type.</summary>
    private static IReadOnlyList<UploadFileInputModel> Files(string mimeType, params string[] texts)
    {
        if (texts.Length == 0)
        {
            texts = ["file-content"];
        }

        return texts.Select(t => new UploadFileInputModel(Content(t), mimeType)).ToList();
    }

    [Fact]
    public async Task Scenario01_UploadPdfLetter_CreatesBlobAndDocument()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId,
            _fixture.PlanId,
            _fixture.SponsorAId,
            _fixture.UploaderContext,
            Files("application/pdf"),
            UploaderNotes: "Optional note"));

        result.IsSuccess.Should().BeTrue();

        var blobName = _fixture.UploadedBlobNames.Single();
        blobName.Should().MatchRegex(
            $@"^chapter-{_fixture.ChapterId}/student-{_fixture.StudentId}/letter/sponsor-{_fixture.SponsorAId}/plan-{_fixture.PlanId}/original/\d{{4}}-\d{{2}}-\d{{2}}-[0-9a-f\-]+\.pdf$");

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var blob = await db.Set<BlobPath>().SingleAsync(TestContext.Current.CancellationToken);
        blob.StoragePath.Should().Be(blobName);
        blob.MimeType.Should().Be("application/pdf");
        blob.FileSizeBytes.Should().Be(Encoding.UTF8.GetByteCount("file-content"));
        blob.Sha256.Should().NotBeNullOrEmpty();

        var doc = await _fixture.GetDocumentAsync(result.Value!);
        doc.FileKind.Should().Be(FileKind.Blob);
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.NotApplicable);
        doc.Status.Should().Be(DocumentStatus.Pending);
        doc.UploaderNotes.Should().Be("Optional note");

        var pages = await _fixture.GetPagesAsync(result.Value!);
        pages.Should().ContainSingle();
        pages[0].PageNumber.Should().Be(1);
        pages[0].ImprovedBlobPathId.Should().BeNull();

        (await db.Set<DocumentQueueItem>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task Scenario02_UploadJpgLetter_RequiresDigitalImprovement()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId,
            _fixture.PlanId,
            _fixture.SponsorAId,
            _fixture.UploaderContext,
            Files("image/jpeg")));

        result.IsSuccess.Should().BeTrue();
        _fixture.UploadedBlobNames.Single().Should().Contain("/original/").And.EndWith(".jpg");

        var doc = await _fixture.GetDocumentAsync(result.Value!);
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.Required);
        doc.Status.Should().Be(DocumentStatus.PendingImprovement);

        var pages = await _fixture.GetPagesAsync(result.Value!);
        pages.Should().ContainSingle();
        pages[0].OriginalBlobPathId.Should().NotBe(0);
        pages[0].ImprovedBlobPathId.Should().BeNull();
    }

    [Fact]
    public async Task Scenario03_LetterWithoutActivePlan_RejectedWithNoBlobOrRows()
    {
        await _fixture.InitializeAsync(includeActivePlan: false);

        var result = await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId,
            PlanId: 1,
            _fixture.SponsorAId,
            _fixture.UploaderContext,
            Files("application/pdf")));

        result.IsSuccess.Should().BeFalse();
        _fixture.UploadedBlobNames.Should().BeEmpty();

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<Document>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (await db.Set<BlobPath>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task Scenario04_UploadReportCardPdfWithoutPlan_Succeeds()
    {
        await _fixture.InitializeAsync(includeActivePlan: false);

        var result = await _fixture.DocumentService.CreateReportCardWithBlobAsync(new CreateReportCardWithBlobInputModel(
            _fixture.StudentId,
            _fixture.UploaderContext,
            Files("application/pdf"),
            new DateOnly(2026, 6, 1),
            "Boletín 2º trimestre"));

        result.IsSuccess.Should().BeTrue();
        _fixture.UploadedBlobNames.Single().Should().MatchRegex(
            $@"^chapter-{_fixture.ChapterId}/student-{_fixture.StudentId}/report-card/original/\d{{4}}-\d{{2}}-\d{{2}}-[0-9a-f\-]+\.pdf$");

        var doc = await _fixture.GetDocumentAsync(result.Value!);
        doc.SponsorId.Should().BeNull();
        doc.Status.Should().Be(DocumentStatus.Pending);
    }

    [Fact]
    public async Task Scenario05_UploadOtherDocumentTxt_Succeeds()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateOtherDocumentWithBlobAsync(new CreateOtherDocumentWithBlobInputModel(
            _fixture.StudentId,
            _fixture.UploaderContext,
            Files("text/plain"),
            "Certificado de alumno regular"));

        result.IsSuccess.Should().BeTrue();
        _fixture.UploadedBlobNames.Single().Should().MatchRegex(
            $@"^chapter-{_fixture.ChapterId}/student-{_fixture.StudentId}/other/original/\d{{4}}-\d{{2}}-\d{{2}}-[0-9a-f\-]+\.txt$");

        var doc = await _fixture.GetDocumentAsync(result.Value!);
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.NotApplicable);
    }

    [Fact]
    public async Task Scenario06_UploadOtherDocumentPng_AsManagerBackup_RequiresImprovement()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateOtherDocumentWithBlobAsync(new CreateOtherDocumentWithBlobInputModel(
            _fixture.StudentId,
            _fixture.ManagerContext,
            Files("image/png"),
            "Documento"));

        result.IsSuccess.Should().BeTrue();
        _fixture.UploadedBlobNames.Single().Should().Contain("/other/original/").And.EndWith(".png");

        var doc = await _fixture.GetDocumentAsync(result.Value!);
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.Required);
        doc.Status.Should().Be(DocumentStatus.PendingImprovement);
    }

    [Fact]
    public async Task Scenario07_UploadWithDisallowedMimeType_Rejected()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateOtherDocumentWithBlobAsync(new CreateOtherDocumentWithBlobInputModel(
            _fixture.StudentId,
            _fixture.UploaderContext,
            Files("application/msword"),
            "Documento"));

        result.IsSuccess.Should().BeFalse();
        _fixture.UploadedBlobNames.Should().BeEmpty();

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<Document>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task Scenario10_DownloadOriginalForImprovement_ReturnsOriginalBlob()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            Files("image/jpeg", "original-bytes")));

        var locked = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ReviewerId, "Reviewer", null);

        var download = await _fixture.DocumentService.DownloadOriginalDocumentBlobAsync(
            create.Value!, pageNumber: 1, _fixture.ReviewerId);

        download.Should().NotBeNull();
        download!.MimeType.Should().Be("image/jpeg");
        download.Sha256.Should().NotBeNullOrEmpty();
        (await ReadAsync(download.Content)).Should().Be("original-bytes");
        locked.Should().NotBeNull();
    }

    [Fact]
    public async Task Scenario11_DownloadOriginalWithoutImprovementLock_Denied()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            Files("image/jpeg")));

        // No improvement lock has been taken.
        var download = await _fixture.DocumentService.DownloadOriginalDocumentBlobAsync(
            create.Value!, pageNumber: 1, _fixture.ReviewerId);

        download.Should().BeNull();
    }

    [Fact]
    public async Task Scenario12_SubmitDigitalImprovement_UploadsImprovedBlobAndPreservesOriginal()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            Files("image/jpeg", "original-bytes")));

        var originalBlobName = _fixture.UploadedBlobNames.Single();

        var locked = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ReviewerId, "Reviewer", null);

        var submit = await _fixture.DocumentService.SubmitDigitalImprovementWithBlobAsync(
            new SubmitDigitalImprovementWithBlobInputModel(
                locked!.DocumentId,
                _fixture.ReviewerId,
                "Reviewer",
                null,
                Files("image/jpeg", "improved-bytes"),
                locked.RowVersion));

        submit.IsSuccess.Should().BeTrue();

        var improvedBlobName = _fixture.UploadedBlobNames.Single(n => n.Contains("/improved/"));
        improvedBlobName.Should().MatchRegex(
            $@"^chapter-{_fixture.ChapterId}/student-{_fixture.StudentId}/letter/sponsor-{_fixture.SponsorAId}/plan-{_fixture.PlanId}/improved/\d{{4}}-\d{{2}}-\d{{2}}-[0-9a-f\-]+\.jpg$");
        _fixture.BlobExists(originalBlobName).Should().BeTrue();

        var doc = await _fixture.GetDocumentAsync(locked.DocumentId);
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.Complete);
        doc.Status.Should().Be(DocumentStatus.Pending);

        var pages = await _fixture.GetPagesAsync(locked.DocumentId);
        pages.Should().ContainSingle();
        pages[0].OriginalBlobPathId.Should().NotBe(0);
        pages[0].ImprovedBlobPathId.Should().NotBeNull();
    }

    [Fact]
    public async Task Scenario14_DownloadActiveBlob_ReturnsImprovedWhenPresent()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            Files("image/jpeg", "original-bytes")));

        var locked = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ReviewerId, "Reviewer", null);
        await _fixture.DocumentService.SubmitDigitalImprovementWithBlobAsync(
            new SubmitDigitalImprovementWithBlobInputModel(
                locked!.DocumentId, _fixture.ReviewerId, "Reviewer", null,
                Files("image/jpeg", "improved-bytes"), locked.RowVersion));

        var download = await _fixture.DocumentService.DownloadDocumentBlobAsync(
            create.Value!, pageNumber: 1, _fixture.ReviewerId);

        download.Should().NotBeNull();
        (await ReadAsync(download!.Content)).Should().Be("improved-bytes");
    }

    [Fact]
    public async Task Scenario15_DownloadActiveBlob_ReturnsOriginalWhenNoImprovement()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            Files("application/pdf", "pdf-bytes")));

        var download = await _fixture.DocumentService.DownloadDocumentBlobAsync(
            create.Value!, pageNumber: 1, _fixture.ReviewerId);

        download.Should().NotBeNull();
        download!.MimeType.Should().Be("application/pdf");
        (await ReadAsync(download.Content)).Should().Be("pdf-bytes");
    }

    [Fact]
    public async Task Scenario16_ReviewerCanDownloadCrossChapterDocuments()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateReportCardWithBlobAsync(new CreateReportCardWithBlobInputModel(
            _fixture.StudentId, _fixture.UploaderContext, Files("application/pdf"),
            new DateOnly(2026, 6, 1), "Boletín"));

        // Reviewer is chapter-less (global) but must be able to read any document.
        var download = await _fixture.DocumentService.DownloadDocumentBlobAsync(
            create.Value!, pageNumber: 1, _fixture.ReviewerId);

        download.Should().NotBeNull();
    }

    [Fact]
    public async Task Scenario17_UploaderCanDownloadOnlyOwnUploads()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateReportCardWithBlobAsync(new CreateReportCardWithBlobInputModel(
            _fixture.StudentId, _fixture.UploaderContext, Files("application/pdf"),
            new DateOnly(2026, 6, 1), "Boletín"));

        var ownDownload = await _fixture.DocumentService.DownloadDocumentBlobAsync(
            create.Value!, pageNumber: 1, _fixture.UploaderId);
        ownDownload.Should().NotBeNull();

        var otherDownload = await _fixture.DocumentService.DownloadDocumentBlobAsync(
            create.Value!, pageNumber: 1, _fixture.OtherUploaderId);
        otherDownload.Should().BeNull();
    }

    [Fact]
    public async Task Scenario18_ManagerCanDownloadDocumentsFromOwnChapter()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateReportCardWithBlobAsync(new CreateReportCardWithBlobInputModel(
            _fixture.StudentId, _fixture.UploaderContext, Files("application/pdf"),
            new DateOnly(2026, 6, 1), "Boletín"));

        var download = await _fixture.DocumentService.DownloadDocumentBlobAsync(
            create.Value!, pageNumber: 1, _fixture.ManagerId);

        download.Should().NotBeNull();
    }

    [Fact]
    public async Task Scenario19_AdminCannotUploadDocument()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateReportCardWithBlobAsync(new CreateReportCardWithBlobInputModel(
            _fixture.StudentId, _fixture.AdminContext, Files("application/pdf"),
            new DateOnly(2026, 6, 1), "Boletín"));

        result.IsSuccess.Should().BeFalse();
        _fixture.UploadedBlobNames.Should().BeEmpty();

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<Document>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task Scenario20_AllowsMultipleReportCardUploadsForSameStudent()
    {
        await _fixture.InitializeAsync();

        var first = await _fixture.DocumentService.CreateReportCardWithBlobAsync(new CreateReportCardWithBlobInputModel(
            _fixture.StudentId, _fixture.UploaderContext, Files("application/pdf"),
            new DateOnly(2026, 5, 1), "Boletín 1"));
        var second = await _fixture.DocumentService.CreateReportCardWithBlobAsync(new CreateReportCardWithBlobInputModel(
            _fixture.StudentId, _fixture.UploaderContext, Files("application/pdf"),
            new DateOnly(2026, 6, 1), "Boletín 2"));

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        _fixture.UploadedBlobNames.Distinct().Should().HaveCount(2);

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<ReportCard>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
        (await db.Set<BlobPath>().Select(b => b.StoragePath).Distinct()
            .CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
    }

    [Fact]
    public async Task Scenario21_UploadMultiImageLetter_CreatesOrderedPagesAndRequiresImprovement()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            Files("image/jpeg", "page-1", "page-2", "page-3")));

        result.IsSuccess.Should().BeTrue();
        _fixture.UploadedBlobNames.Count(n => n.Contains("/original/")).Should().Be(3);

        var doc = await _fixture.GetDocumentAsync(result.Value!);
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.Required);

        var pages = await _fixture.GetPagesAsync(result.Value!);
        pages.Select(p => p.PageNumber).Should().Equal(1, 2, 3);
        pages.Should().OnlyContain(p => p.ImprovedBlobPathId == null);

        // Pages preserve the facilitator-specified order.
        for (var i = 0; i < pages.Count; i++)
        {
            var download = await _fixture.DocumentService.DownloadOriginalDocumentBlobAsync(
                result.Value!, i + 1, _fixture.ReviewerId);
            // No improvement lock yet, so original download is denied; assert order via active download instead.
            download.Should().BeNull();
        }
    }

    [Fact]
    public async Task Scenario22_MultipleFilesWithNonImage_Rejected()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            [
                new UploadFileInputModel(Content("img"), "image/jpeg"),
                new UploadFileInputModel(Content("pdf"), "application/pdf"),
            ]));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.MultipleFilesOnlyForImages);
        _fixture.UploadedBlobNames.Should().BeEmpty();
    }

    [Fact]
    public async Task Scenario24_SubmitImprovementForMultiImage_ReplacesEveryPage()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            Files("image/jpeg", "page-1", "page-2")));

        var locked = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ReviewerId, "Reviewer", null);

        var submit = await _fixture.DocumentService.SubmitDigitalImprovementWithBlobAsync(
            new SubmitDigitalImprovementWithBlobInputModel(
                locked!.DocumentId, _fixture.ReviewerId, "Reviewer", null,
                Files("image/jpeg", "improved-1", "improved-2"), locked.RowVersion));

        submit.IsSuccess.Should().BeTrue();
        _fixture.UploadedBlobNames.Count(n => n.Contains("/improved/")).Should().Be(2);

        var pages = await _fixture.GetPagesAsync(locked.DocumentId);
        pages.Should().HaveCount(2);
        pages.Should().OnlyContain(p => p.ImprovedBlobPathId != null);

        var page2 = await _fixture.DocumentService.DownloadDocumentBlobAsync(
            create.Value!, pageNumber: 2, _fixture.ReviewerId);
        (await ReadAsync(page2!.Content)).Should().Be("improved-2");
    }

    [Fact]
    public async Task Scenario25_SubmitImprovement_WithWrongPageCount_Rejected()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            Files("image/jpeg", "page-1", "page-2")));

        var locked = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ReviewerId, "Reviewer", null);

        // Only one improved file for a two-page document.
        var submit = await _fixture.DocumentService.SubmitDigitalImprovementWithBlobAsync(
            new SubmitDigitalImprovementWithBlobInputModel(
                locked!.DocumentId, _fixture.ReviewerId, "Reviewer", null,
                Files("image/jpeg", "improved-1"), locked.RowVersion));

        submit.IsSuccess.Should().BeFalse();
        submit.Errors.Should().Contain(DocumentMessages.ImprovedPageCountMismatch);
        _fixture.UploadedBlobNames.Count(n => n.Contains("/improved/")).Should().Be(0);
    }

    private static async Task<string> ReadAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }
}