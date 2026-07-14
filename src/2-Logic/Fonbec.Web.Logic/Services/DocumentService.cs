using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.DataModels.Documents.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Models.Users.Output;
using Fonbec.Web.Logic.Options;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fonbec.Web.Logic.Services;

public interface IDocumentService
{
    Task<CrudResult<long>> CreateLetterAsync(CreateLetterInputModel input);
    Task<CrudResult<long>> CreateReportCardAsync(CreateReportCardInputModel input);
    Task<CrudResult<long>> CreateOtherDocumentAsync(CreateOtherDocumentInputModel input);
    Task<CrudResult<long>> CreateLetterWithBlobAsync(CreateLetterWithBlobInputModel input);
    Task<CrudResult<long>> CreateReportCardWithBlobAsync(CreateReportCardWithBlobInputModel input);
    Task<CrudResult<long>> CreateOtherDocumentWithBlobAsync(CreateOtherDocumentWithBlobInputModel input);
    Task<DownloadBlobResult?> DownloadDocumentBlobAsync(long documentId, int requestingUserId);
    Task<DownloadBlobResult?> DownloadOriginalDocumentBlobAsync(long documentId, int requestingUserId);
    Task<CrudResult> SubmitDigitalImprovementWithBlobAsync(SubmitDigitalImprovementWithBlobInputModel input);
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
    Task<List<DocumentDescriptionOptionViewModel>> GetDescriptionOptionsAsync(int chapterId, DocumentType documentType);
}

public class DocumentService(
    IDocumentRepository documentRepository,
    IDocumentNotificationService documentNotificationService,
    IUserService userService,
    IBlobStorageService blobStorageService,
    IOptions<BlobStorageOptions> blobStorageOptions,
    ILogger<DocumentService> logger) : IDocumentService
{
    private readonly BlobStorageOptions _blobStorageOptions = blobStorageOptions.Value;

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

        if (string.IsNullOrWhiteSpace(input.Description))
        {
            return new CrudResult<long>(Errors: [DocumentMessages.DescriptionRequired]);
        }

        if (input.Period == default)
        {
            return new CrudResult<long>(Errors: [DocumentMessages.ReportCardPeriodRequired]);
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

        if (string.IsNullOrWhiteSpace(input.Description))
        {
            return new CrudResult<long>(Errors: [DocumentMessages.DescriptionRequired]);
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

    public async Task<CrudResult<long>> CreateLetterWithBlobAsync(CreateLetterWithBlobInputModel input)
    {
        var authError = await ValidateUploadAuthorizationAsync(input.User, input.StudentId);
        if (authError is not null)
        {
            return new CrudResult<long>(Errors: [authError]);
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

        return await UploadAndCreateAsync(
            input.Content,
            input.MimeType,
            _ => BlobPathBuilder.Letter(student.ChapterId, input.PlanId, input.StudentId, input.SponsorId, _, improved: false),
            upload => new CreateLetterInputDataModel
            {
                StudentId = input.StudentId,
                PlanId = input.PlanId,
                SponsorId = input.SponsorId,
                UploadedById = input.User.UserId,
                FileKind = FileKind.Blob,
                Blob = ToBlobDataModel(upload),
                UploaderNotes = input.UploaderNotes,
            },
            documentRepository.CreateLetterAsync);
    }

    public async Task<CrudResult<long>> CreateReportCardWithBlobAsync(CreateReportCardWithBlobInputModel input)
    {
        var authError = await ValidateUploadAuthorizationAsync(input.User, input.StudentId);
        if (authError is not null)
        {
            return new CrudResult<long>(Errors: [authError]);
        }

        if (string.IsNullOrWhiteSpace(input.Description))
        {
            return new CrudResult<long>(Errors: [DocumentMessages.DescriptionRequired]);
        }

        if (input.Period == default)
        {
            return new CrudResult<long>(Errors: [DocumentMessages.ReportCardPeriodRequired]);
        }

        var student = await documentRepository.GetStudentUploadContextAsync(input.StudentId);
        if (student is not { IsActive: true })
        {
            return new CrudResult<long>(Errors: [DocumentMessages.StudentNotFoundOrInactive]);
        }

        return await UploadAndCreateAsync(
            input.Content,
            input.MimeType,
            _ => BlobPathBuilder.ReportCard(student.ChapterId, input.StudentId, _, improved: false),
            upload => new CreateReportCardInputDataModel
            {
                StudentId = input.StudentId,
                Period = input.Period,
                Description = input.Description,
                UploadedById = input.User.UserId,
                FileKind = FileKind.Blob,
                Blob = ToBlobDataModel(upload),
                UploaderNotes = input.UploaderNotes,
            },
            documentRepository.CreateReportCardAsync);
    }

    public async Task<CrudResult<long>> CreateOtherDocumentWithBlobAsync(CreateOtherDocumentWithBlobInputModel input)
    {
        var authError = await ValidateUploadAuthorizationAsync(input.User, input.StudentId);
        if (authError is not null)
        {
            return new CrudResult<long>(Errors: [authError]);
        }

        if (string.IsNullOrWhiteSpace(input.Description))
        {
            return new CrudResult<long>(Errors: [DocumentMessages.DescriptionRequired]);
        }

        var student = await documentRepository.GetStudentUploadContextAsync(input.StudentId);
        if (student is not { IsActive: true })
        {
            return new CrudResult<long>(Errors: [DocumentMessages.StudentNotFoundOrInactive]);
        }

        return await UploadAndCreateAsync(
            input.Content,
            input.MimeType,
            _ => BlobPathBuilder.Other(student.ChapterId, input.StudentId, _, improved: false),
            upload => new CreateOtherDocumentInputDataModel
            {
                StudentId = input.StudentId,
                Description = input.Description,
                UploadedById = input.User.UserId,
                FileKind = FileKind.Blob,
                Blob = ToBlobDataModel(upload),
                UploaderNotes = input.UploaderNotes,
            },
            documentRepository.CreateOtherDocumentAsync);
    }

    public async Task<DownloadBlobResult?> DownloadDocumentBlobAsync(long documentId, int requestingUserId)
    {
        var context = await documentRepository.GetDocumentBlobContextAsync(documentId);
        if (context is null)
        {
            return null;
        }

        var user = await GetUserAuthContextAsync(requestingUserId);
        if (user is null)
        {
            return null;
        }

        if (!IsAuthorizedForActiveDownload(user.Value, context, requestingUserId))
        {
            return null;
        }

        return await DownloadBlobAsync(context.ActiveBlob, documentId);
    }

    public async Task<DownloadBlobResult?> DownloadOriginalDocumentBlobAsync(long documentId, int requestingUserId)
    {
        var context = await documentRepository.GetDocumentBlobContextAsync(documentId);
        if (context is null)
        {
            return null;
        }

        var user = await GetUserAuthContextAsync(requestingUserId);
        if (user is null)
        {
            return null;
        }

        // The original blob is only available to a user who holds the improvement lock
        // and has the DigitalImprovement permission.
        if (context.ImprovementLockedById != requestingUserId
            || !CanImproveDigitally(user.Value.Role, user.Value.FonbecAuthClaim))
        {
            return null;
        }

        return await DownloadBlobAsync(context.OriginalBlob, documentId);
    }

    public async Task<CrudResult> SubmitDigitalImprovementWithBlobAsync(SubmitDigitalImprovementWithBlobInputModel input)
    {
        if (!CanImproveDigitally(input.UserRole, input.FonbecAuthClaim))
        {
            return new CrudResult(Errors: [DocumentMessages.NotAuthorizedDigitalImprovement]);
        }

        var context = await documentRepository.GetDocumentBlobContextAsync(input.DocumentId);
        if (context is null || context.ImprovementLockedById != input.UserId)
        {
            return new CrudResult(Errors: [DocumentMessages.DocumentNotFoundOrImprovementLockNotHeld]);
        }

        if (context.DigitalImprovementStatus != DigitalImprovementStatus.InProgress
            || context.OriginalBlob is null
            || !DocumentMimeTypes.IsImage(context.OriginalBlob.MimeType))
        {
            return new CrudResult(Errors: [DocumentMessages.DocumentNotEligibleForImprovement]);
        }

        if (!DocumentMimeTypes.IsImage(input.MimeType))
        {
            return new CrudResult(Errors: [DocumentMessages.ImprovedBlobMustBeImage]);
        }

        await using var buffer = await BufferAsync(input.Content);
        var validationError = ValidateBlobFile(input.MimeType, buffer.Length);
        if (validationError is not null)
        {
            return new CrudResult(Errors: [validationError]);
        }

        var extension = DocumentMimeTypes.GetExtension(input.MimeType)!;
        var blobName = context.DocumentType switch
        {
            DocumentType.Letter => BlobPathBuilder.Letter(
                context.ChapterId, context.PlanId.GetValueOrDefault(), context.StudentId,
                context.SponsorId.GetValueOrDefault(), extension, improved: true),
            DocumentType.ReportCard => BlobPathBuilder.ReportCard(
                context.ChapterId, context.StudentId, extension, improved: true),
            _ => BlobPathBuilder.Other(
                context.ChapterId, context.StudentId, extension, improved: true),
        };

        var upload = await blobStorageService.UploadAsync(buffer, blobName, input.MimeType);

        var dataModel = new SubmitDigitalImprovementInputDataModel
        {
            DocumentId = input.DocumentId,
            UserId = input.UserId,
            ImprovedBlob = ToBlobDataModel(upload),
            RowVersion = input.RowVersion,
        };

        try
        {
            var errors = await documentRepository.SubmitDigitalImprovementAsync(dataModel);
            if (errors.Count > 0)
            {
                await blobStorageService.DeleteAsync(blobName);
                return new CrudResult(Errors: errors);
            }

            return new CrudResult(1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to persist digital improvement for document {DocumentId}; rolling back improved blob {BlobName}.",
                input.DocumentId, blobName);
            await blobStorageService.DeleteAsync(blobName);
            return new CrudResult(Errors: [DocumentMessages.DocumentSaveFailed]);
        }
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

    public async Task<List<DocumentDescriptionOptionViewModel>> GetDescriptionOptionsAsync(
        int chapterId, DocumentType documentType)
    {
        var options = await documentRepository.GetDescriptionOptionsAsync(chapterId, documentType);
        return options.Adapt<List<DocumentDescriptionOptionViewModel>>();
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

    private async Task<CrudResult<long>> UploadAndCreateAsync<TDataModel>(
        Stream content,
        string mimeType,
        Func<string, string> buildBlobName,
        Func<UploadBlobResult, TDataModel> buildDataModel,
        Func<TDataModel, Task<CreateDocumentResultDataModel>> createAsync)
        where TDataModel : CreateDocumentBaseInputDataModel
    {
        await using var buffer = await BufferAsync(content);
        var validationError = ValidateBlobFile(mimeType, buffer.Length);
        if (validationError is not null)
        {
            return new CrudResult<long>(Errors: [validationError]);
        }

        var extension = DocumentMimeTypes.GetExtension(mimeType)!;
        var blobName = buildBlobName(extension);

        var upload = await blobStorageService.UploadAsync(buffer, blobName, mimeType);

        try
        {
            var result = await createAsync(buildDataModel(upload));
            if (!result.IsSuccess)
            {
                await blobStorageService.DeleteAsync(blobName);
                return new CrudResult<long>(Errors: result.Errors);
            }

            return new CrudResult<long>(result.DocumentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to persist document after uploading blob {BlobName}; rolling back the uploaded blob.",
                blobName);
            await blobStorageService.DeleteAsync(blobName);
            return new CrudResult<long>(Errors: [DocumentMessages.DocumentSaveFailed]);
        }
    }

    private string? ValidateBlobFile(string mimeType, long fileSizeBytes)
    {
        if (!_blobStorageOptions.AllowedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase)
            || DocumentMimeTypes.GetExtension(mimeType) is null)
        {
            return DocumentMessages.InvalidMimeType;
        }

        if (fileSizeBytes > _blobStorageOptions.MaxFileSizeBytes)
        {
            return DocumentMessages.FileTooLarge;
        }

        return null;
    }

    private async Task<DownloadBlobResult?> DownloadBlobAsync(BlobPathDataModel? blob, long documentId)
    {
        if (blob is null)
        {
            logger.LogWarning("Document {DocumentId} has no blob to download.", documentId);
            return null;
        }

        var downloaded = await blobStorageService.DownloadAsync(blob.StoragePath);
        if (downloaded is null)
        {
            return null;
        }

        return new DownloadBlobResult
        {
            Content = downloaded.Content,
            MimeType = blob.MimeType,
            FileSizeBytes = blob.FileSizeBytes,
            Sha256 = blob.Sha256,
        };
    }

    private bool IsAuthorizedForActiveDownload(
        (string Role, int? ChapterId, string? FonbecAuthClaim) user,
        DocumentBlobContextDataModel context,
        int requestingUserId) =>
        user.Role switch
        {
            FonbecRole.Reviewer => true,
            FonbecRole.Manager => user.ChapterId == context.ChapterId
                                  || context.ReviewLockedById == requestingUserId
                                  || context.ImprovementLockedById == requestingUserId,
            FonbecRole.Uploader => context.UploadedById == requestingUserId,
            _ => false,
        };

    private async Task<(string Role, int? ChapterId, string? FonbecAuthClaim)?> GetUserAuthContextAsync(int userId)
    {
        GetUserOutputModel user;
        try
        {
            user = await userService.GetUserAsync(userId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not resolve requesting user {UserId} for blob authorization.", userId);
            return null;
        }

        if (user is null || string.IsNullOrEmpty(user.UserRole))
        {
            return null;
        }

        var fonbecAuthClaim = await userService.GetFonbecAuthClaim(userId);
        return (user.UserRole, user.ChapterId, fonbecAuthClaim);
    }

    private static CreateBlobPathInputDataModel ToBlobDataModel(UploadBlobResult upload) =>
        new()
        {
            StoragePath = upload.BlobName,
            MimeType = upload.MimeType,
            FileSizeBytes = upload.FileSizeBytes,
            Sha256 = upload.Sha256,
        };

    private static async Task<MemoryStream> BufferAsync(Stream content)
    {
        var buffer = new MemoryStream();
        await content.CopyToAsync(buffer);
        buffer.Position = 0;
        return buffer;
    }
}