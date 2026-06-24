using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Models.Results;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface IDocumentService
{
    Task<CrudResult<long>> CreateLetterAsync(CreateLetterInputModel input);
    Task<CrudResult<long>> CreateReportCardAsync(CreateReportCardInputModel input);
    Task<CrudResult<long>> CreateOtherDocumentAsync(CreateOtherDocumentInputModel input);
    Task<DocumentQueueItemViewModel?> TakeNextForReviewAsync(int userId, string userRole);
    Task ReleaseReviewLockAsync(long documentId, int userId);
    Task<DocumentQueueItemViewModel?> TakeNextForDigitalImprovementAsync(int userId, string userRole, string? fonbecAuthClaim);
    Task<CrudResult> SubmitDigitalImprovementAsync(SubmitDigitalImprovementInputModel input);
    Task ReleaseImprovementLockAsync(long documentId, int userId);
    Task<ReviewResult> ApproveLetterAsync(ApproveLetterInputModel input);
    Task<ReviewResult> RejectLetterAsync(RejectLetterInputModel input);
    Task<ReviewResult> ApproveReportCardAsync(ApproveReportCardInputModel input);
    Task<ReviewResult> RejectReportCardAsync(RejectReportCardInputModel input);
    Task<ReviewResult> ApproveOtherDocumentAsync(ApproveOtherDocumentInputModel input);
    Task<ReviewResult> RejectOtherDocumentAsync(RejectOtherDocumentInputModel input);
    Task<SponsorDocumentHistoryViewModel> GetSharedDocumentsAsync(Guid sponsorPublicAccessToken, int studentId);
    Task<ReviewProgressViewModel> GetGlobalReviewProgressAsync(int userId, string userRole, int? planId);
    Task<LetterPlanProgressViewModel> GetLetterPlanProgressAsync(int userId, string userRole, int planId, int? chapterId);
}

public class DocumentService(
    IDocumentRepository documentRepository,
    IDocumentNotificationService documentNotificationService,
    IUserService userService) : IDocumentService
{
    public async Task<CrudResult<long>> CreateLetterAsync(CreateLetterInputModel input)
    {
        var authError = await ValidateUploadAuthorizationAsync(input.User, input.StudentId);
        if (authError is not null)
        {
            return new CrudResult<long>(Errors: [authError]);
        }

        var contentError = ValidateContent(input.FileKind, input.Blob, input.YouTubeVideoId, input.TextContent);
        if (contentError is not null)
        {
            return new CrudResult<long>(Errors: [contentError]);
        }

        var student = await documentRepository.GetStudentUploadContextAsync(input.StudentId);
        if (student is not { IsActive: true })
        {
            return new CrudResult<long>(Errors: [DocumentMessages.StudentNotFoundOrInactive]);
        }

        if (!await documentRepository.IsActivePlanAsync(input.PlanId, student.ChapterId))
        {
            return new CrudResult<long>(Errors: [DocumentMessages.NoActivePlan]);
        }

        if (!await documentRepository.HasActiveSponsorshipAsync(input.StudentId, input.SponsorId))
        {
            return new CrudResult<long>(Errors: [DocumentMessages.SponsorNotActiveForStudent]);
        }

        if (await documentRepository.HasDuplicateLetterAsync(input.StudentId, input.SponsorId, input.PlanId))
        {
            return new CrudResult<long>(Errors: [DocumentMessages.DuplicateLetter]);
        }

        var dataModel = input.Adapt<DataAccess.DataModels.Documents.Input.CreateLetterInputDataModel>();
        var result = await documentRepository.CreateLetterAsync(dataModel);

        return result.IsSuccess
            ? new CrudResult<long>(result.DocumentId)
            : new CrudResult<long>(Errors: result.Errors);
    }

    public async Task<CrudResult<long>> CreateReportCardAsync(CreateReportCardInputModel input)
    {
        var authError = await ValidateUploadAuthorizationAsync(input.User, input.StudentId);
        if (authError is not null)
        {
            return new CrudResult<long>(Errors: [authError]);
        }

        if (input.FileKind == FileKind.Text)
        {
            return new CrudResult<long>(Errors: [DocumentMessages.ReportCardCannotUseText]);
        }

        var contentError = ValidateContent(input.FileKind, input.Blob, input.YouTubeVideoId, input.TextContent);
        if (contentError is not null)
        {
            return new CrudResult<long>(Errors: [contentError]);
        }

        var dataModel = input.Adapt<DataAccess.DataModels.Documents.Input.CreateReportCardInputDataModel>();
        var result = await documentRepository.CreateReportCardAsync(dataModel);

        return result.IsSuccess
            ? new CrudResult<long>(result.DocumentId)
            : new CrudResult<long>(Errors: result.Errors);
    }

    public async Task<CrudResult<long>> CreateOtherDocumentAsync(CreateOtherDocumentInputModel input)
    {
        var authError = await ValidateUploadAuthorizationAsync(input.User, input.StudentId);
        if (authError is not null)
        {
            return new CrudResult<long>(Errors: [authError]);
        }

        var contentError = ValidateContent(input.FileKind, input.Blob, input.YouTubeVideoId, input.TextContent);
        if (contentError is not null)
        {
            return new CrudResult<long>(Errors: [contentError]);
        }

        var dataModel = input.Adapt<DataAccess.DataModels.Documents.Input.CreateOtherDocumentInputDataModel>();
        var result = await documentRepository.CreateOtherDocumentAsync(dataModel);

        return result.IsSuccess
            ? new CrudResult<long>(result.DocumentId)
            : new CrudResult<long>(Errors: result.Errors);
    }

    public async Task<DocumentQueueItemViewModel?> TakeNextForReviewAsync(int userId, string userRole)
    {
        if (!CanReview(userRole))
        {
            return null;
        }

        var item = await documentRepository.TakeNextForReviewAsync(userId);
        return item?.Adapt<DocumentQueueItemViewModel>();
    }

    public Task ReleaseReviewLockAsync(long documentId, int userId) =>
        documentRepository.ReleaseReviewLockAsync(documentId, userId);

    public async Task<DocumentQueueItemViewModel?> TakeNextForDigitalImprovementAsync(
        int userId, string userRole, string? fonbecAuthClaim)
    {
        if (!CanImproveDigitally(userRole, fonbecAuthClaim))
        {
            throw new UnauthorizedAccessException(DocumentMessages.NotAuthorizedDigitalImprovement);
        }

        var item = await documentRepository.TakeNextForDigitalImprovementAsync(userId);
        return item?.Adapt<DocumentQueueItemViewModel>();
    }

    public async Task<CrudResult> SubmitDigitalImprovementAsync(SubmitDigitalImprovementInputModel input)
    {
        if (!CanImproveDigitally(input.UserRole, input.FonbecAuthClaim))
        {
            return new CrudResult(Errors: [DocumentMessages.NotAuthorizedDigitalImprovement]);
        }

        var dataModel = input.Adapt<DataAccess.DataModels.Documents.Input.SubmitDigitalImprovementInputDataModel>();
        var errors = await documentRepository.SubmitDigitalImprovementAsync(dataModel);
        return errors.Count == 0
            ? new CrudResult(1)
            : new CrudResult(Errors: errors);
    }

    public Task ReleaseImprovementLockAsync(long documentId, int userId) =>
        documentRepository.ReleaseImprovementLockAsync(documentId, userId);

    public async Task<ReviewResult> ApproveLetterAsync(ApproveLetterInputModel input)
    {
        if (!CanReview(input.ReviewerRole))
        {
            return new ReviewResult(false, [DocumentMessages.NotAuthorizedToReview]);
        }

        var document = await documentRepository.GetDocumentByIdAsync(input.DocumentId);
        if (document?.DocumentType != DocumentType.Letter)
        {
            return new ReviewResult(false, [DocumentMessages.DocumentIsNotLetter]);
        }

        if (!input.ConfirmedIsLetter
            || !input.ConfirmedAddressee
            || !input.ConfirmedSignerMatchesStudent)
        {
            return new ReviewResult(false, [DocumentMessages.LetterConfirmationsRequired]);
        }

        var dataModel = input.Adapt<DataAccess.DataModels.Documents.Input.ApproveLetterInputDataModel>();
        var errors = await documentRepository.ApproveLetterAsync(dataModel);
        if (errors.Count > 0)
        {
            return new ReviewResult(false, errors);
        }

        await documentNotificationService.NotifySponsorsAsync(input.DocumentId);
        return new ReviewResult(true);
    }

    public async Task<ReviewResult> RejectLetterAsync(RejectLetterInputModel input)
    {
        if (!CanReview(input.ReviewerRole))
        {
            return new ReviewResult(false, [DocumentMessages.NotAuthorizedToReview]);
        }

        var dataModel = input.Adapt<DataAccess.DataModels.Documents.Input.RejectLetterInputDataModel>();
        var errors = await documentRepository.RejectLetterAsync(dataModel);
        return errors.Count == 0
            ? new ReviewResult(true)
            : new ReviewResult(false, errors);
    }

    public async Task<ReviewResult> ApproveReportCardAsync(ApproveReportCardInputModel input)
    {
        if (!CanReview(input.ReviewerRole))
        {
            return new ReviewResult(false, [DocumentMessages.NotAuthorizedToReview]);
        }

        var document = await documentRepository.GetDocumentByIdAsync(input.DocumentId);
        if (document?.DocumentType != DocumentType.ReportCard)
        {
            return new ReviewResult(false, [DocumentMessages.DocumentIsNotReportCard]);
        }

        if (!input.ConfirmedIsReportCardOrTranscript || !input.ConfirmedStudentNameCorrect)
        {
            return new ReviewResult(false, [DocumentMessages.ReportCardConfirmationsRequired]);
        }

        var dataModel = input.Adapt<DataAccess.DataModels.Documents.Input.ApproveReportCardInputDataModel>();
        var errors = await documentRepository.ApproveReportCardAsync(dataModel);
        if (errors.Count > 0)
        {
            return new ReviewResult(false, errors);
        }

        await documentNotificationService.NotifySponsorsAsync(input.DocumentId);
        return new ReviewResult(true);
    }

    public async Task<ReviewResult> RejectReportCardAsync(RejectReportCardInputModel input)
    {
        if (!CanReview(input.ReviewerRole))
        {
            return new ReviewResult(false, [DocumentMessages.NotAuthorizedToReview]);
        }

        var dataModel = input.Adapt<DataAccess.DataModels.Documents.Input.RejectReportCardInputDataModel>();
        var errors = await documentRepository.RejectReportCardAsync(dataModel);
        return errors.Count == 0
            ? new ReviewResult(true)
            : new ReviewResult(false, errors);
    }

    public async Task<ReviewResult> ApproveOtherDocumentAsync(ApproveOtherDocumentInputModel input)
    {
        if (!CanReview(input.ReviewerRole))
        {
            return new ReviewResult(false, [DocumentMessages.NotAuthorizedToReview]);
        }

        var document = await documentRepository.GetDocumentByIdAsync(input.DocumentId);
        if (document?.DocumentType != DocumentType.Other)
        {
            return new ReviewResult(false, [DocumentMessages.DocumentIsNotOther]);
        }

        var dataModel = input.Adapt<DataAccess.DataModels.Documents.Input.ApproveOtherDocumentInputDataModel>();
        var errors = await documentRepository.ApproveOtherDocumentAsync(dataModel);
        if (errors.Count > 0)
        {
            return new ReviewResult(false, errors);
        }

        await documentNotificationService.NotifySponsorsAsync(input.DocumentId);
        return new ReviewResult(true);
    }

    public async Task<ReviewResult> RejectOtherDocumentAsync(RejectOtherDocumentInputModel input)
    {
        if (!CanReview(input.ReviewerRole))
        {
            return new ReviewResult(false, [DocumentMessages.NotAuthorizedToReview]);
        }

        if (!input.RejectedReasonId.HasValue)
        {
            return new ReviewResult(false, [DocumentMessages.RejectionReasonRequired]);
        }

        var dataModel = input.Adapt<DataAccess.DataModels.Documents.Input.RejectOtherDocumentInputDataModel>();
        var errors = await documentRepository.RejectOtherDocumentAsync(dataModel);
        return errors.Count == 0
            ? new ReviewResult(true)
            : new ReviewResult(false, errors);
    }

    public async Task<SponsorDocumentHistoryViewModel> GetSharedDocumentsAsync(
        Guid sponsorPublicAccessToken, int studentId)
    {
        var result = await documentRepository.GetSharedDocumentsAsync(sponsorPublicAccessToken, studentId);
        return new SponsorDocumentHistoryViewModel
        {
            IsAuthorized = result.IsAuthorized,
            Documents = result.Documents.Adapt<List<SharedDocumentViewModel>>(),
        };
    }

    public async Task<ReviewProgressViewModel> GetGlobalReviewProgressAsync(
        int userId, string userRole, int? planId)
    {
        if (!CanReview(userRole))
        {
            throw new UnauthorizedAccessException(DocumentMessages.NotAuthorizedReviewProgress);
        }

        var progress = await documentRepository.GetGlobalReviewProgressAsync(planId);
        return progress.Adapt<ReviewProgressViewModel>();
    }

    public async Task<LetterPlanProgressViewModel> GetLetterPlanProgressAsync(
        int userId, string userRole, int planId, int? chapterId)
    {
        if (userRole != FonbecRole.Manager)
        {
            throw new UnauthorizedAccessException(DocumentMessages.NotAuthorizedLetterPlanProgress);
        }

        var progress = await documentRepository.GetLetterPlanProgressAsync(planId, chapterId);
        return progress.Adapt<LetterPlanProgressViewModel>();
    }

    private async Task<string?> ValidateUploadAuthorizationAsync(CreateDocumentUserContext user, int studentId)
    {
        if (user.UserRole == FonbecRole.Admin)
        {
            return DocumentMessages.AdminCannotUpload;
        }

        if (user.UserRole is not (FonbecRole.Uploader or FonbecRole.Manager))
        {
            return DocumentMessages.NotAuthorizedToUpload;
        }

        var student = await documentRepository.GetStudentUploadContextAsync(studentId);
        if (student is not { IsActive: true })
        {
            return DocumentMessages.StudentNotFoundOrInactive;
        }

        if (user.UserRole == FonbecRole.Uploader && student.FacilitatorId != user.UserId)
        {
            return DocumentMessages.UploaderNotAssignedToStudent;
        }

        if (user.UserRole == FonbecRole.Manager && user.ChapterId != student.ChapterId)
        {
            return DocumentMessages.ManagerNotAuthorizedForChapter;
        }

        return null;
    }

    private static string? ValidateContent(
        FileKind fileKind,
        CreateBlobPathInputModel? blob,
        string? youTubeVideoId,
        string? textContent) =>
        fileKind switch
        {
            FileKind.Blob when blob is null => DocumentMessages.BlobContentRequired,
            FileKind.YouTube when string.IsNullOrWhiteSpace(youTubeVideoId) => DocumentMessages.YouTubeVideoIdRequired,
            FileKind.Text when string.IsNullOrWhiteSpace(textContent) => DocumentMessages.TextContentRequired,
            _ => null,
        };

    private bool CanReview(string userRole) =>
        userRole is FonbecRole.Reviewer or FonbecRole.Manager;

    private bool CanImproveDigitally(string userRole, string? fonbecAuthClaim) =>
        CanReview(userRole)
        && userService.HasPermission(fonbecAuthClaim, userRole, DocumentPermission.DigitalImprovement);
}