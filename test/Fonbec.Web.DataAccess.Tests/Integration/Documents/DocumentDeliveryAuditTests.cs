using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Documents.Input;

namespace Fonbec.Web.DataAccess.Tests.Integration.Documents;

public class DocumentDeliveryAuditTests
{
    private readonly DocumentTestFixture _fixture = new();

    [Fact]
    public async Task Improvement_Keeps_First_Lock_And_Stores_The_Lock_That_Finished_It()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            FileKind.Blob, Blob: new CreateBlobPathInputModel("orig.jpg", "image/jpeg")));
        create.IsSuccess.Should().BeTrue();

        var firstLock = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ReviewerId, "Reviewer");
        firstLock.Should().NotBeNull();

        var afterFirstLock = await _fixture.GetDocumentAsync(firstLock!.DocumentId);
        var firstLockedOn = afterFirstLock.ImprovementFirstLockedOn;
        firstLockedOn.Should().NotBeNull();
        afterFirstLock.ImprovementLockedAt.Should().Be(firstLockedOn);

        await _fixture.ExpireImprovementLockAsync(firstLock.DocumentId, TimeSpan.FromHours(2));

        var secondLock = await _fixture.DocumentService.TakeNextForDigitalImprovementAsync(
            _fixture.ManagerId, "Manager");
        secondLock.Should().NotBeNull();
        secondLock!.DocumentId.Should().Be(firstLock.DocumentId);

        var afterSecondLock = await _fixture.GetDocumentAsync(firstLock.DocumentId);
        afterSecondLock.ImprovementFirstLockedOn.Should().Be(firstLockedOn);
        afterSecondLock.ImprovementLockedAt.Should().BeAfter(DateTime.UtcNow.AddHours(-1));
        var finishingLock = afterSecondLock.ImprovementLockedAt;

        var submit = await _fixture.DocumentService.SubmitDigitalImprovementAsync(
            new SubmitDigitalImprovementInputModel(
                secondLock.DocumentId,
                _fixture.ManagerId,
                "Manager",
                null,
                [new CreateBlobPathInputModel("improved.jpg", "image/jpeg")],
                secondLock.RowVersion));

        submit.IsSuccess.Should().BeTrue();
        var finished = await _fixture.GetDocumentAsync(firstLock.DocumentId);
        finished.DecisionImprovementLockedOn.Should().Be(finishingLock);
        finished.DecisionImprovementLockedOn.Should().NotBe(firstLockedOn);
        finished.ImprovementCompletedOn.Should().NotBeNull();
        finished.ImprovementLockedAt.Should().BeNull();
        finished.ImprovementFirstLockedOn.Should().Be(firstLockedOn);
    }

    [Fact]
    public async Task Review_Keeps_First_Lock_And_Stores_The_Lock_That_Approved_It()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            FileKind.Text, TextContent: "Dear sponsor"));
        create.IsSuccess.Should().BeTrue();

        var firstLock = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        firstLock.Should().NotBeNull();

        var afterFirstLock = await _fixture.GetDocumentAsync(firstLock!.DocumentId);
        var firstLockedOn = afterFirstLock.ReviewFirstLockedOn;
        firstLockedOn.Should().NotBeNull();

        await _fixture.ExpireReviewLockAsync(firstLock.DocumentId, TimeSpan.FromHours(2));

        var secondLock = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ManagerId, "Manager");
        secondLock.Should().NotBeNull();

        var afterSecondLock = await _fixture.GetDocumentAsync(firstLock.DocumentId);
        afterSecondLock.ReviewFirstLockedOn.Should().Be(firstLockedOn);
        var finishingLock = afterSecondLock.DecisionReviewLockedOn;
        finishingLock.Should().BeNull();
        var currentLock = (await _fixture.GetQueueItemAsync(firstLock.DocumentId)).ReviewLockedAt;
        currentLock.Should().BeAfter(DateTime.UtcNow.AddHours(-1));

        var approve = await _fixture.DocumentService.ApproveLetterAsync(new ApproveLetterInputModel(
            secondLock!.DocumentId, _fixture.ManagerId, "Manager", secondLock.RowVersion,
            ConfirmedIsLetter: true,
            ConfirmedWrittenDate: DateTime.UtcNow.Date,
            ConfirmedAddressee: true,
            ConfirmedSignerMatchesStudent: true,
            SpellingScore: 4, PenmanshipScore: 4, ContentScore: 4,
            HasRedFlags: false, HasGreenFlags: false, IssuesNotes: null, Appraisal: null));

        approve.IsSuccess.Should().BeTrue();
        var approved = await _fixture.GetDocumentAsync(firstLock.DocumentId);
        approved.DecisionReviewLockedOn.Should().Be(currentLock);
        approved.DecisionReviewLockedOn.Should().NotBe(firstLockedOn);
        approved.ReviewFirstLockedOn.Should().Be(firstLockedOn);
        (await _fixture.GetQueueItemAsync(firstLock.DocumentId)).ReviewLockedAt.Should().BeNull();
    }

    [Fact]
    public async Task Reject_Stores_Reviewer_And_The_Lock_That_Rejected_It()
    {
        await _fixture.InitializeAsync();

        var create = await _fixture.DocumentService.CreateLetterAsync(new CreateLetterInputModel(
            _fixture.StudentId, _fixture.PlanId, _fixture.SponsorAId, _fixture.UploaderContext,
            FileKind.Text, TextContent: "Dear sponsor"));
        create.IsSuccess.Should().BeTrue();

        var locked = await _fixture.DocumentService.TakeNextForReviewAsync(_fixture.ReviewerId, "Reviewer");
        locked.Should().NotBeNull();
        var lockStart = (await _fixture.GetQueueItemAsync(locked!.DocumentId)).ReviewLockedAt;

        var reject = await _fixture.DocumentService.RejectLetterAsync(new RejectLetterInputModel(
            locked.DocumentId,
            _fixture.ReviewerId,
            "Reviewer",
            locked.RowVersion,
            RejectedReasonIds.Unreadable,
            RejectionNotes: null));

        reject.IsSuccess.Should().BeTrue();
        var rejected = await _fixture.GetDocumentAsync(locked.DocumentId);
        rejected.RejectedById.Should().Be(_fixture.ReviewerId);
        rejected.DecisionReviewLockedOn.Should().Be(lockStart);
        rejected.RejectedOn.Should().NotBeNull();
        (await _fixture.GetQueueItemAsync(locked.DocumentId)).ReviewLockedAt.Should().BeNull();
    }
}