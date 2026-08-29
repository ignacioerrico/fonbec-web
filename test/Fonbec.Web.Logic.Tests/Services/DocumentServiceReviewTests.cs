using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Options;
using Fonbec.Web.Logic.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class DocumentServiceReviewTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly IDocumentNotificationService _notificationService = Substitute.For<IDocumentNotificationService>();
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IBlobStorageService _blobStorageService = Substitute.For<IBlobStorageService>();
    private readonly ILetterPlanProgressService _letterPlanProgressService = Substitute.For<ILetterPlanProgressService>();
    private readonly IPlanCompletionService _planCompletionService = Substitute.For<IPlanCompletionService>();

    private const int ReviewerId = 20;
    private const long DocumentId = 55;

    private DocumentService CreateService() =>
        new(_repository,
            _notificationService,
            _userService,
            _blobStorageService,
            _letterPlanProgressService,
            _planCompletionService,
            Microsoft.Extensions.Options.Options.Create(new BlobStorageOptions()),
            NullLogger<DocumentService>.Instance);

    private static ReviewWorkspaceDataModel Workspace(int? lockedById, DateTime? expiresAtUtc) =>
        new()
        {
            DocumentId = DocumentId,
            DocumentType = DocumentType.Letter,
            FileKind = FileKind.Text,
            TextContent = "Hola",
            PageCount = 0,
            ReviewLockedById = lockedById,
            LockExpiresAtUtc = expiresAtUtc,
            RowVersion = [1, 2, 3],
        };

    [Fact]
    public async Task GetReviewWorkspace_LockedByUserAndNotExpired_ReturnsWorkspace()
    {
        _repository.GetReviewWorkspaceAsync(DocumentId)
            .Returns(Workspace(ReviewerId, DateTime.UtcNow.AddMinutes(30)));

        var service = CreateService();

        var result = await service.GetReviewWorkspaceAsync(DocumentId, ReviewerId, FonbecRole.Reviewer);

        result.Should().NotBeNull();
        result!.DocumentId.Should().Be(DocumentId);
        result.DocumentType.Should().Be(DocumentType.Letter);
    }

    [Fact]
    public async Task GetReviewWorkspace_LockedByAnotherUser_ReturnsNull()
    {
        _repository.GetReviewWorkspaceAsync(DocumentId)
            .Returns(Workspace(ReviewerId + 1, DateTime.UtcNow.AddMinutes(30)));

        var service = CreateService();

        var result = await service.GetReviewWorkspaceAsync(DocumentId, ReviewerId, FonbecRole.Reviewer);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetReviewWorkspace_LockExpired_ReturnsNull()
    {
        _repository.GetReviewWorkspaceAsync(DocumentId)
            .Returns(Workspace(ReviewerId, DateTime.UtcNow.AddMinutes(-1)));

        var service = CreateService();

        var result = await service.GetReviewWorkspaceAsync(DocumentId, ReviewerId, FonbecRole.Reviewer);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetReviewWorkspace_DocumentNotFound_ReturnsNull()
    {
        _repository.GetReviewWorkspaceAsync(DocumentId).Returns((ReviewWorkspaceDataModel?)null);

        var service = CreateService();

        var result = await service.GetReviewWorkspaceAsync(DocumentId, ReviewerId, FonbecRole.Reviewer);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetReviewWorkspace_NonReviewerRole_ReturnsNullWithoutQueryingRepository()
    {
        var service = CreateService();

        var result = await service.GetReviewWorkspaceAsync(DocumentId, ReviewerId, FonbecRole.Uploader);

        result.Should().BeNull();
        await _repository.DidNotReceive().GetReviewWorkspaceAsync(Arg.Any<long>());
    }

    [Fact]
    public async Task GetActiveReviewLock_Reviewer_ReturnsRepositoryDocumentId()
    {
        _repository.GetActiveReviewLockedDocumentIdAsync(ReviewerId).Returns(DocumentId);

        var service = CreateService();

        var result = await service.GetActiveReviewLockAsync(ReviewerId, FonbecRole.Reviewer);

        result.Should().Be(DocumentId);
    }

    [Fact]
    public async Task GetActiveReviewLock_NonReviewer_ReturnsNullWithoutQueryingRepository()
    {
        var service = CreateService();

        var result = await service.GetActiveReviewLockAsync(ReviewerId, FonbecRole.Uploader);

        result.Should().BeNull();
        await _repository.DidNotReceive().GetActiveReviewLockedDocumentIdAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task TakeNextForReview_EmptyQueue_ReturnsNull()
    {
        _repository.TakeNextForReviewAsync(ReviewerId).Returns((DocumentQueueItemDataModel?)null);

        var service = CreateService();

        var result = await service.TakeNextForReviewAsync(ReviewerId, FonbecRole.Reviewer);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TakeNextForReview_NonReviewer_ReturnsNullWithoutQueryingRepository()
    {
        var service = CreateService();

        var result = await service.TakeNextForReviewAsync(ReviewerId, FonbecRole.Admin);

        result.Should().BeNull();
        await _repository.DidNotReceive().TakeNextForReviewAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetGlobalReviewProgress_MapsRepositoryCounts()
    {
        _repository.GetGlobalReviewProgressAsync(null).Returns(new ReviewProgressDataModel
        {
            PendingLetters = 4,
            PendingReportCards = 3,
            PendingOther = 2,
            PendingImprovement = 1,
            Processing = 5,
        });

        var service = CreateService();

        var result = await service.GetGlobalReviewProgressAsync(ReviewerId, FonbecRole.Manager, null);

        result.PendingLetters.Should().Be(4);
        result.PendingReportCards.Should().Be(3);
        result.PendingOther.Should().Be(2);
        result.PendingImprovement.Should().Be(1);
        result.Processing.Should().Be(5);
    }

    [Fact]
    public async Task GetGlobalReviewProgress_NonReviewer_Throws()
    {
        var service = CreateService();

        var act = () => service.GetGlobalReviewProgressAsync(ReviewerId, FonbecRole.Uploader, null);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static readonly byte[] RowVersion = [1, 2, 3];

    private static OtherDocument OtherDoc() =>
        new() { DocumentId = DocumentId, DocumentType = DocumentType.Other };

    private static RejectedReasonDataModel UnreadableReason() =>
        new() { RejectedReasonId = 9, Code = "Unreadable", Description = "No legible", RequiresNotes = false };

    private static RejectedReasonDataModel OtherReason() =>
        new() { RejectedReasonId = 11, Code = "Other", Description = "Otro", RequiresNotes = true };

    [Fact]
    public async Task ApproveOtherDocument_Reviewer_NotifiesSponsorsAndReturnsSuccess()
    {
        _repository.GetDocumentByIdAsync(DocumentId).Returns(OtherDoc());
        _repository.ApproveOtherDocumentAsync(Arg.Any<DataAccess.DataModels.Documents.Input.ApproveOtherDocumentInputDataModel>())
            .Returns([]);

        var service = CreateService();

        var result = await service.ApproveOtherDocumentAsync(
            new ApproveOtherDocumentInputModel(DocumentId, ReviewerId, FonbecRole.Reviewer, RowVersion));

        result.IsSuccess.Should().BeTrue();
        await _notificationService.Received(1).NotifySponsorsAsync(DocumentId);
    }

    [Fact]
    public async Task RejectOtherDocument_NoReasonId_ReturnsRequiredErrorWithoutQuerying()
    {
        var service = CreateService();

        var result = await service.RejectOtherDocumentAsync(
            new RejectOtherDocumentInputModel(DocumentId, ReviewerId, FonbecRole.Reviewer, RowVersion, null, null));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.RejectionReasonRequired);
        await _repository.DidNotReceive().GetDocumentByIdAsync(Arg.Any<long>());
        await _repository.DidNotReceive().GetApplicableRejectedReasonsAsync(Arg.Any<DocumentType>());
    }

    [Fact]
    public async Task RejectOtherDocument_DocumentIsNotOther_ReturnsErrorWithoutRejecting()
    {
        _repository.GetDocumentByIdAsync(DocumentId)
            .Returns(new Letter { DocumentId = DocumentId, DocumentType = DocumentType.Letter });

        var service = CreateService();

        var result = await service.RejectOtherDocumentAsync(new RejectOtherDocumentInputModel(
            DocumentId, ReviewerId, FonbecRole.Reviewer, RowVersion, 9, null));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.DocumentIsNotOther);
        await _repository.DidNotReceive().GetApplicableRejectedReasonsAsync(Arg.Any<DocumentType>());
        await _repository.DidNotReceive().RejectOtherDocumentAsync(Arg.Any<DataAccess.DataModels.Documents.Input.RejectOtherDocumentInputDataModel>());
    }

    [Fact]
    public async Task RejectOtherDocument_ReasonNotApplicable_ReturnsErrorWithoutRejecting()
    {
        _repository.GetDocumentByIdAsync(DocumentId).Returns(OtherDoc());
        _repository.GetApplicableRejectedReasonsAsync(DocumentType.Other).Returns([UnreadableReason(), OtherReason()]);

        var service = CreateService();

        var result = await service.RejectOtherDocumentAsync(new RejectOtherDocumentInputModel(
            DocumentId, ReviewerId, FonbecRole.Reviewer, RowVersion, RejectedReasonId: 1, null));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.RejectionReasonNotApplicable);
        await _repository.DidNotReceive().RejectOtherDocumentAsync(Arg.Any<DataAccess.DataModels.Documents.Input.RejectOtherDocumentInputDataModel>());
    }

    [Fact]
    public async Task RejectOtherDocument_ReasonRequiresNotesButNoneProvided_ReturnsNotesRequiredError()
    {
        _repository.GetDocumentByIdAsync(DocumentId).Returns(OtherDoc());
        _repository.GetApplicableRejectedReasonsAsync(DocumentType.Other).Returns([OtherReason()]);

        var service = CreateService();

        var result = await service.RejectOtherDocumentAsync(new RejectOtherDocumentInputModel(
            DocumentId, ReviewerId, FonbecRole.Reviewer, RowVersion, 11, "   "));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.RejectionNotesRequiredForOtherReason);
        await _repository.DidNotReceive().RejectOtherDocumentAsync(Arg.Any<DataAccess.DataModels.Documents.Input.RejectOtherDocumentInputDataModel>());
    }

    [Fact]
    public async Task RejectOtherDocument_NotesExceedMaxLength_ReturnsTooLongErrorWithoutRejecting()
    {
        _repository.GetDocumentByIdAsync(DocumentId).Returns(OtherDoc());
        _repository.GetApplicableRejectedReasonsAsync(DocumentType.Other).Returns([UnreadableReason()]);

        var service = CreateService();
        var tooLong = new string('x', MaxLength.Document.RejectionNotes + 1);

        var result = await service.RejectOtherDocumentAsync(new RejectOtherDocumentInputModel(
            DocumentId, ReviewerId, FonbecRole.Reviewer, RowVersion, 9, tooLong));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.RejectionNotesTooLong);
        await _repository.DidNotReceive().RejectOtherDocumentAsync(Arg.Any<DataAccess.DataModels.Documents.Input.RejectOtherDocumentInputDataModel>());
    }

    [Fact]
    public async Task RejectOtherDocument_ValidReason_CallsRepositoryAndDoesNotNotifySponsors()
    {
        _repository.GetDocumentByIdAsync(DocumentId).Returns(OtherDoc());
        _repository.GetApplicableRejectedReasonsAsync(DocumentType.Other).Returns([UnreadableReason(), OtherReason()]);
        _repository.RejectOtherDocumentAsync(Arg.Any<DataAccess.DataModels.Documents.Input.RejectOtherDocumentInputDataModel>())
            .Returns([]);

        var service = CreateService();

        var result = await service.RejectOtherDocumentAsync(new RejectOtherDocumentInputModel(
            DocumentId, ReviewerId, FonbecRole.Reviewer, RowVersion, 9, null));

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).RejectOtherDocumentAsync(
            Arg.Is<DataAccess.DataModels.Documents.Input.RejectOtherDocumentInputDataModel>(m =>
                m.DocumentId == DocumentId && m.ReviewerId == ReviewerId && m.RejectedReasonId == 9));
        await _notificationService.DidNotReceive().NotifySponsorsAsync(Arg.Any<long>());
    }

    [Fact]
    public async Task GetApplicableRejectedReasons_MapsRepositoryResults()
    {
        _repository.GetApplicableRejectedReasonsAsync(DocumentType.Other).Returns([UnreadableReason(), OtherReason()]);

        var service = CreateService();

        var result = await service.GetApplicableRejectedReasonsAsync(DocumentType.Other);

        result.Should().BeEquivalentTo([
            new RejectedReasonViewModel
            {
                RejectedReasonId = 9,
                Code = "Unreadable",
                Description = "No legible",
                RequiresNotes = false,
            },
            new RejectedReasonViewModel
            {
                RejectedReasonId = 11,
                Code = "Other",
                Description = "Otro",
                RequiresNotes = true,
            },
        ]);
    }
}
