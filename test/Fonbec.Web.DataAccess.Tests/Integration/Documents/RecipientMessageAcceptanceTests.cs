using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Documents.Input;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Tests.Integration.Documents;

public class RecipientMessageAcceptanceTests
{
    private readonly DocumentTestFixture _fixture = new();

    [Fact]
    public async Task Person_Send_AppearsPending_AndClearsNothingOnTheDocument()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.SendRecipientMessageAsync(
            _fixture.SponsorAToken, _fixture.StudentId, isCompany: false, "  Hola, ¿cómo estás?  ");

        result.IsAuthorized.Should().BeTrue();
        result.IsSaved.Should().BeTrue();
        result.Message!.Body.Should().Be("Hola, ¿cómo estás?");
        result.Message.SharedOn.Should().BeNull();
        result.Message.StatusLabel.Should().Be("Pendiente");
        result.Message.Letter.Should().BeNull();

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var row = await db.Set<RecipientMessage>().SingleAsync(TestContext.Current.CancellationToken);
        row.SponsorId.Should().Be(_fixture.SponsorAId);
        row.CompanyId.Should().BeNull();
        row.StudentId.Should().Be(_fixture.StudentId);
        row.SharedOn.Should().BeNull();
        row.SharedById.Should().BeNull();
    }

    [Fact]
    public async Task Company_Send_IsNotVisibleOnThePersonHistory()
    {
        await _fixture.InitializeAsync();

        var saved = await _fixture.DocumentService.SendRecipientMessageAsync(
            _fixture.CompanyToken, _fixture.CompanyStudentId, isCompany: true, "De la empresa");
        saved.IsSaved.Should().BeTrue();

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var row = await db.Set<RecipientMessage>().SingleAsync(TestContext.Current.CancellationToken);
        row.CompanyId.Should().Be(_fixture.CompanyId);
        row.SponsorId.Should().BeNull();

        var personThread = await _fixture.DocumentService.GetRecipientThreadAsync(
            _fixture.CompanyLinkedSponsorToken, _fixture.CompanyStudentId, isCompany: false, 0, 20);
        personThread.IsAuthorized.Should().BeTrue();
        personThread.Items.Should().NotContain(i => i.Body == "De la empresa");

        var companyThread = await _fixture.DocumentService.GetRecipientThreadAsync(
            _fixture.CompanyToken, _fixture.CompanyStudentId, isCompany: true, 0, 20);
        companyThread.Items.Should().ContainSingle(i => i.Body == "De la empresa" && i.StatusLabel == "Pendiente");
    }

    [Fact]
    public async Task Thread_IsOwnLettersAndMessages_OldestFirst_ReportCardStaysInOtherDocuments()
    {
        await _fixture.InitializeAsync();
        await CreateAndApproveLetterAsync(_fixture.StudentId, _fixture.SponsorAId, companyId: null);

        var report = await _fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
            _fixture.StudentId, _fixture.UploaderContext,
            FileKind.Blob, Period: new DateOnly(2026, 3, 1), Description: "Boletín 1º trimestre",
            Blob: new CreateBlobPathInputModel("r.pdf", "application/pdf")));
        report.IsSuccess.Should().BeTrue();
        var lockedReport = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        lockedReport.Should().NotBeNull();
        await _fixture.DocumentService.ApproveReportCardAsync(new ApproveReportCardInputModel(
            lockedReport!.DocumentId, _fixture.ReviewerId, "Reviewer", lockedReport.RowVersion,
            ConfirmedIsReportCardOrTranscript: true, ConfirmedPeriodMatches: true,
            ConfirmedStudentNameCorrect: true, ReportCardAssessment.Green, Absences: 0));

        await _fixture.DocumentService.SendRecipientMessageAsync(
            _fixture.SponsorAToken, _fixture.StudentId, false, "Gracias por la carta.");

        await using (var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Set<RecipientMessage>().Add(new RecipientMessage
            {
                StudentId = _fixture.StudentId,
                SponsorId = _fixture.SponsorBId,
                Body = "Mensaje de otro padrino",
                SentOn = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var history = await _fixture.DocumentService.GetSharedDocumentsAsync(_fixture.SponsorAToken, _fixture.StudentId);
        history.Documents.Should().ContainSingle(d => d.DocumentType == DocumentType.ReportCard);
        history.Documents.Should().NotContain(d => d.DocumentType == DocumentType.Letter);

        var thread = await _fixture.DocumentService.GetRecipientThreadAsync(
            _fixture.SponsorAToken, _fixture.StudentId, false, 0, 20);
        thread.Items.Should().HaveCount(2);
        thread.Items[0].Letter!.DocumentType.Should().Be(DocumentType.Letter);
        thread.Items[1].Body.Should().Be("Gracias por la carta.");
        thread.Items.Select(i => i.OccurredOnUtc).Should().BeInAscendingOrder();
        thread.Items.Should().NotContain(i => i.Body == "Mensaje de otro padrino");
        thread.Items.Should().NotContain(i => i.Letter != null && i.Letter.DocumentType == DocumentType.ReportCard);
    }

    [Fact]
    public async Task Send_BeforeAnyLetter_AndAgainWhilePending_StoresTwoUnlinkedMessages()
    {
        await _fixture.InitializeAsync();

        var first = await _fixture.DocumentService.SendRecipientMessageAsync(
            _fixture.SponsorAToken, _fixture.StudentId, false, "Primero");
        var second = await _fixture.DocumentService.SendRecipientMessageAsync(
            _fixture.SponsorAToken, _fixture.StudentId, false, "Segundo");

        first.IsSaved.Should().BeTrue();
        second.IsSaved.Should().BeTrue();
        first.Message!.StatusLabel.Should().Be("Pendiente");
        second.Message!.StatusLabel.Should().Be("Pendiente");
        first.Message.Letter.Should().BeNull();
        second.Message.Letter.Should().BeNull();

        var thread = await _fixture.DocumentService.GetRecipientThreadAsync(
            _fixture.SponsorAToken, _fixture.StudentId, false, 0, 20);
        thread.Items.Should().HaveCount(2);
        thread.Items.Should().OnlyContain(i => i.IsMessage && i.SharedOn == null && i.StatusLabel == "Pendiente");
    }

    [Fact]
    public async Task Reload_ShowsDeliveredTime_AndUndoReturnsToPending()
    {
        await _fixture.InitializeAsync();
        var saved = await _fixture.DocumentService.SendRecipientMessageAsync(
            _fixture.SponsorAToken, _fixture.StudentId, false, "Para el mediador");
        var sharedOn = new DateTime(2026, 4, 3, 12, 12, 0, DateTimeKind.Utc);

        await using (var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            var row = await db.Set<RecipientMessage>().SingleAsync(TestContext.Current.CancellationToken);
            row.SharedOn = sharedOn;
            row.SharedById = _fixture.ReviewerId;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var delivered = await _fixture.DocumentService.GetRecipientThreadAsync(
            _fixture.SponsorAToken, _fixture.StudentId, false, 0, 20);
        delivered.Items.Single().StatusLabel.Should().Be(
            $"Entregado {sharedOn.ToLocalTime().ToLocalizedDateTime()}");
        delivered.Items.Single().StatusLabel.Should().NotContain("Reviewer");

        await using (var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            var row = await db.Set<RecipientMessage>().SingleAsync(TestContext.Current.CancellationToken);
            row.SharedOn = null;
            row.SharedById = null;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var pending = await _fixture.DocumentService.GetRecipientThreadAsync(
            _fixture.SponsorAToken, _fixture.StudentId, false, 0, 20);
        pending.Items.Single().SharedOn.Should().BeNull();
        pending.Items.Single().StatusLabel.Should().Be("Pendiente");
        saved.Message!.RecipientMessageId.Should().Be(pending.Items.Single().RecipientMessageId);
    }

    [Fact]
    public async Task Send_EmptyOrTooLong_IsNotSaved()
    {
        await _fixture.InitializeAsync();

        var empty = await _fixture.DocumentService.SendRecipientMessageAsync(
            _fixture.SponsorAToken, _fixture.StudentId, false, "   ");
        empty.IsAuthorized.Should().BeTrue();
        empty.IsSaved.Should().BeFalse();

        var over = await _fixture.DocumentService.SendRecipientMessageAsync(
            _fixture.SponsorAToken, _fixture.StudentId, false, new string('a', MaxLength.RecipientMessage.Body + 1));
        over.IsAuthorized.Should().BeTrue();
        over.IsSaved.Should().BeFalse();

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<RecipientMessage>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task Send_InvalidToken_DoesNotInsert()
    {
        await _fixture.InitializeAsync();

        var result = await _fixture.DocumentService.SendRecipientMessageAsync(
            Guid.NewGuid(), _fixture.StudentId, false, "Hola");
        result.IsAuthorized.Should().BeFalse();
        result.IsSaved.Should().BeFalse();

        var wrongUrl = await _fixture.DocumentService.SendRecipientMessageAsync(
            _fixture.SponsorAToken, _fixture.StudentId, isCompany: true, "Hola");
        wrongUrl.IsAuthorized.Should().BeFalse();

        await using var db = await _fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.Set<RecipientMessage>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task LetterInTheThread_KeepsTheDocumentIdUsedByTheDownloadRoute()
    {
        await _fixture.InitializeAsync();
        var documentId = await CreateAndApproveLetterAsync(_fixture.StudentId, _fixture.SponsorAId, companyId: null);

        var thread = await _fixture.DocumentService.GetRecipientThreadAsync(
            _fixture.SponsorAToken, _fixture.StudentId, false, 0, 20);

        var letter = thread.Items.Should().ContainSingle().Subject.Letter;
        letter!.DocumentId.Should().Be(documentId);
        letter.DocumentType.Should().Be(DocumentType.Letter);
        letter.FileKind.Should().Be(FileKind.Text);
        letter.TextContent.Should().Be("Dear sponsor");
    }

    private async Task<long> CreateAndApproveLetterAsync(int studentId, int? sponsorId, int? companyId)
    {
        var create = await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            studentId, _fixture.PlanId, sponsorId, _fixture.UploaderContext,
            FileKind.Text, TextContent: "Dear sponsor", CompanyId: companyId));
        create.IsSuccess.Should().BeTrue();

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        await _fixture.DocumentService.ApproveLetterAsync(new ApproveLetterInputModel(
            locked!.DocumentId, _fixture.ReviewerId, "Reviewer", locked.RowVersion,
            ConfirmedIsLetter: true, ConfirmedWrittenDate: DateTime.UtcNow.Date,
            ConfirmedAddressee: true, ConfirmedSignerMatchesStudent: true,
            SpellingScore: 4, PenmanshipScore: 4, ContentScore: 4,
            HasRedFlags: false, HasGreenFlags: true, IssuesNotes: null, Appraisal: "Good"));
        return locked.DocumentId;
    }
}