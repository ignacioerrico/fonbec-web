using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
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

    private const int ReviewerId = 20;
    private const long DocumentId = 55;

    private DocumentService CreateService() =>
        new(_repository,
            _notificationService,
            _userService,
            _blobStorageService,
            _letterPlanProgressService,
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
}