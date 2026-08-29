using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.DataModels.Documents.Input;
using Fonbec.Web.DataAccess.Entities;
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
    Task<DownloadBlobResult?> DownloadDocumentBlobAsync(long documentId, int pageNumber, int requestingUserId);
    Task<DownloadBlobResult?> DownloadOriginalDocumentBlobAsync(long documentId, int pageNumber, int requestingUserId);
    Task<CrudResult> SubmitDigitalImprovementWithBlobAsync(SubmitDigitalImprovementWithBlobInputModel input);
    Task<DocumentQueueItemViewModel?> TakeNextForReviewAsync(int userId, string userRole);

    /// <summary>
    /// Returns the id of the document the reviewer currently holds a valid lock on, or <c>null</c>.
    /// Used to enforce one active review at a time and to resume an in-progress document.
    /// </summary>
    Task<long?> GetActiveReviewLockAsync(int userId, string userRole);

    /// <summary>
    /// Loads the review workspace for a document that must be currently locked to <paramref name="userId"/>.
    /// Returns <c>null</c> when the caller cannot review, the document does not exist, the lock is held by
    /// another user, or the lock has expired — in which case the UI redirects back to the queue.
    /// </summary>
    Task<ReviewWorkspaceViewModel?> GetReviewWorkspaceAsync(long documentId, int userId, string userRole);

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
    Task<SponsorDocumentHistoryViewModel> GetSharedDocumentsForCompanyAsync(Guid companyPublicAccessToken, int studentId);
    Task<ReviewProgressViewModel> GetGlobalReviewProgressAsync(int userId, string userRole, int? planId);
    Task<LetterPlanProgressViewModel> GetLetterPlanProgressAsync(int userId, string userRole, int planId, int? chapterId);
    Task<List<DocumentDescriptionOptionViewModel>> GetDescriptionOptionsAsync(int chapterId, DocumentType documentType);
    Task<List<RejectedReasonViewModel>> GetApplicableRejectedReasonsAsync(DocumentType documentType);
}

public class DocumentService(
    IDocumentRepository documentRepository,
    IDocumentNotificationService documentNotificationService,
    IUserService userService,
    IBlobStorageService blobStorageService,
    ILetterPlanProgressService letterPlanProgressService,
    IPlanCompletionService planCompletionService,
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

        var recipientError = await ValidateLetterRecipientAsync(input.StudentId, input.PlanId, input.SponsorId, input.CompanyId);
        if (recipientError is not null)
        {
            return new CrudResult<long>(Errors: [recipientError]);
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

        var recipientError = await ValidateLetterRecipientAsync(input.StudentId, input.PlanId, input.SponsorId, input.CompanyId);
        if (recipientError is not null)
        {
            return new CrudResult<long>(Errors: [recipientError]);
        }

        var uploadedOn = DateOnly.FromDateTime(DateTime.UtcNow);
        return await UploadAndCreateAsync(
            input.Files,
            (extension, pageNumber, pageCount) => input.SponsorId.HasValue
                ? BlobPathBuilder.LetterForPersonSponsor(student.ChapterId, input.StudentId, input.SponsorId.Value, input.PlanId, extension, isImproved: false, uploadedOn, pageNumber, pageCount)
                : BlobPathBuilder.LetterForCompanySponsor(student.ChapterId, input.StudentId, input.CompanyId!.Value, input.PlanId, extension, isImproved: false, uploadedOn, pageNumber, pageCount),
            blobs => new CreateLetterInputDataModel
            {
                StudentId = input.StudentId,
                PlanId = input.PlanId,
                SponsorId = input.SponsorId,
                CompanyId = input.CompanyId,
                UploadedById = input.User.UserId,
                FileKind = FileKind.Blob,
                Blobs = blobs,
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

        var uploadedOn = DateOnly.FromDateTime(DateTime.UtcNow);
        return await UploadAndCreateAsync(
            input.Files,
            (extension, pageNumber, pageCount) => BlobPathBuilder.ReportCard(student.ChapterId, input.StudentId, extension, isImproved: false, uploadedOn, pageNumber, pageCount),
            blobs => new CreateReportCardInputDataModel
            {
                StudentId = input.StudentId,
                Period = input.Period,
                Description = input.Description,
                UploadedById = input.User.UserId,
                FileKind = FileKind.Blob,
                Blobs = blobs,
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

        var uploadedOn = DateOnly.FromDateTime(DateTime.UtcNow);
        return await UploadAndCreateAsync(
            input.Files,
            (extension, pageNumber, pageCount) => BlobPathBuilder.Other(student.ChapterId, input.StudentId, extension, isImproved: false, uploadedOn, pageNumber, pageCount),
            blobs => new CreateOtherDocumentInputDataModel
            {
                StudentId = input.StudentId,
                Description = input.Description,
                UploadedById = input.User.UserId,
                FileKind = FileKind.Blob,
                Blobs = blobs,
                UploaderNotes = input.UploaderNotes,
            },
            documentRepository.CreateOtherDocumentAsync);
    }

    public async Task<DownloadBlobResult?> DownloadDocumentBlobAsync(long documentId, int pageNumber, int requestingUserId)
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

        var page = context.Pages.FirstOrDefault(p => p.PageNumber == pageNumber);
        return await DownloadBlobAsync(page?.Active, documentId);
    }

    public async Task<DownloadBlobResult?> DownloadOriginalDocumentBlobAsync(long documentId, int pageNumber, int requestingUserId)
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

        var page = context.Pages.FirstOrDefault(p => p.PageNumber == pageNumber);
        return await DownloadBlobAsync(page?.Original, documentId);
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
            || context.Pages.Count == 0
            || context.Pages.Any(p => !DocumentMimeTypes.IsImage(p.Original.MimeType)))
        {
            return new CrudResult(Errors: [DocumentMessages.DocumentNotEligibleForImprovement]);
        }

        // Improvement is submitted for the whole document: one improved image per existing page,
        // provided in page order.
        if (input.Files.Count != context.Pages.Count)
        {
            return new CrudResult(Errors: [DocumentMessages.ImprovedPageCountMismatch]);
        }

        if (input.Files.Any(f => !DocumentMimeTypes.IsImage(f.MimeType)))
        {
            return new CrudResult(Errors: [DocumentMessages.ImprovedBlobMustBeImage]);
        }

        var buffers = new List<MemoryStream>();
        var uploadedBlobNames = new List<string>();
        try
        {
            long totalSize = 0;
            foreach (var file in input.Files)
            {
                var buffer = await BufferAsync(file.Content);
                buffers.Add(buffer);
                totalSize += buffer.Length;
            }

            if (totalSize > _blobStorageOptions.MaxFileSizeBytes)
            {
                return new CrudResult(Errors: [DocumentMessages.TotalFileSizeTooLarge]);
            }

            var uploadedOn = DateOnly.FromDateTime(DateTime.UtcNow);
            var pageCount = input.Files.Count;
            var improvedBlobs = new List<CreateBlobPathInputDataModel>();
            for (var i = 0; i < input.Files.Count; i++)
            {
                var mimeType = input.Files[i].MimeType;
                var extension = DocumentMimeTypes.GetExtension(mimeType)!;
                var pageNumber = i + 1;
                var blobName = context.DocumentType switch
                {
                    DocumentType.Letter when context.SponsorId.HasValue => BlobPathBuilder.LetterForPersonSponsor(
                        context.ChapterId, context.StudentId, context.SponsorId.Value,
                        context.PlanId.GetValueOrDefault(), extension, isImproved: true, uploadedOn, pageNumber, pageCount),
                    DocumentType.Letter => BlobPathBuilder.LetterForCompanySponsor(
                        context.ChapterId, context.StudentId, context.CompanyId.GetValueOrDefault(),
                        context.PlanId.GetValueOrDefault(), extension, isImproved: true, uploadedOn, pageNumber, pageCount),
                    DocumentType.ReportCard => BlobPathBuilder.ReportCard(
                        context.ChapterId, context.StudentId, extension, isImproved: true, uploadedOn, pageNumber, pageCount),
                    _ => BlobPathBuilder.Other(
                        context.ChapterId, context.StudentId, extension, isImproved: true, uploadedOn, pageNumber, pageCount),
                };

                var upload = await blobStorageService.UploadAsync(buffers[i], blobName, mimeType);
                uploadedBlobNames.Add(blobName);
                improvedBlobs.Add(ToBlobDataModel(upload));
            }

            var dataModel = new SubmitDigitalImprovementInputDataModel
            {
                DocumentId = input.DocumentId,
                UserId = input.UserId,
                ImprovedBlobs = improvedBlobs,
                RowVersion = input.RowVersion,
            };

            try
            {
                var errors = await documentRepository.SubmitDigitalImprovementAsync(dataModel);
                if (errors.Count > 0)
                {
                    await DeleteBlobsAsync(uploadedBlobNames);
                    return new CrudResult(Errors: errors);
                }

                return new CrudResult(1);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to persist digital improvement for document {DocumentId}; rolling back {Count} improved blob(s).",
                    input.DocumentId, uploadedBlobNames.Count);
                await DeleteBlobsAsync(uploadedBlobNames);
                return new CrudResult(Errors: [DocumentMessages.DocumentSaveFailed]);
            }
        }
        finally
        {
            foreach (var buffer in buffers)
            {
                await buffer.DisposeAsync();
            }
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

    public async Task<long?> GetActiveReviewLockAsync(int userId, string userRole)
    {
        if (!CanReview(userRole))
        {
            return null;
        }

        return await documentRepository.GetActiveReviewLockedDocumentIdAsync(userId);
    }

    public async Task<ReviewWorkspaceViewModel?> GetReviewWorkspaceAsync(long documentId, int userId, string userRole)
    {
        if (!CanReview(userRole))
        {
            return null;
        }

        var workspace = await documentRepository.GetReviewWorkspaceAsync(documentId);

        // Only the user who currently holds a non-expired lock may open the workspace. The server
        // re-enforces lock ownership on every review action; this check gates the read.
        if (workspace?.ReviewLockedById != userId
            || workspace.LockExpiresAtUtc is not { } expiresAt
            || expiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        return new ReviewWorkspaceViewModel
        {
            DocumentId = workspace.DocumentId,
            DocumentType = workspace.DocumentType,
            FileKind = workspace.FileKind,
            TextContent = workspace.TextContent,
            YouTubeVideoId = workspace.YouTubeVideoId,
            PageCount = workspace.PageCount,
            UploaderNotes = workspace.UploaderNotes,
            LockExpiresAtUtc = expiresAt,
            RowVersion = workspace.RowVersion,
        };
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

        if (document is Letter letter)
        {
            // Eventual consistency: a failure to re-evaluate plan completion must not roll back the
            // already-committed letter approval. The plan can be reconciled later.
            try
            {
                await planCompletionService.EvaluateAndUpdateAsync(
                    letter.PlanId,
                    letter.ChapterId,
                    input.ReviewerId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to evaluate plan completion for plan {PlanId} in chapter {ChapterId} after approving letter {DocumentId}.",
                    letter.PlanId, letter.ChapterId, input.DocumentId);
            }
        }

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

        var applicableReasons = await documentRepository.GetApplicableRejectedReasonsAsync(DocumentType.Other);
        var reason = applicableReasons.SingleOrDefault(r => r.RejectedReasonId == input.RejectedReasonId.Value);

        if (reason is null)
        {
            return new ReviewResult(false, [DocumentMessages.RejectionReasonNotApplicable]);
        }

        if (reason.RequiresNotes && string.IsNullOrWhiteSpace(input.RejectionNotes))
        {
            return new ReviewResult(false, [DocumentMessages.RejectionNotesRequiredForOtherReason]);
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

    public async Task<SponsorDocumentHistoryViewModel> GetSharedDocumentsForCompanyAsync(
        Guid companyPublicAccessToken, int studentId)
    {
        var result = await documentRepository.GetSharedDocumentsForCompanyAsync(companyPublicAccessToken, studentId);
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

    public async Task<List<RejectedReasonViewModel>> GetApplicableRejectedReasonsAsync(DocumentType documentType)
    {
        var reasons = await documentRepository.GetApplicableRejectedReasonsAsync(documentType);
        return reasons.Adapt<List<RejectedReasonViewModel>>();
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

    /// <summary>
    /// Validates that a letter is addressed to exactly one recipient (a sponsor XOR a company),
    /// that the recipient has an active sponsorship with the student, and that no duplicate exists.
    /// </summary>
    private async Task<string?> ValidateLetterRecipientAsync(int studentId, int planId, int? sponsorId, int? companyId)
    {
        if (sponsorId.HasValue == companyId.HasValue)
        {
            return DocumentMessages.LetterRequiresRecipient;
        }

        if (sponsorId.HasValue)
        {
            if (!await documentRepository.HasActiveSponsorshipAsync(studentId, sponsorId.Value))
            {
                return DocumentMessages.SponsorNotActiveForStudent;
            }

            if (await documentRepository.HasDuplicateLetterAsync(studentId, sponsorId.Value, planId))
            {
                return DocumentMessages.DuplicateLetter;
            }

            return null;
        }

        if (!await documentRepository.HasActiveCompanySponsorshipAsync(studentId, companyId!.Value))
        {
            return DocumentMessages.CompanyNotActiveForStudent;
        }

        if (await documentRepository.HasDuplicateCompanyLetterAsync(studentId, companyId.Value, planId))
        {
            return DocumentMessages.DuplicateCompanyLetter;
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

    private static bool CanReview(string userRole) =>
        userRole is FonbecRole.Reviewer or FonbecRole.Manager;

    private bool CanImproveDigitally(string userRole, string? fonbecAuthClaim) =>
        CanReview(userRole)
        && userService.HasPermission(fonbecAuthClaim, userRole, DocumentPermission.DigitalImprovement);

    /// <summary>
    /// Uploads one or more files (pages) and creates the document. A document may consist of several
    /// files only when every file is an image (JPG/PNG); PDF/text documents are always a single file.
    /// The combined size of all files must not exceed <see cref="BlobStorageOptions.MaxFileSizeBytes"/>.
    /// On persistence failure all uploaded blobs are rolled back.
    /// </summary>
    private async Task<CrudResult<long>> UploadAndCreateAsync<TDataModel>(
        IReadOnlyList<UploadFileInputModel> files,
        Func<string, int, int, string> buildBlobName,
        Func<List<CreateBlobPathInputDataModel>, TDataModel> buildDataModel,
        Func<TDataModel, Task<CreateDocumentResultDataModel>> createAsync)
        where TDataModel : CreateDocumentBaseInputDataModel
    {
        if (files.Count == 0)
        {
            return new CrudResult<long>(Errors: [DocumentMessages.BlobContentRequired]);
        }

        foreach (var file in files)
        {
            var mimeError = ValidateMimeType(file.MimeType);
            if (mimeError is not null)
            {
                return new CrudResult<long>(Errors: [mimeError]);
            }
        }

        // Only image documents may consist of multiple files.
        if (files.Count > 1 && files.Any(f => !DocumentMimeTypes.IsImage(f.MimeType)))
        {
            return new CrudResult<long>(Errors: [DocumentMessages.MultipleFilesOnlyForImages]);
        }

        var buffers = new List<MemoryStream>();
        var uploadedBlobNames = new List<string>();
        try
        {
            long totalSize = 0;
            foreach (var file in files)
            {
                var buffer = await BufferAsync(file.Content);
                buffers.Add(buffer);
                totalSize += buffer.Length;
            }

            if (totalSize > _blobStorageOptions.MaxFileSizeBytes)
            {
                return new CrudResult<long>(Errors: [DocumentMessages.TotalFileSizeTooLarge]);
            }

            var blobs = new List<CreateBlobPathInputDataModel>();
            for (var i = 0; i < files.Count; i++)
            {
                var extension = DocumentMimeTypes.GetExtension(files[i].MimeType)!;
                var blobName = buildBlobName(extension, i + 1, files.Count);
                var upload = await blobStorageService.UploadAsync(buffers[i], blobName, files[i].MimeType);
                uploadedBlobNames.Add(blobName);
                blobs.Add(ToBlobDataModel(upload));
            }

            try
            {
                var result = await createAsync(buildDataModel(blobs));
                if (!result.IsSuccess)
                {
                    await DeleteBlobsAsync(uploadedBlobNames);
                    return new CrudResult<long>(Errors: result.Errors);
                }

                return new CrudResult<long>(result.DocumentId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to persist document after uploading {Count} blob(s); rolling back the uploaded blobs.",
                    uploadedBlobNames.Count);
                await DeleteBlobsAsync(uploadedBlobNames);
                return new CrudResult<long>(Errors: [DocumentMessages.DocumentSaveFailed]);
            }
        }
        finally
        {
            foreach (var buffer in buffers)
            {
                await buffer.DisposeAsync();
            }
        }
    }

    private async Task DeleteBlobsAsync(IEnumerable<string> blobNames)
    {
        foreach (var blobName in blobNames)
        {
            await blobStorageService.DeleteAsync(blobName);
        }
    }

    private string? ValidateMimeType(string mimeType)
    {
        if (!_blobStorageOptions.AllowedMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase)
            || DocumentMimeTypes.GetExtension(mimeType) is null)
        {
            return DocumentMessages.InvalidMimeType;
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

    private static bool IsAuthorizedForActiveDownload(
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