using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
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
            Description: "Certificado de alumno regular",
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
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.Required);
        doc.Status.Should().Be(DocumentStatus.PendingImprovement);

        var pages = await _fixture.GetPagesAsync(result.Value!);
        pages.Should().ContainSingle();
        pages[0].OriginalBlobPathId.Should().NotBe(0);
        pages[0].ImprovedBlobPathId.Should().BeNull();
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
            Period: new DateOnly(2026, 6, 1),
            Description: "Boletín 2º trimestre",
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
            Period: new DateOnly(2026, 6, 1),
            Description: "Boletín 2º trimestre",
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
            FileKind.Blob, Period: new DateOnly(2026, 5, 1), Description: "Boletín 1º trimestre",
            Blob: new CreateBlobPathInputModel("b.pdf", "application/pdf")));

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
                [new CreateBlobPathInputModel("improved.jpg", "image/jpeg")],
                locked.RowVersion));

        submit.IsSuccess.Should().BeTrue();
        var doc = await _fixture.GetDocumentAsync(locked.DocumentId);
        doc.DigitalImprovementStatus.Should().Be(DigitalImprovementStatus.Complete);
        doc.Status.Should().Be(DocumentStatus.Pending);
        doc.ImprovementLockedById.Should().BeNull();

        var pages = await _fixture.GetPagesAsync(locked.DocumentId);
        pages.Should().ContainSingle();
        pages[0].OriginalBlobPathId.Should().NotBe(0);
        pages[0].ImprovedBlobPathId.Should().NotBeNull();
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
            [new CreateBlobPathInputModel("improved.jpg", "image/jpeg")], locked.RowVersion));

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
    public async Task Scenario12b_ApproveCompanyLetter_SharesWithLinkedSponsorsAndNotifiesCompany()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            _fixture.CompanyStudentId, _fixture.PlanId, SponsorId: null, _fixture.UploaderContext,
            FileKind.Text, TextContent: "Dear company", CompanyId: _fixture.CompanyId));

        create.IsSuccess.Should().BeTrue();

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;

        var approve = await _fixture.DocumentService.ApproveLetterAsync(new ApproveLetterInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            ConfirmedIsLetter: true, ConfirmedWrittenDate: DateTime.UtcNow.Date,
            ConfirmedAddressee: true, ConfirmedSignerMatchesStudent: true,
            SpellingScore: 4, PenmanshipScore: 4, ContentScore: 4,
            HasRedFlags: false, HasGreenFlags: true, IssuesNotes: null, Appraisal: "Good"));

        approve.IsSuccess.Should().BeTrue();

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        // Two shares: one for the company itself and one for its linked individual sponsor.
        var shares = await db.Set<DocumentShare>().ToListAsync(TestContext.Current.CancellationToken);
        shares.Should().HaveCount(2);
        shares.Should().ContainSingle(s => s.CompanyId == _fixture.CompanyId && s.SponsorId == null);
        shares.Should().ContainSingle(s => s.SponsorId == _fixture.CompanyLinkedSponsorId && s.CompanyId == null);
        shares.Should().OnlyContain(s => s.NotificationSentOn != null);

        var letter = await db.Set<Letter>().SingleAsync(TestContext.Current.CancellationToken);
        letter.CompanyId.Should().Be(_fixture.CompanyId);

        // The linked sponsor and the company itself are both emailed, each with its own history link.
        await _fixture.EmailSender.Received(1).SendEmailAsync(
            Arg.Is<string>(e => e == DocumentTestFixture.CompanyLinkedSponsorEmail),
            Arg.Any<string>(),
            Arg.Is<string>(html => html.Contains($"/padrinos/{_fixture.CompanyLinkedSponsorToken}/")));
        await _fixture.EmailSender.Received(1).SendEmailAsync(
            Arg.Is<string>(e => e == DocumentTestFixture.CompanyEmail),
            Arg.Any<string>(),
            Arg.Is<string>(html => html.Contains("Acme SA") && html.Contains("/empresas/")));
    }

    [Fact]
    public async Task Scenario13_ApproveReportCard_SharesWithAllActiveSponsors()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Period: new DateOnly(2026, 6, 1), Description: "Boletín 2º trimestre",
            Blob: new CreateBlobPathInputModel("r.pdf", "application/pdf")));

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;

        var approve = await _fixture.DocumentService.ApproveReportCardAsync(new ApproveReportCardInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            ConfirmedIsReportCardOrTranscript: true, ConfirmedPeriodMatches: true,
            ConfirmedStudentNameCorrect: true, ReportCardAssessment.Green, Absences: 3));

        approve.IsSuccess.Should().BeTrue();

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<DocumentShare>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
        await _fixture.EmailSender.Received(2).SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Scenario13b_ApproveReportCard_ForCompanySponsoredStudent_SharesWithCompanyAndLinkedSponsors()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.CompanyStudentId, _fixture.UploaderContext,
            FileKind.Blob, Period: new DateOnly(2026, 6, 1), Description: "Boletín 2º trimestre",
            Blob: new CreateBlobPathInputModel("r.pdf", "application/pdf")));

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;

        var approve = await _fixture.DocumentService.ApproveReportCardAsync(new ApproveReportCardInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            ConfirmedIsReportCardOrTranscript: true, ConfirmedPeriodMatches: true,
            ConfirmedStudentNameCorrect: true, ReportCardAssessment.Green, Absences: 0));

        approve.IsSuccess.Should().BeTrue();

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        // A report card for a company-sponsored student reaches both the company and its linked sponsor.
        var shares = await db.Set<DocumentShare>().ToListAsync(TestContext.Current.CancellationToken);
        shares.Should().HaveCount(2);
        shares.Should().ContainSingle(s => s.CompanyId == _fixture.CompanyId && s.SponsorId == null);
        shares.Should().ContainSingle(s => s.SponsorId == _fixture.CompanyLinkedSponsorId && s.CompanyId == null);

        await _fixture.EmailSender.Received(1).SendEmailAsync(
            Arg.Is<string>(e => e == DocumentTestFixture.CompanyEmail), Arg.Any<string>(), Arg.Any<string>());
        await _fixture.EmailSender.Received(1).SendEmailAsync(
            Arg.Is<string>(e => e == DocumentTestFixture.CompanyLinkedSponsorEmail), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Scenario14_ApproveOtherDocument_SharesWithAllActiveSponsors()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateOtherDocumentAsync(new CreateOtherDocumentInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Text, Description: "Constancia", TextContent: "Note"));

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
            _fixture.StudentId, _fixture.UploaderContext, FileKind.Text, Description: "Constancia", TextContent: "Note"));

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;

        await _fixture.DocumentService.RejectOtherDocumentAsync(new RejectOtherDocumentInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            RejectedReasonId: RejectedReasonIds.Unreadable, RejectionNotes: null));

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<DocumentShare>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        await _fixture.EmailSender.DidNotReceive().SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Scenario16_RejectOtherWithoutReason_Fails()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateOtherDocumentAsync(new CreateOtherDocumentInputModel(
            _fixture.StudentId, _fixture.UploaderContext, FileKind.Text, Description: "Constancia", TextContent: "Note"));

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
    public async Task Scenario18_ApproveReportCard_PersistsReviewAssessmentAndAbsences()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Period: new DateOnly(2026, 6, 1), Description: "Boletín 2º trimestre",
            Blob: new CreateBlobPathInputModel("r.pdf", "application/pdf")));

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;

        await _fixture.DocumentService.ApproveReportCardAsync(new ApproveReportCardInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            ConfirmedIsReportCardOrTranscript: true, ConfirmedPeriodMatches: true,
            ConfirmedStudentNameCorrect: true, ReportCardAssessment.Yellow, Absences: 4));

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var review = await db.Set<ReportCardReview>().SingleAsync(TestContext.Current.CancellationToken);
        review.ConfirmedPeriodMatches.Should().BeTrue();
        review.OverallAssessment.Should().Be(ReportCardAssessment.Yellow);
        review.Absences.Should().Be(4);
        (await db.Set<Assessment>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task Scenario19_WrongApproveMethodForDocumentType_Fails()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Period: new DateOnly(2026, 6, 1), Description: "Boletín 2º trimestre",
            Blob: new CreateBlobPathInputModel("r.pdf", "application/pdf")));

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
            FileKind.Blob, Period: new DateOnly(2026, 6, 1), Description: "Boletín 2º trimestre",
            Blob: new CreateBlobPathInputModel("r.pdf", "application/pdf")));

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;
        await _fixture.DocumentService.ApproveReportCardAsync(new ApproveReportCardInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            ConfirmedIsReportCardOrTranscript: true, ConfirmedPeriodMatches: true,
            ConfirmedStudentNameCorrect: true, ReportCardAssessment.Green, Absences: 0));

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
            _fixture.StudentId, _fixture.UploaderContext, FileKind.Text, Description: "Constancia", TextContent: "Other"));
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
            FileKind.Text, Description: "Constancia", TextContent: "Note"));

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
            FileKind.Blob, Period: new DateOnly(2026, 5, 1), Description: "Boletín 1º trimestre",
            Blob: new CreateBlobPathInputModel("b.pdf", "application/pdf")));

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
            FileKind.Blob, Period: new DateOnly(2026, 6, 1), Description: "Boletín 2º trimestre",
            Blob: new CreateBlobPathInputModel("r.pdf", "application/pdf")));
        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer")!;
        await _fixture.DocumentService.ApproveReportCardAsync(new ApproveReportCardInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            ConfirmedIsReportCardOrTranscript: true, ConfirmedPeriodMatches: true,
            ConfirmedStudentNameCorrect: true, ReportCardAssessment.Green, Absences: 0));

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
            FileKind.Text, Period: new DateOnly(2026, 6, 1), Description: "Boletín 2º trimestre",
            TextContent: "Not allowed"));

        result.IsSuccess.Should().BeFalse();
        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<Document>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task Scenario28_ReportCard_StoresPeriodAndDescription()
    {
        await _fixture.InitializeAsync();

        var period = new DateOnly(2026, 6, 1);
        var result = await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Period: period, Description: "Boletín 2º trimestre",
            Blob: new CreateBlobPathInputModel("r.pdf", "application/pdf")));

        result.IsSuccess.Should().BeTrue();
        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var reportCard = await db.Set<ReportCard>().SingleAsync(TestContext.Current.CancellationToken);
        reportCard.Period.Should().Be(period);
        reportCard.Description.Should().Be("Boletín 2º trimestre");
    }

    [Fact]
    public async Task Scenario29_OtherDocument_RequiresDescription()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateOtherDocumentAsync(new CreateOtherDocumentInputModel(
            _fixture.StudentId, _fixture.UploaderContext, FileKind.Text, Description: "  ", TextContent: "Note"));

        result.IsSuccess.Should().BeFalse();
        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<Document>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task Scenario28b_ReportCard_RequiresPeriod()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Period: default, Description: "Boletín 2º trimestre",
            Blob: new CreateBlobPathInputModel("r.pdf", "application/pdf")));

        result.IsSuccess.Should().BeFalse();
        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<Document>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task Scenario30_DescriptionIsDenormalizedCopy()
    {
        await _fixture.InitializeAsync();

        var options = await _fixture.DocumentService.GetDescriptionOptionsAsync(
            _fixture.ChapterId, DocumentType.ReportCard);
        var chosen = options.First().Text;

        var result = await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Period: new DateOnly(2026, 6, 1), Description: chosen,
            Blob: new CreateBlobPathInputModel("r.pdf", "application/pdf")));

        result.IsSuccess.Should().BeTrue();
        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var reportCard = await db.Set<ReportCard>().SingleAsync(TestContext.Current.CancellationToken);
        // Stored as a copy of the option text, independent of the option row.
        reportCard.Description.Should().Be(chosen);
    }

    [Fact]
    public async Task GetDescriptionOptions_ReturnsGlobalDefaultsForType()
    {
        await _fixture.InitializeAsync();

        var reportCardOptions = await _fixture.DocumentService.GetDescriptionOptionsAsync(
            _fixture.ChapterId, DocumentType.ReportCard);
        var otherOptions = await _fixture.DocumentService.GetDescriptionOptionsAsync(
            _fixture.ChapterId, DocumentType.Other);

        reportCardOptions.Should().HaveCount(5);
        reportCardOptions.Should().BeInAscendingOrder(o => o.SortOrder);
        otherOptions.Should().HaveCount(4);
    }

    [Fact]
    public async Task Scenario31_TakeNextForReview_ReTakesDocumentAfterLockExpires()
    {
        await _fixture.InitializeAsync();

        // A single review-ready document (PDF report card needs no digital improvement).
        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Period: new DateOnly(2026, 5, 1), Description: "Boletín 1º trimestre",
            Blob: new CreateBlobPathInputModel("a.pdf", "application/pdf")));

        // Reviewer takes it for review.
        var first = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        first.Should().NotBeNull();

        // While the lock is still valid, another reviewer taking "next" gets nothing.
        var blockedWhileLocked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ManagerId, "Manager");
        blockedWhileLocked.Should().BeNull();

        // 41 minutes pass with no approve/reject: the lock is now stale (timeout is 40 min).
        await _fixture.ExpireReviewLockAsync(first!.DocumentId, TimeSpan.FromMinutes(41));

        // The next reviewer re-takes the same document.
        var reTaken = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ManagerId, "Manager");
        reTaken.Should().NotBeNull();
        reTaken!.DocumentId.Should().Be(first.DocumentId);

        var doc = await _fixture.GetDocumentAsync(reTaken.DocumentId);
        doc.Status.Should().Be(DocumentStatus.Processing);

        var queueItem = await _fixture.GetQueueItemAsync(reTaken.DocumentId);
        queueItem.ReviewLockedById.Should().Be(_fixture.ManagerId);
        queueItem.DequeueCount.Should().Be(2);
    }

    [Fact]
    public async Task Scenario32_TakeNextForReview_ReTakesEarliestExpired_EvenWhenLaterDocumentStillLocked()
    {
        await _fixture.InitializeAsync();

        // Two review-ready documents, enqueued in order.
        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Period: new DateOnly(2026, 5, 1), Description: "Primero",
            Blob: new CreateBlobPathInputModel("first.pdf", "application/pdf")));
        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Period: new DateOnly(2026, 6, 1), Description: "Segundo",
            Blob: new CreateBlobPathInputModel("second.pdf", "application/pdf")));

        // Reviewer A takes the first (oldest); Manager takes the second.
        var firstDoc = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        var secondDoc = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ManagerId, "Manager");
        firstDoc.Should().NotBeNull();
        secondDoc.Should().NotBeNull();
        firstDoc!.DocumentId.Should().NotBe(secondDoc!.DocumentId);

        // The first document's lock expires; the second remains validly locked.
        await _fixture.ExpireReviewLockAsync(firstDoc.DocumentId, TimeSpan.FromMinutes(41));

        // "Take next" re-locks the earliest free document (the expired first one),
        // even though a later document in the queue is still locked.
        var reTaken = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        reTaken.Should().NotBeNull();
        reTaken!.DocumentId.Should().Be(firstDoc.DocumentId);
    }

    [Fact]
    public async Task Scenario32b_TakeNextForReview_WithActiveLock_ResumesSameDocumentWithoutResettingTimer()
    {
        await _fixture.InitializeAsync();

        // Two review-ready documents.
        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Period: new DateOnly(2026, 5, 1), Description: "Primero",
            Blob: new CreateBlobPathInputModel("first.pdf", "application/pdf")));
        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Period: new DateOnly(2026, 6, 1), Description: "Segundo",
            Blob: new CreateBlobPathInputModel("second.pdf", "application/pdf")));

        // Reviewer takes the first document.
        var first = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        first.Should().NotBeNull();
        var lockedAtAfterFirstTake = (await _fixture.GetQueueItemAsync(first!.DocumentId)).ReviewLockedAt;

        // Taking "next" again while still holding a valid lock resumes the SAME document — a reviewer
        // may only hold one at a time — and must not take the second document.
        var resumed = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        resumed.Should().NotBeNull();
        resumed!.DocumentId.Should().Be(first.DocumentId);

        var queueItem = await _fixture.GetQueueItemAsync(first.DocumentId);
        // The original lock timestamp is preserved (the countdown continues; it is not reset)...
        queueItem.ReviewLockedAt.Should().Be(lockedAtAfterFirstTake);
        // ...and resuming does not count as a fresh dequeue.
        queueItem.DequeueCount.Should().Be(1);
    }

    [Fact]
    public async Task Scenario32c_GetActiveReviewLockedDocumentId_ReturnsValidLock_NullAfterExpiry()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Period: new DateOnly(2026, 5, 1), Description: "Boletín",
            Blob: new CreateBlobPathInputModel("a.pdf", "application/pdf")));

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        locked.Should().NotBeNull();

        var active = await _fixture.DocumentRepository.GetActiveReviewLockedDocumentIdAsync(_fixture.ReviewerId);
        active.Should().Be(locked!.DocumentId);

        // Once the lock has expired it is no longer considered an active hold.
        await _fixture.ExpireReviewLockAsync(locked.DocumentId, TimeSpan.FromMinutes(41));

        var afterExpiry = await _fixture.DocumentRepository.GetActiveReviewLockedDocumentIdAsync(_fixture.ReviewerId);
        afterExpiry.Should().BeNull();
    }

    [Fact]
    public async Task Scenario32d_ReleaseExpiredReviewLocks_FreesAbandonedDocument()
    {
        await _fixture.InitializeAsync();

        await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Period: new DateOnly(2026, 5, 1), Description: "Boletín",
            Blob: new CreateBlobPathInputModel("a.pdf", "application/pdf")));

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        locked.Should().NotBeNull();

        // The reviewer closes the browser and never returns; the lock ages past the timeout.
        await _fixture.ExpireReviewLockAsync(locked!.DocumentId, TimeSpan.FromMinutes(41));

        await _fixture.DocumentRepository.ReleaseExpiredReviewLocksAsync();

        var queueItem = await _fixture.GetQueueItemAsync(locked.DocumentId);
        queueItem.ReviewLockedById.Should().BeNull();
        queueItem.ReviewLockedAt.Should().BeNull();

        var doc = await _fixture.GetDocumentAsync(locked.DocumentId);
        doc.Status.Should().Be(DocumentStatus.Pending);
    }

    [Fact]
    public async Task Scenario33_TakeNextForDigitalImprovement_ReTakesDocumentAfterLockExpires()
    {
        await _fixture.InitializeAsync();

        // Image document requiring digital improvement.
        await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            FileKind.Blob, Blob: new CreateBlobPathInputModel("a.jpg", "image/jpeg")));

        var first = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ReviewerId, "Reviewer", fonbecAuthClaim: null);
        first.Should().NotBeNull();

        // Still validly locked: another taker gets nothing.
        var blockedWhileLocked = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ManagerId, "Manager", fonbecAuthClaim: null);
        blockedWhileLocked.Should().BeNull();

        // Lock goes stale after the timeout.
        await _fixture.ExpireImprovementLockAsync(first!.DocumentId, TimeSpan.FromMinutes(41));

        var reTaken = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ManagerId, "Manager", fonbecAuthClaim: null);
        reTaken.Should().NotBeNull();
        reTaken!.DocumentId.Should().Be(first.DocumentId);

        var doc = await _fixture.GetDocumentAsync(reTaken.DocumentId);
        doc.ImprovementLockedById.Should().Be(_fixture.ManagerId);
        doc.Status.Should().Be(DocumentStatus.ProcessingImprovement);
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

    [Fact]
    public async Task FairDequeue_RotatesChapters_EvenWhenOneChapterUploadedFirst()
    {
        await _fixture.InitializeAsync();
        await _fixture.AddChapterWithStudentAsync(chapterId: 2, studentId: 20);
        await _fixture.AddChapterWithStudentAsync(chapterId: 3, studentId: 30);

        var t0 = DateTime.UtcNow.AddHours(-3);
        var chapter1First = await _fixture.EnqueuePendingOtherDocumentAsync(_fixture.ChapterId, _fixture.StudentId, t0);
        var chapter1Second = await _fixture.EnqueuePendingOtherDocumentAsync(
            _fixture.ChapterId, _fixture.StudentId, t0.AddMinutes(1));
        var chapter2 = await _fixture.EnqueuePendingOtherDocumentAsync(2, 20, t0.AddHours(1));
        var chapter3 = await _fixture.EnqueuePendingOtherDocumentAsync(3, 30, t0.AddHours(2));

        var first = await TakeAndCompleteAsync(_fixture.ReviewerId);
        first.Should().Be(chapter1First);
        (await _fixture.GetLastServedChapterIdAsync()).Should().Be(_fixture.ChapterId);

        var second = await TakeAndCompleteAsync(_fixture.ReviewerId);
        second.Should().Be(chapter2);
        (await _fixture.GetLastServedChapterIdAsync()).Should().Be(2);

        var third = await TakeAndCompleteAsync(_fixture.ReviewerId);
        third.Should().Be(chapter3);
        (await _fixture.GetLastServedChapterIdAsync()).Should().Be(3);

        var fourth = await TakeAndCompleteAsync(_fixture.ReviewerId);
        fourth.Should().Be(chapter1Second);
        (await _fixture.GetLastServedChapterIdAsync()).Should().Be(_fixture.ChapterId);
    }

    [Fact]
    public async Task FairDequeue_HighestPriorityWins_ThenRotatesWithinThatTier()
    {
        await _fixture.InitializeAsync();
        await _fixture.AddChapterWithStudentAsync(chapterId: 2, studentId: 20);

        var t0 = DateTime.UtcNow.AddHours(-2);
        await _fixture.EnqueuePendingOtherDocumentAsync(_fixture.ChapterId, _fixture.StudentId, t0, priority: 0);
        var urgentChapter2 = await _fixture.EnqueuePendingOtherDocumentAsync(2, 20, t0.AddHours(1), priority: -1);

        var first = await TakeAndReleaseAsync(_fixture.ReviewerId);
        first.Should().Be(urgentChapter2);
        (await _fixture.GetLastServedChapterIdAsync()).Should().Be(2);
    }

    [Fact]
    public async Task FairDequeue_SkipsChapterWhenItsDocumentsAreAllLocked()
    {
        await _fixture.InitializeAsync();
        await _fixture.AddChapterWithStudentAsync(chapterId: 2, studentId: 20);
        await _fixture.AddChapterWithStudentAsync(chapterId: 3, studentId: 30);

        var t0 = DateTime.UtcNow.AddHours(-3);
        await _fixture.EnqueuePendingOtherDocumentAsync(_fixture.ChapterId, _fixture.StudentId, t0);
        var chapter2 = await _fixture.EnqueuePendingOtherDocumentAsync(2, 20, t0.AddMinutes(1));
        var chapter3 = await _fixture.EnqueuePendingOtherDocumentAsync(3, 30, t0.AddMinutes(2));

        var lockedChapter1 = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ManagerId, "Manager");
        lockedChapter1.Should().NotBeNull();
        var lockedDocument = await _fixture.GetDocumentAsync(lockedChapter1!.DocumentId);
        lockedDocument.ChapterId.Should().Be(_fixture.ChapterId);

        var next = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        next.Should().NotBeNull();
        next!.DocumentId.Should().Be(chapter2);

        await _fixture.DocumentService.ReleaseReviewLockAsync(next.DocumentId, _fixture.ReviewerId);
        var afterRelease = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        afterRelease.Should().NotBeNull();
        afterRelease!.DocumentId.Should().Be(chapter3);
    }

    [Fact]
    public async Task FairDequeue_ResumeLock_DoesNotAdvanceCursor()
    {
        await _fixture.InitializeAsync();
        await _fixture.AddChapterWithStudentAsync(chapterId: 2, studentId: 20);

        var t0 = DateTime.UtcNow.AddHours(-1);
        var chapter1 = await _fixture.EnqueuePendingOtherDocumentAsync(_fixture.ChapterId, _fixture.StudentId, t0);
        await _fixture.EnqueuePendingOtherDocumentAsync(2, 20, t0.AddMinutes(1));

        var first = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        first.Should().NotBeNull();
        first!.DocumentId.Should().Be(chapter1);
        var cursorAfterTake = await _fixture.GetLastServedChapterIdAsync();

        var resumed = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        resumed.Should().NotBeNull();
        resumed!.DocumentId.Should().Be(chapter1);
        (await _fixture.GetLastServedChapterIdAsync()).Should().Be(cursorAfterTake);
    }

    [Fact]
    public async Task FairDequeue_ConcurrentReviewers_ReceiveDistinctDocuments()
    {
        await _fixture.InitializeAsync();
        await _fixture.AddChapterWithStudentAsync(chapterId: 2, studentId: 20);
        await _fixture.AddChapterWithStudentAsync(chapterId: 3, studentId: 30);

        var t0 = DateTime.UtcNow.AddHours(-1);
        var ids = new[]
        {
            await _fixture.EnqueuePendingOtherDocumentAsync(_fixture.ChapterId, _fixture.StudentId, t0),
            await _fixture.EnqueuePendingOtherDocumentAsync(2, 20, t0.AddMinutes(1)),
            await _fixture.EnqueuePendingOtherDocumentAsync(3, 30, t0.AddMinutes(2)),
        };

        var reviewerTask = _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        var managerTask = _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ManagerId, "Manager");
        await Task.WhenAll(reviewerTask, managerTask);

        var reviewerResult = await reviewerTask;
        var managerResult = await managerTask;

        reviewerResult.Should().NotBeNull();
        managerResult.Should().NotBeNull();
        reviewerResult!.DocumentId.Should().NotBe(managerResult!.DocumentId);
        ids.Should().Contain(reviewerResult.DocumentId);
        ids.Should().Contain(managerResult.DocumentId);
    }

    private async Task<long> TakeAndCompleteAsync(int reviewerId)
    {
        var taken = await _fixture.DocumentService.TakeNextForReviewAsync(reviewerId, "Reviewer");
        taken.Should().NotBeNull();
        var result = await _fixture.DocumentService.RejectOtherDocumentAsync(new RejectOtherDocumentInputModel(
            taken!.DocumentId,
            reviewerId,
            "Reviewer",
            taken.RowVersion,
            RejectedReasonIds.Unreadable,
            RejectionNotes: null));
        result.IsSuccess.Should().BeTrue();
        return taken.DocumentId;
    }

    private async Task<long> TakeAndReleaseAsync(int reviewerId)
    {
        var taken = await _fixture.DocumentService.TakeNextForReviewAsync(reviewerId, "Reviewer");
        taken.Should().NotBeNull();
        await _fixture.DocumentService.ReleaseReviewLockAsync(taken!.DocumentId, reviewerId);
        return taken.DocumentId;
    }
}