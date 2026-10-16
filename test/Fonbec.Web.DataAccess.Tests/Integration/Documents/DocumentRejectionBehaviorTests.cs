using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Documents.Input;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Fonbec.Web.DataAccess.Tests.Integration.Documents;

public class DocumentRejectionBehaviorTests
{
    [Theory]
    [InlineData(DocumentType.Letter, "la carta", "Carta rechazada")]
    [InlineData(DocumentType.ReportCard, "el boletín", "Boletín rechazado")]
    [InlineData(DocumentType.Other, "el documento", "Documento rechazado")]
    public async Task ReturnMode_Rejects_The_Document_And_Emails_The_Facilitator(
        DocumentType documentType, string phrase, string subject)
    {
        var fixture = new DocumentTestFixture();
        await fixture.InitializeAsync();

        var documentId = await CreateAndRejectAsync(fixture, documentType);

        var document = await fixture.GetDocumentAsync(documentId);
        document.Status.Should().Be(DocumentStatus.Rejected);
        document.RejectedReasonId.Should().Be(RejectedReasonIds.Unreadable);
        document.FacilitatorRejectionNotifiedOn.Should().NotBeNull();
        await fixture.EmailSender.Received(1).SendEmailAsync(
            Arg.Is<Recipient>(recipient =>
                recipient.EmailAddress == "<uploader@fonbec.test>"
                && recipient.DisplayName == "\"uploader User\""),
            $"FONBEC | {subject}",
            Arg.Is<string>(html =>
                html.Contains($"Se rechazó {phrase} de Maria Garcia.")
                && html.Contains("Motivo: Ilegible")
                && html.Contains("/mediadores/mis-becarios")));
    }

    [Theory]
    [InlineData(DocumentType.Letter, "Carta rechazada", "la carta")]
    [InlineData(DocumentType.ReportCard, "Boletín rechazado", "el boletín")]
    [InlineData(DocumentType.Other, "Documento rechazado", "el documento")]
    public async Task HoldMode_Keeps_The_Rejection_Until_The_Manager_Decides(
        DocumentType documentType, string subject, string phrase)
    {
        var fixture = new DocumentTestFixture();
        await fixture.InitializeAsync();
        await fixture.DocumentRepository.SetChapterRejectionBehaviorAsync(
            fixture.ChapterId, DocumentRejectionBehavior.HoldForManager, fixture.ManagerId);

        var documentId = await CreateAndRejectAsync(fixture, documentType);

        var document = await fixture.GetDocumentAsync(documentId);
        document.Status.Should().Be(DocumentStatus.PendingManagerDecision);
        document.RejectedOn.Should().NotBeNull();
        document.RejectedReasonId.Should().Be(RejectedReasonIds.Unreadable);
        document.FacilitatorRejectionNotifiedOn.Should().BeNull();
        document.ManagersNotifiedOfHeldRejectionOn.Should().NotBeNull();
        await fixture.EmailSender.DidNotReceive().SendEmailAsync(
            Arg.Is<Recipient>(recipient => recipient.EmailAddress == "<uploader@fonbec.test>"),
            Arg.Any<string>(),
            Arg.Any<string>());
        await fixture.EmailSender.Received(1).SendEmailAsync(
            Arg.Is<Recipient>(recipient =>
                recipient.EmailAddress == "<manager@fonbec.test>"
                && recipient.DisplayName == "\"manager User\""),
            $"FONBEC | {subject} requiere la decisión de un coordinador",
            Arg.Is<string>(html =>
                html.Contains($"Se rechazó {phrase} de Maria Garcia.")
                && html.Contains("Se requiere tu decisión.")
                && html.Contains("/rechazados")));

        var stillHeld = await fixture.DocumentRepository.SetChapterRejectionBehaviorAsync(
            fixture.ChapterId, DocumentRejectionBehavior.ReturnToFacilitator, fixture.ManagerId);
        stillHeld.Should().BeTrue();
        (await fixture.GetDocumentAsync(documentId)).Status.Should().Be(DocumentStatus.PendingManagerDecision);
    }

    [Fact]
    public async Task HoldMode_Return_Shows_The_Rejection_And_Emails_The_Facilitator()
    {
        var fixture = new DocumentTestFixture();
        await fixture.InitializeAsync();
        await fixture.DocumentRepository.SetChapterRejectionBehaviorAsync(
            fixture.ChapterId, DocumentRejectionBehavior.HoldForManager, fixture.ManagerId);
        var documentId = await CreateAndRejectAsync(fixture, DocumentType.Other);
        var held = await fixture.GetDocumentAsync(documentId);

        var result = await fixture.DocumentService.ReturnHeldRejectionAsync(new HeldRejectionDecisionInputModel(
            documentId, fixture.ManagerId, "Manager", fixture.ChapterId, held.RowVersion));

        result.IsSuccess.Should().BeTrue();
        var returned = await fixture.GetDocumentAsync(documentId);
        returned.Status.Should().Be(DocumentStatus.Rejected);
        returned.RejectedReasonId.Should().Be(RejectedReasonIds.Unreadable);
        returned.FacilitatorRejectionNotifiedOn.Should().NotBeNull();
        await fixture.EmailSender.Received(1).SendEmailAsync(
            Arg.Is<Recipient>(recipient =>
                recipient.EmailAddress == "<uploader@fonbec.test>"
                && recipient.DisplayName == "\"uploader User\""),
            "FONBEC | Documento rechazado",
            Arg.Is<string>(html => html.Contains("Se rechazó el documento de Maria Garcia.")));
    }

    [Fact]
    public async Task HoldMode_Share_Approves_The_Document_And_Keeps_The_Rejection_For_Managers()
    {
        var fixture = new DocumentTestFixture();
        await fixture.InitializeAsync();
        await fixture.DocumentRepository.SetChapterRejectionBehaviorAsync(
            fixture.ChapterId, DocumentRejectionBehavior.HoldForManager, fixture.ManagerId);
        var documentId = await CreateAndRejectAsync(fixture, DocumentType.Letter);
        var held = await fixture.GetDocumentAsync(documentId);

        var result = await fixture.DocumentService.ShareHeldRejectionAsync(new HeldRejectionDecisionInputModel(
            documentId, fixture.ManagerId, "Manager", fixture.ChapterId, held.RowVersion));

        result.IsSuccess.Should().BeTrue();
        var shared = await fixture.GetDocumentAsync(documentId);
        shared.Status.Should().Be(DocumentStatus.Approved);
        shared.ApprovedOn.Should().NotBeNull();
        shared.RejectedOn.Should().NotBeNull();
        shared.RejectedReasonId.Should().Be(RejectedReasonIds.Unreadable);
        shared.RejectionOverriddenById.Should().Be(fixture.ManagerId);
        shared.RejectionOverriddenOn.Should().NotBeNull();
        shared.FacilitatorRejectionNotifiedOn.Should().BeNull();

        await using var db = await fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var shares = await db.Set<DocumentShare>().Where(s => s.DocumentId == documentId).ToListAsync(TestContext.Current.CancellationToken);
        shares.Should().ContainSingle(s => s.SponsorId == fixture.SponsorAId);

        await fixture.EmailSender.DidNotReceive().SendEmailAsync(
            Arg.Is<Recipient>(recipient => recipient.EmailAddress == "<uploader@fonbec.test>"),
            Arg.Any<string>(),
            Arg.Any<string>());
        await fixture.EmailSender.Received(1).SendEmailAsync(
            Arg.Is<IReadOnlyList<Recipient>>(to => to.Any(r => r.EmailAddress.Contains("sponsor.a@test.com"))),
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<IReadOnlyList<Recipient>>(),
            "FONBEC | Nueva carta disponible",
            Arg.Any<string>());
    }

    [Fact]
    public async Task HoldMode_Share_ReportCard_Reaches_Every_Active_Sponsor()
    {
        var fixture = new DocumentTestFixture();
        await fixture.InitializeAsync();
        await fixture.DocumentRepository.SetChapterRejectionBehaviorAsync(
            fixture.ChapterId, DocumentRejectionBehavior.HoldForManager, fixture.ManagerId);
        var documentId = await CreateAndRejectAsync(fixture, DocumentType.ReportCard);
        var held = await fixture.GetDocumentAsync(documentId);

        var result = await fixture.DocumentService.ShareHeldRejectionAsync(new HeldRejectionDecisionInputModel(
            documentId, fixture.ManagerId, "Manager", fixture.ChapterId, held.RowVersion));

        result.IsSuccess.Should().BeTrue();
        (await fixture.GetDocumentAsync(documentId)).Status.Should().Be(DocumentStatus.Approved);

        await using var db = await fixture.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var sponsorIds = await db.Set<DocumentShare>()
            .Where(s => s.DocumentId == documentId)
            .Select(s => s.SponsorId)
            .ToListAsync(TestContext.Current.CancellationToken);
        sponsorIds.Should().BeEquivalentTo([fixture.SponsorAId, fixture.SponsorBId]);
    }

    private static async Task<long> CreateAndRejectAsync(DocumentTestFixture fixture, DocumentType documentType)
    {
        var created = documentType switch
        {
            DocumentType.Letter => await fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
                fixture.StudentId, fixture.PlanId, fixture.SponsorAId, fixture.UploaderContext,
                FileKind.Text, TextContent: "Dear sponsor")),
            DocumentType.ReportCard => await fixture.DocumentService.CreateReportCardAsync(new CreateReportCardInputModel(
                fixture.StudentId, fixture.UploaderContext, FileKind.Blob,
                Period: new DateOnly(2026, 6, 1),
                Description: "Boletín",
                Blob: new CreateBlobPathInputModel("path/report.pdf", "application/pdf"))),
            _ => await fixture.DocumentService.CreateOtherDocumentAsync(new CreateOtherDocumentInputModel(
                fixture.StudentId, fixture.UploaderContext, FileKind.Blob,
                Description: "Certificado",
                Blob: new CreateBlobPathInputModel("path/note.txt", "text/plain"))),
        };
        created.IsSuccess.Should().BeTrue(string.Join(", ", created.Errors ?? []));
        var documentId = created.Value;

        var locked = await fixture.DocumentService.TakeNextForReviewAsync(fixture.ReviewerId, "Reviewer");
        locked.Should().NotBeNull();
        locked!.DocumentId.Should().Be(documentId);

        var rejected = documentType switch
        {
            DocumentType.Letter => await fixture.DocumentService.RejectLetterAsync(new RejectLetterInputModel(
                documentId, fixture.ReviewerId, "Reviewer", locked.RowVersion, RejectedReasonIds.Unreadable, null)),
            DocumentType.ReportCard => await fixture.DocumentService.RejectReportCardAsync(new RejectReportCardInputModel(
                documentId, fixture.ReviewerId, "Reviewer", locked.RowVersion, RejectedReasonIds.Unreadable, null)),
            _ => await fixture.DocumentService.RejectOtherDocumentAsync(new RejectOtherDocumentInputModel(
                documentId, fixture.ReviewerId, "Reviewer", locked.RowVersion, RejectedReasonIds.Unreadable, null)),
        };
        rejected.IsSuccess.Should().BeTrue(string.Join(", ", rejected.Errors ?? []));
        return documentId;
    }
}