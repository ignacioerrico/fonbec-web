using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.Options;
using Fonbec.Web.Logic.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class DocumentServiceImprovementTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly IDocumentNotificationQueue _notificationQueue = Substitute.For<IDocumentNotificationQueue>();
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IBlobStorageService _blobStorageService = Substitute.For<IBlobStorageService>();
    private readonly IPlanCompletionService _planCompletionService = Substitute.For<IPlanCompletionService>();

    private const int ReviewerId = 20;
    private const long DocumentId = 55;

    private DocumentService CreateService() =>
        new(_repository,
            _notificationQueue,
            _userService,
            _blobStorageService,
            _planCompletionService,
            Microsoft.Extensions.Options.Options.Create(new BlobStorageOptions()),
            Substitute.For<IRecipientMessageNotificationService>(),
            NullLogger<DocumentService>.Instance);

    private void GrantImprovement()
    {
        _userService.GetFonbecGrantsClaim(Arg.Any<int>()).Returns(DocumentPermission.DigitalImprovement);
        _userService.HasPermission(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(true);
    }

    private static ImprovementWorkspaceDataModel Workspace(int? lockedById, DateTime? expiresAtUtc) =>
        new()
        {
            DocumentId = DocumentId,
            DocumentType = DocumentType.Letter,
            FileKind = FileKind.Blob,
            PageCount = 1,
            Pages = [new ReviewWorkspacePageDataModel { PageNumber = 1, MimeType = "image/jpeg" }],
            ImprovementLockedById = lockedById,
            LockExpiresAtUtc = expiresAtUtc,
            RowVersion = [1, 2, 3],
        };

    [Fact]
    public async Task GetActiveImprovementLock_GrantedReviewer_ReturnsRepositoryDocumentId()
    {
        GrantImprovement();
        _repository.GetActiveImprovementLockedDocumentIdAsync(ReviewerId).Returns(DocumentId);

        var service = CreateService();

        var result = await service.GetActiveImprovementLockAsync(ReviewerId, FonbecRole.Reviewer);

        result.Should().Be(DocumentId);
    }

    [Fact]
    public async Task GetActiveImprovementLock_WithoutGrant_ReturnsNullWithoutQueryingRepository()
    {
        _userService.GetFonbecGrantsClaim(Arg.Any<int>()).Returns(string.Empty);
        _userService.HasPermission(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(false);

        var service = CreateService();

        var result = await service.GetActiveImprovementLockAsync(ReviewerId, FonbecRole.Reviewer);

        result.Should().BeNull();
        await _repository.DidNotReceive().GetActiveImprovementLockedDocumentIdAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetImprovementWorkspace_LockedByUserAndNotExpired_ReturnsWorkspace()
    {
        GrantImprovement();
        _repository.GetImprovementWorkspaceAsync(DocumentId)
            .Returns(Workspace(ReviewerId, DateTime.UtcNow.AddMinutes(30)));

        var service = CreateService();

        var result = await service.GetImprovementWorkspaceAsync(DocumentId, ReviewerId, FonbecRole.Reviewer);

        result.Should().NotBeNull();
        result!.DocumentId.Should().Be(DocumentId);
        result.PageCount.Should().Be(1);
        result.Pages.Should().ContainSingle(p => p.PageNumber == 1 && p.MimeType == "image/jpeg");
    }

    [Fact]
    public async Task GetImprovementWorkspace_LockedByAnotherUser_ReturnsNull()
    {
        GrantImprovement();
        _repository.GetImprovementWorkspaceAsync(DocumentId)
            .Returns(Workspace(ReviewerId + 1, DateTime.UtcNow.AddMinutes(30)));

        var service = CreateService();

        var result = await service.GetImprovementWorkspaceAsync(DocumentId, ReviewerId, FonbecRole.Reviewer);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetImprovementWorkspace_ExpiredLock_ReturnsNull()
    {
        GrantImprovement();
        _repository.GetImprovementWorkspaceAsync(DocumentId)
            .Returns(Workspace(ReviewerId, DateTime.UtcNow.AddMinutes(-1)));

        var service = CreateService();

        var result = await service.GetImprovementWorkspaceAsync(DocumentId, ReviewerId, FonbecRole.Reviewer);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetImprovementWorkspace_WithoutGrant_ReturnsNullWithoutQueryingRepository()
    {
        _userService.GetFonbecGrantsClaim(Arg.Any<int>()).Returns(string.Empty);
        _userService.HasPermission(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .Returns(false);

        var service = CreateService();

        var result = await service.GetImprovementWorkspaceAsync(DocumentId, ReviewerId, FonbecRole.Reviewer);

        result.Should().BeNull();
        await _repository.DidNotReceive().GetImprovementWorkspaceAsync(Arg.Any<long>());
    }
}