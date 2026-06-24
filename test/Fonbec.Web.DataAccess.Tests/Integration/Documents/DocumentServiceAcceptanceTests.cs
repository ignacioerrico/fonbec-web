using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Documents.Input;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Fonbec.Web.DataAccess.Tests.Integration.Documents;

public class DocumentServiceAcceptanceTests
{
    private readonly DocumentTestFixture _fixture = new();

    [Fact]
    public async Task Scenario01_CreateLetterWithText_EnqueuesWithPendingStatus()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            _fixture.StudentId,
            _fixture.PlanId,
            _fixture.SponsorAId,
            _fixture.UploaderContext,
            FileKind.Text,
            TextContent: "Dear sponsor...",
            UploaderNotes: "Optional note"));

        result.IsSuccess.Should().BeTrue();
        var doc = await _fixture.GetDocumentAsync(result.Value!);
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.NotApplicable);
        doc.Status.Should().Be(DocumentStatus.Pending);
        doc.UploaderNotes.Should().Be("Optional note");

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<DocumentQueueItem>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task Scenario02_CreateOtherDocumentWithPlainTextBlob_DoesNotRequireImprovement()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateOtherDocumentAsync(new CreateOtherDocumentInputModel(
            _fixture.StudentId,
            _fixture.UploaderContext,
            FileKind.Blob,
            Blob: new CreateBlobPathInputModel("path/note.txt", "text/plain"),
            UploaderNotes: "Certificate"));

        result.IsSuccess.Should().BeTrue();
        var doc = await _fixture.GetDocumentAsync(result.Value!);
        doc.SponsorId.Should().BeNull();
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.NotApplicable);
        (await dbQueueCount()).Should().Be(1);

        async Task<int> dbQueueCount()
        {
            await using var db = await _fixture.Factory.CreateDbContextAsync();
            return await db.Set<DocumentQueueItem>().CountAsync();
        }
    }

    [Fact]
    public async Task Scenario03_CreateLetterWithJpg_RequiresImprovement()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            _fixture.StudentId,
            _fixture.PlanId,
            _fixture.SponsorAId,
            _fixture.UploaderContext,
            FileKind.Blob,
            Blob: new CreateBlobPathInputModel("path/letter.jpg", "image/jpeg")));

        result.IsSuccess.Should().BeTrue();
        var doc = await _fixture.GetDocumentAsync(result.Value!);
        doc.OriginalBlobPathId.Should().NotBeNull();
        doc.ImprovedBlobPathId.Should().BeNull();
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.Required);
        doc.Status.Should().Be(DocumentStatus.PendingImprovement);
    }

    [Fact]
    public async Task Scenario04_CreateLetterWithoutActivePlan_Fails()
    {
        await _fixture.InitializeAsync(includeActivePlan: false);

        var result = await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            _fixture.StudentId,
            PlanId: 1,
            _fixture.SponsorAId,
            _fixture.UploaderContext,
            FileKind.Text,
            TextContent: "Letter"));

        result.IsSuccess.Should().BeFalse();
        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<Document>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task Scenario05_CreateReportCardWithoutPlan_Succeeds()
    {
        await _fixture.InitializeAsync(includeActivePlan: false);

        var result = await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId,
            _fixture.UploaderContext,
            FileKind.Blob,
            Blob: new CreateBlobPathInputModel("path/report.pdf", "application/pdf")));

        result.IsSuccess.Should().BeTrue();
        var doc = await _fixture.GetDocumentAsync(result.Value!);
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.NotApplicable);
        doc.Status.Should().Be(DocumentStatus.Pending);
    }

    [Fact]
    public async Task Scenario06_CreateReportCardWithJpg_RequiresImprovement()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId,
            _fixture.UploaderContext,
            FileKind.Blob,
            Blob: new CreateBlobPathInputModel("path/report.jpg", "image/jpeg")));

        result.IsSuccess.Should().BeTrue();
        var doc = await _fixture.GetDocumentAsync(result.Value!);
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.Required);
        doc.Status.Should().Be(DocumentStatus.PendingImprovement);
    }

    [Fact]
    public async Task Scenario07_TakeNextForReview_SkipsUnimprovedImage()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            FileKind.Blob, Blob: new CreateBlobPathInputModel("a.jpg", "image/jpeg")));

        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Blob: new CreateBlobPathInputModel("b.pdf", "application/pdf")));

        var next = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");

        next.Should().NotBeNull();
        next!.DocumentType.Should().Be(DocumentType.ReportCard);

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var letter = await db.Set<Document>().FirstAsync(d => d.DocumentType == DocumentType.Letter, TestContext.Current.CancellationToken);
        letter.Status.Should().Be(DocumentStatus.PendingImprovement);
        (await db.Set<DocumentQueueItem>().Where(q => q.DocumentId == letter.DocumentId)
            .Select(q => q.ReviewLockedById).FirstAsync(TestContext.Current.CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task Scenario08_TakeNextForDigitalImprovement_ReturnsOldestImage()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            FileKind.Blob, Blob: new CreateBlobPathInputModel("a.jpg", "image/jpeg")));

        var next = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ReviewerId, "Reviewer", fonbecAuthClaim: null);

        next.Should().NotBeNull();
        next!.DocumentType.Should().Be(DocumentType.Letter);

        var doc = await _fixture.GetDocumentAsync(next.DocumentId);
        doc.ImprovementLockedById.Should().Be(_fixture.ReviewerId);
        doc.Status.Should().Be(DocumentStatus.ProcessingImprovement);
    }

    [Fact]
    public async Task Scenario09_ReviewerWithoutDigitalImprovementPermission_CannotTakeImprovementQueue()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            FileKind.Blob, Blob: new CreateBlobPathInputModel("a.jpg", "image/jpeg")));

        var act = () => _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ReviewerId, "Reviewer", fonbecAuthClaim: "DigitalImprovement");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Scenario10_SubmitDigitalImprovement_PreservesOriginal()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            FileKind.Blob, Blob: new CreateBlobPathInputModel("orig.jpg", "image/jpeg")));

        var locked = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ReviewerId, "Reviewer", null)!;

        var submit = await _fixture.DocumentService.SubmitDigitalImprovementAsync(
            new SubmitDigitalImprovementInputModel(
                locked!.DocumentId,
                _fixture.ReviewerId,
                "Reviewer",
                null,
                new CreateBlobPathInputModel("improved.jpg", "image/jpeg"),
                locked.RowVersion));

        submit.IsSuccess.Should().BeTrue();
        var doc = await _fixture.GetDocumentAsync(locked.DocumentId);
        doc.OriginalBlobPathId.Should().NotBeNull();
        doc.ImprovedBlobPathId.Should().NotBeNull();
        doc.BlobPathId.Should().Be(doc.ImprovedBlobPathId);
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.Complete);
        doc.Status.Should().Be(DocumentStatus.Pending);
        doc.ImprovementLockedById.Should().BeNull();
    }

    [Fact]
    public async Task Scenario11_ImprovedImageDocument_BecomesReviewEligible()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            FileKind.Blob, Blob: new CreateBlobPathInputModel("orig.jpg", "image/jpeg")));

        var locked = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ReviewerId, "Reviewer", null)!;

        await _fixture.DocumentService.SubmitDigitalImprovementAsync(new SubmitDigitalImprovementInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", null,
            new CreateBlobPathInputModel("improved.jpg", "image/jpeg"), locked.RowVersion));

        var next = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        next.Should().NotBeNull();
        next!.DocumentId.Should().Be(locked.DocumentId);
    }

    [Fact]
    public async Task Scenario12_ApproveLetter_CreatesShareAndSendsNotification()
    {
        await _fixture.InitializeAsync();

        var create = await CreatePendingLetterAsync();
        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;

        var approve = await _fixture.DocumentService.ApproveLetterAsync(new ApproveLetterInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            ConfirmedIsLetter: true,
            ConfirmedWrittenDate: DateTime.UtcNow.Date,
            ConfirmedAddressee: true,
            ConfirmedSignerMatchesStudent: true,
            SpellingScore: 4, PenmanshipScore: 4, ContentScore: 4,
            HasRedFlags: false, HasGreenFlags: true, IssuesNotes: null, Appraisal: "Good"));

        approve.IsSuccess.Should().BeTrue();

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<DocumentShare>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        var share = await db.Set<DocumentShare>().SingleAsync(TestContext.Current.CancellationToken);
        share.SponsorId.Should().Be(_fixture.SponsorAId);
        share.NotificationSentOn.Should().NotBeNull();

        await _fixture.EmailSender.Received(1).SendEmailAsync(
            Arg.Is<string>(e => e == "sponsor.a@test.com"),
            Arg.Any<string>(),
            Arg.Is<string>(html => html.Contains($"/padrinos/{_fixture.SponsorAToken}/{_fixture.StudentId}")));
    }

    [Fact]
    public async Task Scenario13_ApproveReportCard_SharesWithAllActiveSponsors()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Blob: new CreateBlobPathInputModel("r.pdf", "application/pdf")));

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;

        var approve = await _fixture.DocumentService.ApproveReportCardAsync(new ApproveReportCardInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            ConfirmedIsReportCardOrTranscript: true, ConfirmedStudentNameCorrect: true));

        approve.IsSuccess.Should().BeTrue();

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<DocumentShare>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
        await _fixture.EmailSender.Received(2).SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Scenario14_ApproveOtherDocument_SharesWithAllActiveSponsors()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateOtherDocumentAsync(new CreateOtherDocumentInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Text, TextContent: "Note"));

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;

        var approve = await _fixture.DocumentService.ApproveOtherDocumentAsync(new ApproveOtherDocumentInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion));

        approve.IsSuccess.Should().BeTrue();

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<DocumentShare>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
        await _fixture.EmailSender.Received(2).SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Scenario15_RejectedDocument_CreatesNoSharesOrEmails()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateOtherDocumentAsync(new CreateOtherDocumentInputModel(
            _fixture.StudentId, _fixture.UploaderContext, FileKind.Text, TextContent: "Note"));

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;

        await _fixture.DocumentService.RejectOtherDocumentAsync(new RejectOtherDocumentInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            RejectedReasonId: 9, RejectionNotes: null));

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<DocumentShare>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        await _fixture.EmailSender.DidNotReceive().SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Scenario16_RejectOtherWithoutReason_Fails()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateOtherDocumentAsync(new CreateOtherDocumentInputModel(
            _fixture.StudentId, _fixture.UploaderContext, FileKind.Text, TextContent: "Note"));

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;

        var result = await _fixture.DocumentService.RejectOtherDocumentAsync(new RejectOtherDocumentInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            RejectedReasonId: null, RejectionNotes: null));

        result.IsSuccess.Should().BeFalse();
        var doc = await _fixture.GetDocumentAsync(locked.DocumentId);
        doc.Status.Should().Be(DocumentStatus.Processing);
    }

    [Fact]
    public async Task Scenario17_ApproveLetterWithoutConfirmations_Fails()
    {
        await _fixture.InitializeAsync();

        await CreatePendingLetterAsync();
        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;

        var result = await _fixture.DocumentService.ApproveLetterAsync(new ApproveLetterInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            ConfirmedIsLetter: false,
            ConfirmedWrittenDate: DateTime.UtcNow.Date,
            ConfirmedAddressee: true,
            ConfirmedSignerMatchesStudent: true,
            SpellingScore: 4, PenmanshipScore: 4, ContentScore: 4,
            HasRedFlags: false, HasGreenFlags: true, IssuesNotes: null, Appraisal: "Good"));

        result.IsSuccess.Should().BeFalse();
        var doc = await _fixture.GetDocumentAsync(locked.DocumentId);
        doc.Status.Should().Be(DocumentStatus.Processing);
    }

    [Fact]
    public async Task Scenario18_ApproveReportCard_CreatesReportCardReviewWithoutAssessment()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Blob: new CreateBlobPathInputModel("r.pdf", "application/pdf")));

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;

        await _fixture.DocumentService.ApproveReportCardAsync(new ApproveReportCardInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            ConfirmedIsReportCardOrTranscript: true, ConfirmedStudentNameCorrect: true));

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<ReportCardReview>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        (await db.Set<Assessment>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task Scenario19_WrongApproveMethodForDocumentType_Fails()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Blob: new CreateBlobPathInputModel("r.pdf", "application/pdf")));

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;

        var result = await _fixture.DocumentService.ApproveLetterAsync(new ApproveLetterInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            ConfirmedIsLetter: true, ConfirmedWrittenDate: DateTime.UtcNow.Date,
            ConfirmedAddressee: true, ConfirmedSignerMatchesStudent: true,
            SpellingScore: 4, PenmanshipScore: 4, ContentScore: 4,
            HasRedFlags: false, HasGreenFlags: true, IssuesNotes: null, Appraisal: "Good"));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Scenario20_NoRetroactiveShareWhenSponsorAddedLater()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Blob: new CreateBlobPathInputModel("r.pdf", "application/pdf")));

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;
        await _fixture.DocumentService.ApproveReportCardAsync(new ApproveReportCardInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            ConfirmedIsReportCardOrTranscript: true, ConfirmedStudentNameCorrect: true));

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var shareCountBefore = await db.Set<DocumentShare>().CountAsync(TestContext.Current.CancellationToken);

        db.Set<Sponsorship>().Add(new Sponsorship
        {
            StudentId = _fixture.StudentId,
            SponsorId = 99,
            StartDate = DateTime.UtcNow,
            CreatedById = _fixture.UploaderId,
            CreatedOnUtc = DateTime.UtcNow,
            IsActive = true,
        });
        db.Set<Sponsor>().Add(new Sponsor
        {
            Id = 99,
            FirstName = "New",
            LastName = "Sponsor",
            Email = "new@test.com",
            ChapterId = _fixture.ChapterId,
            PublicAccessToken = Guid.NewGuid(),
            CreatedById = _fixture.UploaderId,
            CreatedOnUtc = DateTime.UtcNow,
            IsActive = true,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await db.Set<DocumentShare>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(shareCountBefore);
        (await db.Set<DocumentShare>().AnyAsync(s => s.SponsorId == 99, TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task Scenario21_GetSharedDocuments_ReturnsSponsorSpecificHistory()
    {
        await _fixture.InitializeAsync();

        await CreateAndApproveLetterForShareAsync();
        await _fixture.DocumentService.CreateOtherDocumentAsync(new CreateOtherDocumentInputModel(
            _fixture.StudentId, _fixture.UploaderContext, FileKind.Text, TextContent: "Other"));
        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;
        await _fixture.DocumentService.ApproveOtherDocumentAsync(new ApproveOtherDocumentInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion));

        var history = await _fixture.DocumentService.GetSharedDocumentsAsync(_fixture.SponsorAToken, _fixture.StudentId);

        history.IsAuthorized.Should().BeTrue();
        history.Documents.Should().HaveCount(2);
        history.Documents.Select(d => d.SharedOn).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Scenario22_InvalidSponsorToken_Denied()
    {
        await _fixture.InitializeAsync();

        var history = await _fixture.DocumentService.GetSharedDocumentsAsync(Guid.NewGuid(), _fixture.StudentId);
        history.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task Scenario23_UploaderCannotCreateForUnassignedStudent()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateOtherDocumentAsync(new CreateOtherDocumentInputModel(
            _fixture.StudentId, _fixture.OtherUploaderUserContext,
            FileKind.Text, TextContent: "Note"));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Scenario24_GlobalReviewProgress_ExcludesNonReviewable()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            FileKind.Blob, Blob: new CreateBlobPathInputModel("a.jpg", "image/jpeg")));

        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Blob: new CreateBlobPathInputModel("b.pdf", "application/pdf")));

        var progress = await _fixture.DocumentService.GetGlobalReviewProgressAsync(
            _fixture.ReviewerId, "Reviewer", planId: null);

        progress.PendingImprovement.Should().Be(1);
        progress.PendingReportCards.Should().Be(1);
    }

    [Fact]
    public async Task Scenario25_LetterPlanProgress_OnlyCountsLetters()
    {
        await _fixture.InitializeAsync();

        await CreatePendingLetterAsync();
        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Blob: new CreateBlobPathInputModel("r.pdf", "application/pdf")));
        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;
        await _fixture.DocumentService.ApproveReportCardAsync(new ApproveReportCardInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            ConfirmedIsReportCardOrTranscript: true, ConfirmedStudentNameCorrect: true));

        var progress = await _fixture.DocumentService.GetLetterPlanProgressAsync(
            _fixture.ManagerId, "Manager", _fixture.PlanId, _fixture.ChapterId);

        progress.TotalLetters.Should().Be(1);
        progress.PendingLetters.Should().Be(1);
        progress.ApprovedLetters.Should().Be(0);
    }

    [Fact]
    public async Task Scenario26_ReportCardTextContent_RejectedAtServiceLayer()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Text, TextContent: "Not allowed"));

        result.IsSuccess.Should().BeFalse();
        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<Document>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    private async Task<long> CreatePendingLetterAsync()
    {
        var result = await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            FileKind.Text, TextContent: "Dear sponsor"));
        return result.Value!;
    }

    private async Task CreateAndApproveLetterForShareAsync()
    {
        await CreatePendingLetterAsync();
        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;
        await _fixture.DocumentService.ApproveLetterAsync(new ApproveLetterInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            ConfirmedIsLetter: true, ConfirmedWrittenDate: DateTime.UtcNow.Date,
            ConfirmedAddressee: true, ConfirmedSignerMatchesStudent: true,
            SpellingScore: 4, PenmanshipScore: 4, ContentScore: 4,
            HasRedFlags: false, HasGreenFlags: true, IssuesNotes: null, Appraisal: "Good"));
    }
}