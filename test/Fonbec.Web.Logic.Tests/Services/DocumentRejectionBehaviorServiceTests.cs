using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Models.PlannedDeliveries;
using Fonbec.Web.Logic.Options;
using Fonbec.Web.Logic.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class DocumentRejectionBehaviorServiceTests
{
    private const int ReviewerId = 20;
    private const int ManagerId = 30;
    private const int ChapterId = 4;
    private const long DocumentId = 55;
    private const int PlanId = 10;
    private static readonly byte[] RowVersion = [1, 2, 3];
    private static readonly DateTime PlanStartsOn = new(2026, 9, 1);

    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly IDocumentNotificationQueue _notificationQueue = Substitute.For<IDocumentNotificationQueue>();
    private readonly IPlanCompletionService _planCompletionService = Substitute.For<IPlanCompletionService>();

    private DocumentService CreateService() =>
        new(_repository,
            _notificationQueue,
            Substitute.For<IUserService>(),
            Substitute.For<IBlobStorageService>(),
            _planCompletionService,
            Microsoft.Extensions.Options.Options.Create(new BlobStorageOptions()),
            Substitute.For<IRecipientMessageNotificationService>(),
            NullLogger<DocumentService>.Instance);

    [Theory]
    [InlineData(DocumentType.Letter)]
    [InlineData(DocumentType.ReportCard)]
    [InlineData(DocumentType.Other)]
    public async Task Reject_Emails_The_Facilitator_When_The_Document_Is_Returned(DocumentType documentType)
    {
        var document = Document(documentType, DocumentStatus.Rejected);
        _repository.GetDocumentByIdAsync(DocumentId).Returns(document);
        StubApplicableReason(documentType);
        StubReject(documentType);

        var result = await RejectAsync(documentType);

        result.IsSuccess.Should().BeTrue();
        await _notificationQueue.Received(1).EnqueueFacilitatorRejectionNotificationAsync(DocumentId);
        await _notificationQueue.DidNotReceive().EnqueueHeldRejectionManagerNotificationAsync(Arg.Any<long>());
        await _notificationQueue.DidNotReceive().EnqueueSponsorNotificationAsync(Arg.Any<long>());
    }

    [Theory]
    [InlineData(DocumentType.Letter)]
    [InlineData(DocumentType.ReportCard)]
    [InlineData(DocumentType.Other)]
    public async Task Reject_Emails_The_Managers_While_They_Still_Have_To_Decide(DocumentType documentType)
    {
        var document = Document(documentType, DocumentStatus.PendingManagerDecision);
        _repository.GetDocumentByIdAsync(DocumentId).Returns(document);
        StubApplicableReason(documentType);
        StubReject(documentType);

        var result = await RejectAsync(documentType);

        result.IsSuccess.Should().BeTrue();
        await _notificationQueue.Received(1).EnqueueHeldRejectionManagerNotificationAsync(DocumentId);
        await _notificationQueue.DidNotReceive().EnqueueFacilitatorRejectionNotificationAsync(Arg.Any<long>());
    }

    [Fact]
    public async Task ShareHeldRejection_Approves_A_Letter_And_Notifies_The_Sponsor_And_Managers()
    {
        var letter = (Letter)Document(DocumentType.Letter, DocumentStatus.PendingManagerDecision);
        _repository.GetDocumentByIdAsync(DocumentId).Returns(letter);
        _repository.ShareHeldRejectionAsync(DocumentId, ManagerId, ChapterId, RowVersion).Returns([]);
        _planCompletionService.GetReadinessAsync(PlanId, ChapterId, Arg.Any<CancellationToken>())
            .Returns(
                new PlanReadinessResult { IsCompleted = false, IsReadyToComplete = false },
                new PlanReadinessResult
                {
                    IsCompleted = false,
                    IsReadyToComplete = true,
                    PlanStartsOn = PlanStartsOn,
                });

        var result = await CreateService().ShareHeldRejectionAsync(Decision(FonbecRole.Manager));

        result.IsSuccess.Should().BeTrue();
        await _notificationQueue.Received(1).EnqueueSponsorNotificationAsync(DocumentId);
        await _notificationQueue.Received(1).EnqueuePlanReadyNotificationAsync(ChapterId, PlanId, PlanStartsOn);
        await _notificationQueue.DidNotReceive().EnqueueFacilitatorRejectionNotificationAsync(Arg.Any<long>());
        await _notificationQueue.DidNotReceive().EnqueueHeldRejectionManagerNotificationAsync(Arg.Any<long>());
    }

    [Fact]
    public async Task ShareHeldRejection_Reviewer_Is_Not_Authorized()
    {
        var result = await CreateService().ShareHeldRejectionAsync(Decision(FonbecRole.Reviewer));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.NotAuthorizedToDecideRejection);
        await _repository.DidNotReceive().ShareHeldRejectionAsync(
            Arg.Any<long>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<byte[]>());
    }

    [Fact]
    public async Task ReturnHeldRejection_Emails_The_Facilitator()
    {
        _repository.ReturnHeldRejectionAsync(DocumentId, ChapterId, RowVersion).Returns([]);

        var result = await CreateService().ReturnHeldRejectionAsync(Decision(FonbecRole.Manager));

        result.IsSuccess.Should().BeTrue();
        await _notificationQueue.Received(1).EnqueueFacilitatorRejectionNotificationAsync(DocumentId);
        await _notificationQueue.DidNotReceive().EnqueueSponsorNotificationAsync(Arg.Any<long>());
    }

    [Fact]
    public async Task ReturnHeldRejection_Reviewer_Is_Not_Authorized()
    {
        var result = await CreateService().ReturnHeldRejectionAsync(Decision(FonbecRole.Reviewer));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.NotAuthorizedToDecideRejection);
        await _repository.DidNotReceive().ReturnHeldRejectionAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<byte[]>());
    }

    private async Task<Fonbec.Web.Logic.Models.Results.ReviewResult> RejectAsync(DocumentType documentType)
    {
        var service = CreateService();
        return documentType switch
        {
            DocumentType.Letter => await service.RejectLetterAsync(new RejectLetterInputModel(
                DocumentId, ReviewerId, FonbecRole.Reviewer, RowVersion, RejectedReasonIds.Unreadable, null)),
            DocumentType.ReportCard => await service.RejectReportCardAsync(new RejectReportCardInputModel(
                DocumentId, ReviewerId, FonbecRole.Reviewer, RowVersion, RejectedReasonIds.Unreadable, null)),
            _ => await service.RejectOtherDocumentAsync(new RejectOtherDocumentInputModel(
                DocumentId, ReviewerId, FonbecRole.Reviewer, RowVersion, RejectedReasonIds.Unreadable, null)),
        };
    }

    private void StubReject(DocumentType documentType)
    {
        switch (documentType)
        {
            case DocumentType.Letter:
                _repository.RejectLetterAsync(Arg.Any<DataAccess.DataModels.Documents.Input.RejectLetterInputDataModel>())
                    .Returns([]);
                break;
            case DocumentType.ReportCard:
                _repository.RejectReportCardAsync(Arg.Any<DataAccess.DataModels.Documents.Input.RejectReportCardInputDataModel>())
                    .Returns([]);
                break;
            default:
                _repository.RejectOtherDocumentAsync(Arg.Any<DataAccess.DataModels.Documents.Input.RejectOtherDocumentInputDataModel>())
                    .Returns([]);
                break;
        }
    }

    private void StubApplicableReason(DocumentType documentType)
    {
        if (documentType is DocumentType.Letter)
        {
            return;
        }

        _repository.GetApplicableRejectedReasonsAsync(documentType).Returns([
            new DataAccess.DataModels.Documents.RejectedReasonDataModel
            {
                RejectedReasonId = RejectedReasonIds.Unreadable,
                Code = "Unreadable",
                Description = "Ilegible",
            },
        ]);
    }

    private static Document Document(DocumentType documentType, DocumentStatus status)
    {
        Document document = documentType switch
        {
            DocumentType.Letter => new Letter { PlanId = PlanId },
            DocumentType.ReportCard => new ReportCard
            {
                Student = new Student(),
            },
            _ => new OtherDocument(),
        };

        document.DocumentId = DocumentId;
        document.DocumentType = documentType;
        document.ChapterId = ChapterId;
        document.Status = status;
        return document;
    }

    private static HeldRejectionDecisionInputModel Decision(string role) =>
        new(DocumentId, ManagerId, role, ChapterId, RowVersion);
}