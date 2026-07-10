using System.Text;
using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Documents.Input;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Tests.Integration.Documents;

/// <summary>
/// Acceptance tests for US 100 (Azure Blob Storage upload/download). Blob storage is
/// simulated by an in-memory substitute (see <see cref="DocumentTestFixture"/>), while the
/// document repository runs against the in-memory EF Core provider.
/// </summary>
public class DocumentBlobAcceptanceTests
{
    private readonly DocumentTestFixture _fixture = new();

    private static MemoryStream Content(string text = "file-content") =>
        new(Encoding.UTF8.GetBytes(text));

    [Fact]
    public async Task Scenario01_UploadPdfLetter_CreatesBlobAndDocument()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId,
            _fixture.PlanId,
            _fixture.SponsorAId,
            _fixture.UploaderContext,
            Content(),
            "application/pdf",
            UploaderNotes: "Optional note"));

        result.IsSuccess.Should().BeTrue();

        var blobName = _fixture.UploadedBlobNames.Single();
        blobName.Should().MatchRegex(
            $@"^{_fixture.ChapterId}/{_fixture.PlanId}/{_fixture.StudentId}/{_fixture.SponsorAId}/original/[0-9a-f\-]+\.pdf$");

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
            Content(),
            "image/jpeg"));

        result.IsSuccess.Should().BeTrue();
        _fixture.UploadedBlobNames.Single().Should().Contain("/original/").And.EndWith(".jpg");

        var doc = await _fixture.GetDocumentAsync(result.Value!);
        doc.OriginalBlobPathId.Should().NotBeNull();
        doc.BlobPathId.Should().Be(doc.OriginalBlobPathId);
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.Required);
        doc.Status.Should().Be(DocumentStatus.PendingImprovement);
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
            Content(),
            "application/pdf"));

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
            Content(),
            "application/pdf",
            new DateOnly(2026, 6, 1),
            "Boletín 2º trimestre"));

        result.IsSuccess.Should().BeTrue();
        _fixture.UploadedBlobNames.Single().Should().MatchRegex(
            $@"^{_fixture.ChapterId}/{_fixture.StudentId}/report-card/original/[0-9a-f\-]+\.pdf$");

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
            Content(),
            "text/plain",
            "Certificado de alumno regular"));

        result.IsSuccess.Should().BeTrue();
        _fixture.UploadedBlobNames.Single().Should().MatchRegex(
            $@"^{_fixture.ChapterId}/{_fixture.StudentId}/other/original/[0-9a-f\-]+\.txt$");

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
            Content(),
            "image/png",
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
            Content(),
            "application/msword",
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
            Content("original-bytes"), "image/jpeg"));

        var locked = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ReviewerId, "Reviewer", null);

        var download = await _fixture.DocumentService.DownloadOriginalDocumentBlobAsync(
            create.Value!, _fixture.ReviewerId);

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
            Content(), "image/jpeg"));

        // No improvement lock has been taken.
        var download = await _fixture.DocumentService.DownloadOriginalDocumentBlobAsync(
            create.Value!, _fixture.ReviewerId);

        download.Should().BeNull();
    }

    [Fact]
    public async Task Scenario12_SubmitDigitalImprovement_UploadsImprovedBlobAndPreservesOriginal()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            Content("original-bytes"), "image/jpeg"));

        var originalBlobName = _fixture.UploadedBlobNames.Single();

        var locked = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ReviewerId, "Reviewer", null);

        var submit = await _fixture.DocumentService.SubmitDigitalImprovementWithBlobAsync(
            new SubmitDigitalImprovementWithBlobInputModel(
                locked!.DocumentId,
                _fixture.ReviewerId,
                "Reviewer",
                null,
                Content("improved-bytes"),
                "image/jpeg",
                locked.RowVersion));

        submit.IsSuccess.Should().BeTrue();

        var improvedBlobName = _fixture.UploadedBlobNames.Single(n => n.Contains("/improved/"));
        improvedBlobName.Should().MatchRegex(
            $@"^{_fixture.ChapterId}/{_fixture.PlanId}/{_fixture.StudentId}/{_fixture.SponsorAId}/improved/[0-9a-f\-]+\.jpg$");
        _fixture.BlobExists(originalBlobName).Should().BeTrue();

        var doc = await _fixture.GetDocumentAsync(locked.DocumentId);
        doc.OriginalBlobPathId.Should().NotBeNull();
        doc.ImprovedBlobPathId.Should().NotBeNull();
        doc.BlobPathId.Should().Be(doc.ImprovedBlobPathId);
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.Complete);
        doc.Status.Should().Be(DocumentStatus.Pending);
    }

    [Fact]
    public async Task Scenario14_DownloadActiveBlob_ReturnsImprovedWhenPresent()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            Content("original-bytes"), "image/jpeg"));

        var locked = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ReviewerId, "Reviewer", null);
        await _fixture.DocumentService.SubmitDigitalImprovementWithBlobAsync(
            new SubmitDigitalImprovementWithBlobInputModel(
                locked!.DocumentId, _fixture.ReviewerId, "Reviewer", null,
                Content("improved-bytes"), "image/jpeg", locked.RowVersion));

        var download = await _fixture.DocumentService.DownloadDocumentBlobAsync(
            create.Value!, _fixture.ReviewerId);

        download.Should().NotBeNull();
        (await ReadAsync(download!.Content)).Should().Be("improved-bytes");
    }

    [Fact]
    public async Task Scenario15_DownloadActiveBlob_ReturnsOriginalWhenNoImprovement()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            Content("pdf-bytes"), "application/pdf"));

        var download = await _fixture.DocumentService.DownloadDocumentBlobAsync(
            create.Value!, _fixture.ReviewerId);

        download.Should().NotBeNull();
        download!.MimeType.Should().Be("application/pdf");
        (await ReadAsync(download.Content)).Should().Be("pdf-bytes");
    }

    [Fact]
    public async Task Scenario16_ReviewerCanDownloadCrossChapterDocuments()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateReportCardWithBlobAsync(new CreateReportCardWithBlobInputModel(
            _fixture.StudentId, _fixture.UploaderContext, Content(), "application/pdf",
            new DateOnly(2026, 6, 1), "Boletín"));

        // Reviewer is chapter-less (global) but must be able to read any document.
        var download = await _fixture.DocumentService.DownloadDocumentBlobAsync(
            create.Value!, _fixture.ReviewerId);

        download.Should().NotBeNull();
    }

    [Fact]
    public async Task Scenario17_UploaderCanDownloadOnlyOwnUploads()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateReportCardWithBlobAsync(new CreateReportCardWithBlobInputModel(
            _fixture.StudentId, _fixture.UploaderContext, Content(), "application/pdf",
            new DateOnly(2026, 6, 1), "Boletín"));

        var ownDownload = await _fixture.DocumentService.DownloadDocumentBlobAsync(
            create.Value!, _fixture.UploaderId);
        ownDownload.Should().NotBeNull();

        var otherDownload = await _fixture.DocumentService.DownloadDocumentBlobAsync(
            create.Value!, _fixture.OtherUploaderId);
        otherDownload.Should().BeNull();
    }

    [Fact]
    public async Task Scenario18_ManagerCanDownloadDocumentsFromOwnChapter()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateReportCardWithBlobAsync(new CreateReportCardWithBlobInputModel(
            _fixture.StudentId, _fixture.UploaderContext, Content(), "application/pdf",
            new DateOnly(2026, 6, 1), "Boletín"));

        var download = await _fixture.DocumentService.DownloadDocumentBlobAsync(
            create.Value!, _fixture.ManagerId);

        download.Should().NotBeNull();
    }

    [Fact]
    public async Task Scenario19_AdminCannotUploadDocument()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateReportCardWithBlobAsync(new CreateReportCardWithBlobInputModel(
            _fixture.StudentId, _fixture.AdminContext, Content(), "application/pdf",
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
            _fixture.StudentId, _fixture.UploaderContext, Content(), "application/pdf",
            new DateOnly(2026, 5, 1), "Boletín 1"));
        var second = await _fixture.DocumentService.CreateReportCardWithBlobAsync(new CreateReportCardWithBlobInputModel(
            _fixture.StudentId, _fixture.UploaderContext, Content(), "application/pdf",
            new DateOnly(2026, 6, 1), "Boletín 2"));

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        _fixture.UploadedBlobNames.Distinct().Should().HaveCount(2);

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<ReportCard>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
        (await db.Set<BlobPath>().Select(b => b.StoragePath).Distinct()
            .CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
    }

    private static async Task<string> ReadAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }
}