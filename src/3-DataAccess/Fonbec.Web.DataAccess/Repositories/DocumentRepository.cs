using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.DataModels.Documents.Input;
using Fonbec.Web.DataAccess.DataModels.Review;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Options;
using Fonbec.Web.DataAccess.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IDocumentRepository
{
    Task<StudentUploadContextDataModel?> GetStudentUploadContextAsync(int studentId);
    Task<bool> IsActivePlanAsync(int planId, int chapterId);
    Task<bool> HasActiveSponsorshipAsync(int studentId, int sponsorId, int planId);
    Task<bool> HasActiveCompanySponsorshipAsync(int studentId, int companyId, int planId);
    Task<bool> HasDuplicateLetterAsync(int studentId, int sponsorId, int planId);
    Task<bool> HasDuplicateCompanyLetterAsync(int studentId, int companyId, int planId);
    Task<CreateDocumentResultDataModel> CreateLetterAsync(CreateLetterInputDataModel input);
    Task<CreateDocumentResultDataModel> CreateReportCardAsync(CreateReportCardInputDataModel input);
    Task<CreateDocumentResultDataModel> CreateOtherDocumentAsync(CreateOtherDocumentInputDataModel input);
    Task<DocumentQueueItemDataModel?> TakeNextForReviewAsync(int userId);

    /// <summary>
    /// Review queue contents: improvement is finished, and the document is pending or held by a valid lock.
    /// Expired review locks are released first. Ordered oldest first.
    /// </summary>
    Task<List<ManagerQueueEntryDataModel>> ListReviewQueueAsync();

    /// <summary>
    /// Image-improvement queue contents, pending or in progress. Ordered oldest first.
    /// An expired improvement lock is listed as not actively locked.
    /// </summary>
    Task<List<ManagerQueueEntryDataModel>> ListImprovementQueueAsync();

    /// <summary>
    /// Locks one review-eligible document for <paramref name="userId"/> without advancing the fair-dequeue cursor.
    /// Fails when the user already holds another review lock, or the document is not free to take.
    /// </summary>
    Task<TakeReviewLockDataModel> TryTakeReviewLockAsync(long documentId, int userId);

    /// <summary>Document id the user currently holds a valid (non-expired) review lock on, or <c>null</c>.</summary>
    Task<long?> GetActiveReviewLockedDocumentIdAsync(int userId);

    /// <summary>Releases every review lock whose timeout has elapsed, returning those documents to the queue.</summary>
    Task ReleaseExpiredReviewLocksAsync();

    Task ReleaseReviewLockAsync(long documentId, int userId);
    Task<DocumentQueueItemDataModel?> TakeNextForDigitalImprovementAsync(int userId);

    /// <summary>Document id the user currently holds a valid (non-expired) improvement lock on, or <c>null</c>.</summary>
    Task<long?> GetActiveImprovementLockedDocumentIdAsync(int userId);

    Task<ImprovementWorkspaceDataModel?> GetImprovementWorkspaceAsync(long documentId);
    Task<List<string>> SubmitDigitalImprovementAsync(SubmitDigitalImprovementInputDataModel input);
    Task ReleaseImprovementLockAsync(long documentId, int userId);
    Task<List<string>> ApproveLetterAsync(ApproveLetterInputDataModel input);
    Task<List<string>> RejectLetterAsync(RejectLetterInputDataModel input);
    Task<List<string>> ApproveReportCardAsync(ApproveReportCardInputDataModel input);
    Task<List<string>> RejectReportCardAsync(RejectReportCardInputDataModel input);
    Task<List<string>> ApproveOtherDocumentAsync(ApproveOtherDocumentInputDataModel input);
    Task<List<string>> RejectOtherDocumentAsync(RejectOtherDocumentInputDataModel input);
    Task<SponsorDocumentHistoryDataModel> GetSharedDocumentsAsync(
        Guid sponsorPublicAccessToken, int studentId, int skip, int take);
    Task<SponsorDocumentHistoryDataModel> GetSharedDocumentsForCompanyAsync(
        Guid companyPublicAccessToken, int studentId, int skip, int take);

    /// <summary>
    /// Same authorization as the anonymous history page. Null when the token, recipient, or sponsorship fails.
    /// </summary>
    Task<RecipientHistoryAccessDataModel?> GetRecipientHistoryAccessAsync(
        Guid publicAccessToken, int studentId, bool isCompany);

    /// <summary>
    /// Letters shared with this recipient and this recipient's messages, oldest first within the page.
    /// <paramref name="skipFromEnd"/> skips that many of the newest items so older pages can be prepended.
    /// </summary>
    Task<RecipientThreadDataModel> GetRecipientThreadAsync(
        Guid publicAccessToken, int studentId, bool isCompany, int skipFromEnd, int take);

    /// <summary>
    /// Inserts a message with <c>SharedOn</c> null. Does not insert when unauthorized or the body is empty or too long.
    /// </summary>
    Task<SendRecipientMessageDataModel> SendRecipientMessageAsync(
        Guid publicAccessToken, int studentId, bool isCompany, string? body);

    /// <summary>
    /// Records an authorized history-page visit. Returns <c>false</c> without writing
    /// when the token or sponsorship is invalid.
    /// </summary>
    Task<bool> RecordSharedDocumentHistoryVisitAsync(
        Guid publicAccessToken, int studentId, bool isCompany);

    /// <summary>
    /// Returns the blob context for a document only when the token recipient still sponsors
    /// the student and a matching <c>DocumentShare</c> exists. Null on any authorization failure.
    /// </summary>
    Task<DocumentBlobContextDataModel?> TryGetSharedDocumentBlobContextAsync(
        Guid publicAccessToken, int studentId, long documentId, bool isCompany);
    Task<ReviewWorkspaceDataModel?> GetReviewWorkspaceAsync(long documentId);
    Task<ReviewProgressDataModel> GetGlobalReviewProgressAsync(int? planId);
    Task<LetterPlanProgressDataModel> GetLetterPlanProgressAsync(int planId, int? chapterId);
    Task<List<DocumentShareNotificationDataModel>> GetUnnotifiedSharesAsync(long documentId);

    /// <summary>Documents that still have at least one share with no notification timestamp.</summary>
    Task<List<long>> GetDocumentIdsWithUnnotifiedSharesAsync();

    Task MarkShareNotifiedAsync(long documentShareId, DateTime notifiedOn);
    Task<Document?> GetDocumentByIdAsync(long documentId);
    Task<DocumentBlobContextDataModel?> GetDocumentBlobContextAsync(long documentId);

    /// <summary>
    /// Blob context for an other-document only when it belongs to an active student of
    /// <paramref name="facilitatorId"/>. Null on any authorization failure.
    /// </summary>
    Task<DocumentBlobContextDataModel?> GetFacilitatorOtherDocumentBlobContextAsync(
        int facilitatorId, int studentId, long documentId);

    /// <summary>
    /// Every document for a student in <paramref name="chapterId"/> who is not deleted,
    /// newest first. Includes inactive students. Null when the student is missing, deleted,
    /// or in another chapter.
    /// </summary>
    Task<StudentDocumentHistoryDataModel?> GetStudentDocumentHistoryAsync(int chapterId, int studentId);

    /// <summary>
    /// Blob context when the document belongs to a non-deleted student of <paramref name="chapterId"/>.
    /// Null on any authorization failure.
    /// </summary>
    Task<DocumentBlobContextDataModel?> GetManagerStudentDocumentBlobContextAsync(
        int chapterId, int studentId, long documentId);
    Task<List<int>> GetActiveSponsorIdsForStudentAsync(int studentId);
    Task<List<DocumentDescriptionOptionDataModel>> GetDescriptionOptionsAsync(int chapterId, DocumentType documentType);
    Task<List<RejectedReasonDataModel>> GetApplicableRejectedReasonsAsync(DocumentType documentType);

    Task<DocumentRejectionBehavior?> GetChapterRejectionBehaviorAsync(int chapterId);

    Task<bool> SetChapterRejectionBehaviorAsync(int chapterId, DocumentRejectionBehavior behavior, int updatedById);

    Task<List<HeldRejectionDataModel>> ListHeldRejectionsAsync(int chapterId);

    Task<int> CountHeldRejectionsAsync(int chapterId);

    /// <summary>
    /// Approves a document held after rejection, keeps the rejection audit, and creates the same shares as an approval.
    /// </summary>
    Task<List<string>> ShareHeldRejectionAsync(long documentId, int managerId, int chapterId, byte[] rowVersion);

    /// <summary>Returns a held rejection to the facilitator. The rejection fields stay as the reviewer left them.</summary>
    Task<List<string>> ReturnHeldRejectionAsync(long documentId, int chapterId, byte[] rowVersion);

    Task<FacilitatorRejectionNotificationDataModel?> GetFacilitatorRejectionNotificationAsync(long documentId);

    /// <summary>Rejected documents whose facilitator email has not been sent or skipped.</summary>
    Task<List<long>> GetDocumentIdsWithUnnotifiedFacilitatorRejectionsAsync();

    Task MarkFacilitatorRejectionNotifiedAsync(long documentId, DateTime notifiedOn);

    Task<HeldRejectionManagerNotificationDataModel?> GetHeldRejectionManagerNotificationAsync(long documentId);

    /// <summary>Held rejections whose manager email has not been sent or skipped.</summary>
    Task<List<long>> GetDocumentIdsWithUnnotifiedHeldRejectionsAsync();

    Task MarkManagersNotifiedOfHeldRejectionAsync(long documentId, DateTime notifiedOn);
}

public class DocumentRepository(
    IDbContextFactory<FonbecWebDbContext> dbContext,
    TimeProvider timeProvider,
    IOptions<DocumentQueueOptions> queueOptions) : IDocumentRepository
{
    private static readonly string[] ImageMimeTypes = ["image/jpeg", "image/png"];

    private TimeSpan LockTimeout => TimeSpan.FromMinutes(queueOptions.Value.ReviewLockTimeoutMinutes);

    public async Task<StudentUploadContextDataModel?> GetStudentUploadContextAsync(int studentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await db.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId && !s.IsDeleted)
            .Select(s => new StudentUploadContextDataModel
            {
                StudentId = s.Id,
                ChapterId = s.ChapterId,
                FacilitatorId = s.FacilitatorId,
                IsActive = s.IsActive,
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsActivePlanAsync(int planId, int chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await CampaignQueries.Open(db.PlannedDeliveries.AsNoTracking())
            .AnyAsync(p => p.Id == planId
                           && (p.ChapterId == null || p.ChapterId == chapterId));
    }

    public async Task<bool> HasActiveSponsorshipAsync(int studentId, int sponsorId, int planId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var asOf = await OpenPlanStartsOnAsync(db, planId);
        if (asOf is null)
        {
            return false;
        }

        return await db.Sponsorships
            .AsNoTracking()
            .Where(CampaignQueries.Covers(asOf.Value))
            .AnyAsync(sp => sp.StudentId == studentId && sp.SponsorId == sponsorId);
    }

    public async Task<bool> HasActiveCompanySponsorshipAsync(int studentId, int companyId, int planId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var asOf = await OpenPlanStartsOnAsync(db, planId);
        if (asOf is null)
        {
            return false;
        }

        return await db.Sponsorships
            .AsNoTracking()
            .Where(CampaignQueries.Covers(asOf.Value))
            .AnyAsync(sp => sp.StudentId == studentId && sp.CompanyId == companyId);
    }

    private static Task<DateTime?> OpenPlanStartsOnAsync(FonbecWebDbContext db, int planId) =>
        CampaignQueries.SelectStartsOnAsync(
            CampaignQueries.Open(db.PlannedDeliveries.AsNoTracking()).Where(p => p.Id == planId));

    public async Task<bool> HasDuplicateLetterAsync(int studentId, int sponsorId, int planId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await db.Set<Letter>()
            .AsNoTracking()
            .AnyAsync(l => l.StudentId == studentId
                           && l.SponsorId == sponsorId
                           && l.PlanId == planId
                           && l.Status != DocumentStatus.Rejected);
    }

    public async Task<bool> HasDuplicateCompanyLetterAsync(int studentId, int companyId, int planId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await db.Set<Letter>()
            .AsNoTracking()
            .AnyAsync(l => l.StudentId == studentId
                           && l.CompanyId == companyId
                           && l.PlanId == planId
                           && l.Status != DocumentStatus.Rejected);
    }

    public Task<CreateDocumentResultDataModel> CreateLetterAsync(CreateLetterInputDataModel input) =>
        CreateDocumentAsync(input, requiresImprovement => new Letter
        {
            ChapterId = 0, // set below
            StudentId = input.StudentId,
            SponsorId = input.SponsorId,
            CompanyId = input.CompanyId,
            PlanId = input.PlanId,
            FileKind = input.FileKind,
            YouTubeVideoId = input.YouTubeVideoId,
            TextContent = input.TextContent,
            UploaderNotes = input.UploaderNotes,
            UploadedById = input.UploadedById,
            UploadedOn = DateTime.UtcNow,
            DigitalImprovementStatus = requiresImprovement
                ? DigitalImprovementStatus.Required
                : DigitalImprovementStatus.NotApplicable,
            Status = requiresImprovement
                ? DocumentStatus.DigitalImprovementPending
                : DocumentStatus.ReviewPending,
        });

    public Task<CreateDocumentResultDataModel> CreateReportCardAsync(CreateReportCardInputDataModel input) =>
        CreateDocumentAsync(input, requiresImprovement => new ReportCard
        {
            StudentId = input.StudentId,
            Period = input.Period,
            Description = input.Description,
            FileKind = input.FileKind,
            YouTubeVideoId = input.YouTubeVideoId,
            TextContent = input.TextContent,
            UploaderNotes = input.UploaderNotes,
            UploadedById = input.UploadedById,
            UploadedOn = DateTime.UtcNow,
            DigitalImprovementStatus = requiresImprovement
                ? DigitalImprovementStatus.Required
                : DigitalImprovementStatus.NotApplicable,
            Status = requiresImprovement
                ? DocumentStatus.DigitalImprovementPending
                : DocumentStatus.ReviewPending,
        });

    public Task<CreateDocumentResultDataModel> CreateOtherDocumentAsync(CreateOtherDocumentInputDataModel input) =>
        CreateDocumentAsync(input, requiresImprovement => new OtherDocument
        {
            StudentId = input.StudentId,
            Description = input.Description,
            FileKind = input.FileKind,
            YouTubeVideoId = input.YouTubeVideoId,
            TextContent = input.TextContent,
            UploaderNotes = input.UploaderNotes,
            UploadedById = input.UploadedById,
            UploadedOn = DateTime.UtcNow,
            DigitalImprovementStatus = requiresImprovement
                ? DigitalImprovementStatus.Required
                : DigitalImprovementStatus.NotApplicable,
            Status = requiresImprovement
                ? DocumentStatus.DigitalImprovementPending
                : DocumentStatus.ReviewPending,
        });

    private async Task<CreateDocumentResultDataModel> CreateDocumentAsync<TDocument>(
        CreateDocumentBaseInputDataModel input,
        Func<bool, TDocument> createEntity)
        where TDocument : Document
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var student = await db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == input.StudentId && !s.IsDeleted);

        if (student is not { IsActive: true })
        {
            return new CreateDocumentResultDataModel { Errors = [DocumentMessages.StudentNotFoundOrInactive] };
        }

        var requiresImprovement = false;

        if (input.FileKind == FileKind.Blob)
        {
            if (input.Blobs.Count == 0)
            {
                return new CreateDocumentResultDataModel { Errors = [DocumentMessages.BlobContentRequired] };
            }

            // Only images may span multiple files; all pages of an image document require improvement.
            requiresImprovement = input.Blobs.Any(b =>
                ImageMimeTypes.Contains(b.MimeType, StringComparer.OrdinalIgnoreCase));
        }

        var document = createEntity(requiresImprovement);
        document.ChapterId = student.ChapterId;
        document.RowVersion = new byte[8];

        db.Documents.Add(document);
        await db.SaveChangesAsync();

        if (input.FileKind == FileKind.Blob)
        {
            var pageNumber = 1;
            foreach (var blob in input.Blobs)
            {
                var blobPath = new BlobPath
                {
                    StoragePath = blob.StoragePath,
                    MimeType = blob.MimeType,
                    FileSizeBytes = blob.FileSizeBytes,
                    Sha256 = blob.Sha256,
                };
                db.BlobPaths.Add(blobPath);
                await db.SaveChangesAsync();

                db.DocumentPages.Add(new DocumentPage
                {
                    DocumentId = document.DocumentId,
                    PageNumber = pageNumber++,
                    OriginalBlobPathId = blobPath.BlobPathId,
                });
            }

            await db.SaveChangesAsync();
        }

        db.DocumentQueueItems.Add(new DocumentQueueItem
        {
            DocumentId = document.DocumentId,
            EnqueuedAt = DateTime.UtcNow,
            Priority = 0,
        });
        await db.SaveChangesAsync();

        return new CreateDocumentResultDataModel { DocumentId = document.DocumentId };
    }

    public async Task<DocumentQueueItemDataModel?> TakeNextForReviewAsync(int userId)
    {
        // Free any abandoned (expired) review locks first so no document stays locked forever and
        // the queue metrics stay honest ("check for documents that need to be unlocked").
        await ReleaseExpiredReviewLocksAsync();

        // A reviewer may hold only one review lock at a time. If they still hold a valid lock (e.g.
        // they navigated away, closed the browser, or signed in elsewhere), resume that document
        // rather than locking a new one — the original ReviewLockedAt is preserved, so the on-screen
        // countdown continues from where it was.
        var existingLock = await GetActiveReviewLockedQueueItemAsync(userId);
        if (existingLock is not null)
        {
            return existingLock;
        }

        // Lock the next review-eligible document whose lock is free: either never locked
        // (Status ReviewPending) or taken but abandoned past the timeout (Status ReviewOngoing with a
        // stale ReviewLockedAt). Selection is fair across chapters: highest Priority first,
        // then the next chapter after the last served one (wrap-around), then oldest EnqueuedAt
        // within that chapter. The document's RowVersion (and the cursor's) arbitrates concurrent
        // takes; the loser retries and picks the next free one.
        while (true)
        {
            await using var db = await dbContext.CreateDbContextAsync();

            var utcNow = timeProvider.GetUtcNow().UtcDateTime;
            var lockExpiredBefore = utcNow - LockTimeout;

            var eligible = db.DocumentQueueItems
                .Include(q => q.Document)
                .Where(q => (q.Document.DigitalImprovementStatus == DigitalImprovementStatus.NotApplicable
                             || q.Document.DigitalImprovementStatus == DigitalImprovementStatus.Complete)
                            && ((q.ReviewLockedById == null && q.Document.Status == DocumentStatus.ReviewPending)
                                || (q.ReviewLockedById != null
                                    && q.Document.Status == DocumentStatus.ReviewOngoing
                                    && q.ReviewLockedAt != null
                                    && q.ReviewLockedAt < lockExpiredBefore)));

            if (!await eligible.AnyAsync())
            {
                return null;
            }

            var minPriority = await eligible.MinAsync(q => q.Priority);
            var atPriority = eligible.Where(q => q.Priority == minPriority);

            var chapterIds = await atPriority
                .Select(q => q.Document.ChapterId)
                .Distinct()
                .OrderBy(id => id)
                .ToListAsync();

            var cursor = await db.ReviewQueueCursors
                .SingleOrDefaultAsync(c => c.Id == ReviewQueueCursor.SingletonId)
                ?? db.ReviewQueueCursors.Add(new ReviewQueueCursor
                {
                    Id = ReviewQueueCursor.SingletonId,
                    RowVersion = [1, 0, 0, 0, 0, 0, 0, 0],
                }).Entity;

            var nextChapter = NextChapterAfter(chapterIds, cursor.LastServedChapterId);

            var queueItem = await atPriority
                .Where(q => q.Document.ChapterId == nextChapter)
                .OrderBy(q => q.EnqueuedAt)
                .FirstAsync();

            var isExpiredRetake = queueItem.Document.Status == DocumentStatus.ReviewOngoing;

            queueItem.ReviewLockedById = userId;
            queueItem.ReviewLockedAt = utcNow;
            queueItem.Document.ReviewFirstLockedOn ??= utcNow;
            queueItem.DequeueCount++;
            cursor.LastServedChapterId = nextChapter;

            if (isExpiredRetake)
            {
                // Status is already ReviewOngoing. Force a guarded update so its RowVersion
                // still arbitrates concurrent re-takes, including when the first-lock timestamp
                // was already stored.
                db.Entry(queueItem.Document).State = EntityState.Modified;
            }
            else
            {
                queueItem.Document.Status = DocumentStatus.ReviewOngoing;
            }

            try
            {
                await db.SaveChangesAsync();
                return MapQueueItem(queueItem);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another reviewer took this document (or advanced the cursor) first; loop and
                // pick the next free one.
            }
        }
    }

    public async Task<List<ManagerQueueEntryDataModel>> ListReviewQueueAsync()
    {
        await ReleaseExpiredReviewLocksAsync();

        await using var db = await dbContext.CreateDbContextAsync();
        var lockValidFrom = timeProvider.GetUtcNow().UtcDateTime - LockTimeout;

        var rows = await db.DocumentQueueItems
            .AsNoTracking()
            .Where(q => (q.Document.DigitalImprovementStatus == DigitalImprovementStatus.NotApplicable
                         || q.Document.DigitalImprovementStatus == DigitalImprovementStatus.Complete)
                        && ((q.Document.Status == DocumentStatus.ReviewPending && q.ReviewLockedById == null)
                            || (q.Document.Status == DocumentStatus.ReviewOngoing
                                && q.ReviewLockedById != null
                                && q.ReviewLockedAt != null
                                && q.ReviewLockedAt >= lockValidFrom)))
            .OrderBy(q => q.EnqueuedAt)
            .ThenBy(q => q.DocumentId)
            .Select(q => new QueueEntryRow
            {
                DocumentId = q.DocumentId,
                DocumentType = q.Document.DocumentType,
                StudentName = q.Document.Student.FirstName + " " + q.Document.Student.LastName,
                ChapterName = q.Document.Chapter.Name,
                EnqueuedAt = q.EnqueuedAt,
                LockedById = q.ReviewLockedById,
                LockedByName = q.ReviewLockedBy == null
                    ? null
                    : q.ReviewLockedBy.FirstName + " " + q.ReviewLockedBy.LastName,
                LockedAt = q.ReviewLockedAt,
            })
            .ToListAsync();

        return rows.Select(row => ToManagerQueueEntry(row, lockValidFrom)).ToList();
    }

    public async Task<List<ManagerQueueEntryDataModel>> ListImprovementQueueAsync()
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var lockValidFrom = timeProvider.GetUtcNow().UtcDateTime - LockTimeout;

        var rows = await db.DocumentQueueItems
            .AsNoTracking()
            .Where(q => q.Document.Status == DocumentStatus.DigitalImprovementPending
                        || q.Document.Status == DocumentStatus.DigitalImprovementOngoing)
            .OrderBy(q => q.EnqueuedAt)
            .ThenBy(q => q.DocumentId)
            .Select(q => new QueueEntryRow
            {
                DocumentId = q.DocumentId,
                DocumentType = q.Document.DocumentType,
                StudentName = q.Document.Student.FirstName + " " + q.Document.Student.LastName,
                ChapterName = q.Document.Chapter.Name,
                EnqueuedAt = q.EnqueuedAt,
                LockedById = q.Document.ImprovementLockedById,
                LockedByName = q.Document.ImprovementLockedBy == null
                    ? null
                    : q.Document.ImprovementLockedBy.FirstName + " " + q.Document.ImprovementLockedBy.LastName,
                LockedAt = q.Document.ImprovementLockedAt,
            })
            .ToListAsync();

        return rows.Select(row => ToManagerQueueEntry(row, lockValidFrom)).ToList();
    }

    public async Task<TakeReviewLockDataModel> TryTakeReviewLockAsync(long documentId, int userId)
    {
        await ReleaseExpiredReviewLocksAsync();

        var existingLock = await GetActiveReviewLockedQueueItemAsync(userId);
        if (existingLock is not null)
        {
            return new TakeReviewLockDataModel
            {
                Outcome = existingLock.DocumentId == documentId
                    ? TakeReviewLockOutcome.Locked
                    : TakeReviewLockOutcome.AlreadyReviewingAnother,
                DocumentId = existingLock.DocumentId,
            };
        }

        while (true)
        {
            await using var db = await dbContext.CreateDbContextAsync();

            var utcNow = timeProvider.GetUtcNow().UtcDateTime;
            var lockExpiredBefore = utcNow - LockTimeout;

            var queueItem = await db.DocumentQueueItems
                .Include(q => q.Document)
                .FirstOrDefaultAsync(q => q.DocumentId == documentId);

            if (queueItem is null || !IsEligibleForSelectedReview(queueItem, lockExpiredBefore))
            {
                return new TakeReviewLockDataModel { Outcome = TakeReviewLockOutcome.Unavailable };
            }

            var isExpiredRetake = queueItem.Document.Status == DocumentStatus.ReviewOngoing;

            queueItem.ReviewLockedById = userId;
            queueItem.ReviewLockedAt = utcNow;
            queueItem.Document.ReviewFirstLockedOn ??= utcNow;
            queueItem.DequeueCount++;

            if (isExpiredRetake)
            {
                db.Entry(queueItem.Document).State = EntityState.Modified;
            }
            else
            {
                queueItem.Document.Status = DocumentStatus.ReviewOngoing;
            }

            try
            {
                await db.SaveChangesAsync();
                return new TakeReviewLockDataModel
                {
                    Outcome = TakeReviewLockOutcome.Locked,
                    DocumentId = documentId,
                };
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another reviewer took this document first; loop and report it unavailable
                // once the row is no longer eligible.
            }
        }
    }

    /// <summary>
    /// First chapter id strictly greater than <paramref name="lastServedChapterId"/>, wrapping to
    /// the smallest eligible id. A null cursor starts at the smallest eligible chapter.
    /// </summary>
    public static int NextChapterAfter(IReadOnlyList<int> sortedEligibleChapterIds, int? lastServedChapterId)
    {
        if (sortedEligibleChapterIds.Count == 0)
        {
            throw new ArgumentException("At least one eligible chapter is required.", nameof(sortedEligibleChapterIds));
        }

        if (lastServedChapterId is { } lastServed)
        {
            foreach (var chapterId in sortedEligibleChapterIds)
            {
                if (chapterId > lastServed)
                {
                    return chapterId;
                }
            }
        }

        return sortedEligibleChapterIds[0];
    }

    public async Task<long?> GetActiveReviewLockedDocumentIdAsync(int userId)
    {
        var queueItem = await GetActiveReviewLockedQueueItemAsync(userId);
        return queueItem?.DocumentId;
    }

    /// <summary>
    /// Returns the queue item the user currently holds a valid (non-expired) review lock on, or
    /// <c>null</c>. Read-only: the existing <c>ReviewLockedAt</c> is not touched, so a resumed lock
    /// keeps its original expiry.
    /// </summary>
    private async Task<DocumentQueueItemDataModel?> GetActiveReviewLockedQueueItemAsync(int userId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var lockValidFrom = utcNow - LockTimeout;

        var queueItem = await db.DocumentQueueItems
            .AsNoTracking()
            .Include(q => q.Document)
            .Where(q => q.ReviewLockedById == userId
                        && q.ReviewLockedAt != null
                        && q.ReviewLockedAt >= lockValidFrom)
            .OrderByDescending(q => q.ReviewLockedAt)
            .FirstOrDefaultAsync();

        return queueItem is null ? null : MapQueueItem(queueItem);
    }

    /// <summary>
    /// Releases every review lock whose timeout has elapsed: clears the lock fields and returns the
    /// document to <see cref="DocumentStatus.ReviewPending"/>. Best-effort — a concurrent take-next that
    /// swept the same items wins and this call simply no-ops.
    /// </summary>
    public async Task ReleaseExpiredReviewLocksAsync()
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var lockExpiredBefore = utcNow - LockTimeout;

        var expired = await db.DocumentQueueItems
            .Include(q => q.Document)
            .Where(q => q.ReviewLockedById != null
                        && q.ReviewLockedAt != null
                        && q.ReviewLockedAt < lockExpiredBefore)
            .ToListAsync();

        if (expired.Count == 0)
        {
            return;
        }

        foreach (var queueItem in expired)
        {
            queueItem.ReviewLockedById = null;
            queueItem.ReviewLockedAt = null;
            if (queueItem.Document.Status == DocumentStatus.ReviewOngoing)
            {
                queueItem.Document.Status = DocumentStatus.ReviewPending;
            }
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another reviewer's take-next swept the same locks first; the documents are already free.
        }
    }

    public async Task ReleaseReviewLockAsync(long documentId, int userId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var queueItem = await db.DocumentQueueItems
            .Include(q => q.Document)
            .FirstOrDefaultAsync(q => q.DocumentId == documentId && q.ReviewLockedById == userId);

        if (queueItem is null)
        {
            return;
        }

        queueItem.ReviewLockedById = null;
        queueItem.ReviewLockedAt = null;
        if (queueItem.Document.Status == DocumentStatus.ReviewOngoing)
        {
            queueItem.Document.Status = DocumentStatus.ReviewPending;
        }

        await db.SaveChangesAsync();
    }

    public async Task<DocumentQueueItemDataModel?> TakeNextForDigitalImprovementAsync(int userId)
    {
        // A reviewer may hold only one improvement lock at a time. Resume that document rather
        // than locking a new one — the original ImprovementLockedAt is preserved.
        var existingLock = await GetActiveImprovementLockedQueueItemAsync(userId);
        if (existingLock is not null)
        {
            return existingLock;
        }

        // Same free-lock rule as review, applied to the improvement lock: oldest image document
        // that is either awaiting improvement (Required, unlocked) or was taken but abandoned
        // past the timeout (InProgress with a stale ImprovementLockedAt). The improvement lock
        // fields live on the document itself, so its RowVersion arbitrates concurrent takes.
        while (true)
        {
            await using var db = await dbContext.CreateDbContextAsync();

            var utcNow = timeProvider.GetUtcNow().UtcDateTime;
            var lockExpiredBefore = utcNow - LockTimeout;

            var document = await db.Documents
                .Include(d => d.QueueItem)
                .Where(d => (d.DigitalImprovementStatus == DigitalImprovementStatus.Required
                             && d.ImprovementLockedById == null)
                            || (d.DigitalImprovementStatus == DigitalImprovementStatus.InProgress
                                && d.ImprovementLockedById != null
                                && d.ImprovementLockedAt != null
                                && d.ImprovementLockedAt < lockExpiredBefore))
                .OrderBy(d => d.QueueItem!.EnqueuedAt)
                .FirstOrDefaultAsync();

            if (document?.QueueItem is null)
            {
                return null;
            }

            document.ImprovementLockedById = userId;
            document.ImprovementLockedAt = utcNow;
            document.ImprovementFirstLockedOn ??= utcNow;
            document.DigitalImprovementStatus = DigitalImprovementStatus.InProgress;
            document.Status = DocumentStatus.DigitalImprovementOngoing;

            try
            {
                await db.SaveChangesAsync();
                return MapQueueItem(document.QueueItem, document);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another user took this document first; loop and pick the next free one.
            }
        }
    }

    public async Task<long?> GetActiveImprovementLockedDocumentIdAsync(int userId)
    {
        var queueItem = await GetActiveImprovementLockedQueueItemAsync(userId);
        return queueItem?.DocumentId;
    }

    /// <summary>
    /// Returns the queue item the user currently holds a valid (non-expired) improvement lock on, or
    /// <c>null</c>. Read-only: the existing <c>ImprovementLockedAt</c> is not touched, so a resumed lock
    /// keeps its original expiry.
    /// </summary>
    private async Task<DocumentQueueItemDataModel?> GetActiveImprovementLockedQueueItemAsync(int userId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var lockValidFrom = utcNow - LockTimeout;

        var document = await db.Documents
            .AsNoTracking()
            .Include(d => d.QueueItem)
            .Where(d => d.ImprovementLockedById == userId
                        && d.DigitalImprovementStatus == DigitalImprovementStatus.InProgress
                        && d.ImprovementLockedAt != null
                        && d.ImprovementLockedAt >= lockValidFrom
                        && d.QueueItem != null)
            .OrderByDescending(d => d.ImprovementLockedAt)
            .FirstOrDefaultAsync();

        return document?.QueueItem is null ? null : MapQueueItem(document.QueueItem, document);
    }

    public async Task<ImprovementWorkspaceDataModel?> GetImprovementWorkspaceAsync(long documentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var workspace = await db.Documents
            .AsNoTracking()
            .Where(d => d.DocumentId == documentId)
            .Select(d => new ImprovementWorkspaceDataModel
            {
                DocumentId = d.DocumentId,
                DocumentType = d.DocumentType,
                FileKind = d.FileKind,
                PageCount = d.Pages.Count,
                Pages = d.Pages
                    .OrderBy(p => p.PageNumber)
                    .Select(p => new ReviewWorkspacePageDataModel
                    {
                        PageNumber = p.PageNumber,
                        MimeType = p.OriginalBlobPath.MimeType,
                    })
                    .ToList(),
                UploaderNotes = d.UploaderNotes,
                ImprovementLockedById = d.ImprovementLockedById,
                ImprovementLockedAt = d.ImprovementLockedAt,
                RowVersion = d.RowVersion,
            })
            .FirstOrDefaultAsync();

        if (workspace is not null && workspace.ImprovementLockedAt is { } lockedAt)
        {
            workspace.LockExpiresAtUtc = lockedAt + LockTimeout;
        }

        return workspace;
    }

    public async Task<List<string>> SubmitDigitalImprovementAsync(SubmitDigitalImprovementInputDataModel input)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var document = await db.Documents
            .Include(d => d.Pages)
            .FirstOrDefaultAsync(d => d.DocumentId == input.DocumentId
                                      && d.ImprovementLockedById == input.UserId);

        if (document is null)
        {
            return [DocumentMessages.DocumentNotFoundOrImprovementLockNotHeld];
        }

        // One slot per page, in page order. A null entry keeps the original for that page.
        var pages = document.Pages.OrderBy(p => p.PageNumber).ToList();
        if (input.ImprovedBlobs.Count != pages.Count)
        {
            return [DocumentMessages.ImprovedPageCountMismatch];
        }

        db.Entry(document).Property(d => d.RowVersion).OriginalValue = input.RowVersion;

        for (var i = 0; i < pages.Count; i++)
        {
            var source = input.ImprovedBlobs[i];
            if (source is null)
            {
                continue;
            }

            var improvedBlob = new BlobPath
            {
                StoragePath = source.StoragePath,
                MimeType = source.MimeType,
                FileSizeBytes = source.FileSizeBytes,
                Sha256 = source.Sha256,
            };
            db.BlobPaths.Add(improvedBlob);
            await db.SaveChangesAsync();

            pages[i].ImprovedBlobPathId = improvedBlob.BlobPathId;
        }

        var completedOn = timeProvider.GetUtcNow().UtcDateTime;
        document.DecisionImprovementLockedOn = document.ImprovementLockedAt;
        document.ImprovementCompletedOn = completedOn;
        document.DigitalImprovementStatus = DigitalImprovementStatus.Complete;
        document.Status = DocumentStatus.ReviewPending;
        document.ImprovementLockedById = null;
        document.ImprovementLockedAt = null;

        try
        {
            await db.SaveChangesAsync();
            return [];
        }
        catch (DbUpdateConcurrencyException)
        {
            return [DocumentMessages.ConcurrencyConflict];
        }
    }

    public async Task ReleaseImprovementLockAsync(long documentId, int userId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var document = await db.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.ImprovementLockedById == userId);

        if (document is null)
        {
            return;
        }

        document.ImprovementLockedById = null;
        document.ImprovementLockedAt = null;
        document.DigitalImprovementStatus = DigitalImprovementStatus.Required;
        document.Status = DocumentStatus.DigitalImprovementPending;

        await db.SaveChangesAsync();
    }

    public async Task<List<string>> ApproveLetterAsync(ApproveLetterInputDataModel input)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var letter = await db.Set<Letter>()
            .Include(l => l.QueueItem)
            .FirstOrDefaultAsync(l => l.DocumentId == input.DocumentId
                                      && l.Status == DocumentStatus.ReviewOngoing
                                      && l.QueueItem!.ReviewLockedById == input.ReviewerId);

        if (letter is null)
        {
            return [DocumentMessages.LetterNotFoundOrNotLockedForReview];
        }

        db.Entry(letter).Property(d => d.RowVersion).OriginalValue = input.RowVersion;

        var assessment = new Assessment
        {
            SpellingScore = input.SpellingScore,
            PenmanshipScore = input.PenmanshipScore,
            ContentScore = input.ContentScore,
            HasRedFlags = input.HasRedFlags,
            RedFlagPriority = input.HasRedFlags ? input.RedFlagPriority : null,
            HasGreenFlags = input.HasGreenFlags,
            IssuesNotes = input.IssuesNotes,
            Appraisal = input.Appraisal,
        };
        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        var review = new LetterReview
        {
            DocumentId = letter.DocumentId,
            ConfirmedIsLetter = input.ConfirmedIsLetter,
            ConfirmedWrittenDate = input.ConfirmedWrittenDate,
            ConfirmedAddressee = input.ConfirmedAddressee,
            ConfirmedSignerMatchesStudent = input.ConfirmedSignerMatchesStudent,
            AssessmentId = assessment.AssessmentId,
            ReviewedById = input.ReviewerId,
            ReviewedOn = DateTime.UtcNow,
        };
        db.LetterReviews.Add(review);

        // A letter is addressed to exactly one recipient (a person-sponsor XOR a company). Both are
        // sponsors: a company additionally fans out to the person-sponsors linked to it.
        var directSponsorIds = letter.SponsorId.HasValue ? new[] { letter.SponsorId.Value } : [];
        var companyIds = letter.CompanyId.HasValue ? new[] { letter.CompanyId.Value } : [];
        var targets = await ResolveShareTargetsAsync(db, directSponsorIds, companyIds);

        return await FinalizeApprovalAsync(db, letter, input.ReviewerId, targets);
    }

    public async Task<List<string>> RejectLetterAsync(RejectLetterInputDataModel input) =>
        await RejectDocumentAsync(input.DocumentId, input.ReviewerId, input.RowVersion, input.RejectedReasonId,
            input.RejectionNotes, DocumentType.Letter);

    public async Task<List<string>> ApproveReportCardAsync(ApproveReportCardInputDataModel input)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var reportCard = await db.Set<ReportCard>()
            .Include(r => r.QueueItem)
            .FirstOrDefaultAsync(r => r.DocumentId == input.DocumentId
                                      && r.Status == DocumentStatus.ReviewOngoing
                                      && r.QueueItem!.ReviewLockedById == input.ReviewerId);

        if (reportCard is null)
        {
            return [DocumentMessages.ReportCardNotFoundOrNotLockedForReview];
        }

        db.Entry(reportCard).Property(d => d.RowVersion).OriginalValue = input.RowVersion;

        var review = new ReportCardReview
        {
            DocumentId = reportCard.DocumentId,
            ConfirmedIsReportCardOrTranscript = input.ConfirmedIsReportCardOrTranscript,
            ConfirmedPeriodMatches = input.ConfirmedPeriodMatches,
            ConfirmedStudentNameCorrect = input.ConfirmedStudentNameCorrect,
            OverallAssessment = input.OverallAssessment,
            Absences = input.Absences,
            ReviewedById = input.ReviewerId,
            ReviewedOn = DateTime.UtcNow,
        };
        db.ReportCardReviews.Add(review);

        var targets = await ResolveStudentShareTargetsAsync(db, reportCard.StudentId);
        return await FinalizeApprovalAsync(db, reportCard, input.ReviewerId, targets);
    }

    public async Task<List<string>> RejectReportCardAsync(RejectReportCardInputDataModel input) =>
        await RejectDocumentAsync(input.DocumentId, input.ReviewerId, input.RowVersion, input.RejectedReasonId,
            input.RejectionNotes, DocumentType.ReportCard);

    public async Task<List<string>> ApproveOtherDocumentAsync(ApproveOtherDocumentInputDataModel input)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var other = await db.Set<OtherDocument>()
            .Include(o => o.QueueItem)
            .FirstOrDefaultAsync(o => o.DocumentId == input.DocumentId
                                      && o.Status == DocumentStatus.ReviewOngoing
                                      && o.QueueItem!.ReviewLockedById == input.ReviewerId);

        if (other is null)
        {
            return [DocumentMessages.DocumentNotFoundOrNotLockedForReview];
        }

        db.Entry(other).Property(d => d.RowVersion).OriginalValue = input.RowVersion;

        var targets = await ResolveStudentShareTargetsAsync(db, other.StudentId);
        return await FinalizeApprovalAsync(db, other, input.ReviewerId, targets);
    }

    public async Task<List<string>> RejectOtherDocumentAsync(RejectOtherDocumentInputDataModel input) =>
        await RejectDocumentAsync(input.DocumentId, input.ReviewerId, input.RowVersion, input.RejectedReasonId,
            input.RejectionNotes, DocumentType.Other);

    public async Task<SponsorDocumentHistoryDataModel> GetSharedDocumentsAsync(
        Guid sponsorPublicAccessToken, int studentId, int skip, int take)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var sponsor = await db.Sponsors
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.PublicAccessToken == sponsorPublicAccessToken
                                      && s.IsActive
                                      && !s.IsDeleted);

        if (sponsor is null)
        {
            return new SponsorDocumentHistoryDataModel { IsAuthorized = false };
        }

        var utcNow = DateTime.UtcNow;
        var hasSponsorship = await db.Sponsorships
            .AsNoTracking()
            .AnyAsync(sp => sp.StudentId == studentId
                            && sp.IsActive
                            && sp.StartDate <= utcNow
                            && (sp.EndDate == null || sp.EndDate >= utcNow)
                            // Direct sponsorship, or the sponsor belongs to a company that sponsors the student
                            // (company letters are shared with the company's individual sponsors).
                            && (sp.SponsorId == sponsor.Id
                                || (sponsor.CompanyId != null && sp.CompanyId == sponsor.CompanyId)));

        if (!hasSponsorship)
        {
            return new SponsorDocumentHistoryDataModel { IsAuthorized = false };
        }

        return await BuildSharedHistoryAsync(
            db,
            studentId,
            $"{sponsor.FirstName} {sponsor.LastName}",
            sponsorId: sponsor.Id,
            companyId: null,
            skip,
            take,
            s => s.SponsorId == sponsor.Id && s.StudentId == studentId);
    }

    public async Task<SponsorDocumentHistoryDataModel> GetSharedDocumentsForCompanyAsync(
        Guid companyPublicAccessToken, int studentId, int skip, int take)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var company = await db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PublicAccessToken == companyPublicAccessToken && c.IsActive);

        if (company is null)
        {
            return new SponsorDocumentHistoryDataModel { IsAuthorized = false };
        }

        var utcNow = DateTime.UtcNow;
        var hasSponsorship = await db.Sponsorships
            .AsNoTracking()
            .AnyAsync(sp => sp.StudentId == studentId
                            && sp.CompanyId == company.Id
                            && sp.IsActive
                            && sp.StartDate <= utcNow
                            && (sp.EndDate == null || sp.EndDate >= utcNow));

        if (!hasSponsorship)
        {
            return new SponsorDocumentHistoryDataModel { IsAuthorized = false };
        }

        return await BuildSharedHistoryAsync(
            db,
            studentId,
            company.Name,
            sponsorId: null,
            companyId: company.Id,
            skip,
            take,
            s => s.CompanyId == company.Id && s.StudentId == studentId);
    }

    public async Task<RecipientHistoryAccessDataModel?> GetRecipientHistoryAccessAsync(
        Guid publicAccessToken, int studentId, bool isCompany)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await TryAuthorizeHistoryAsync(db, publicAccessToken, studentId, isCompany);
    }

    public async Task<RecipientThreadDataModel> GetRecipientThreadAsync(
        Guid publicAccessToken, int studentId, bool isCompany, int skipFromEnd, int take)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var access = await TryAuthorizeHistoryAsync(db, publicAccessToken, studentId, isCompany);
        if (access is null)
        {
            return new RecipientThreadDataModel { IsAuthorized = false };
        }

        if (skipFromEnd < 0)
        {
            skipFromEnd = 0;
        }

        if (take < 1)
        {
            take = 1;
        }

        var letterShares = db.DocumentShares
            .AsNoTracking()
            .Where(s => s.StudentId == studentId && s.Document.DocumentType == DocumentType.Letter);
        letterShares = access.SponsorId is int sponsorId
            ? letterShares.Where(s => s.SponsorId == sponsorId)
            : letterShares.Where(s => s.CompanyId == access.CompanyId);

        var letters = await letterShares
            .Select(s => new RecipientThreadItemDataModel
            {
                IsMessage = false,
                OccurredOnUtc = s.SharedOn,
                SortId = s.DocumentShareId,
                DocumentId = s.DocumentId,
                DocumentType = s.Document.DocumentType,
                FileKind = s.Document.FileKind,
                PageCount = s.Document.Pages.Count,
                PlanStartsOn = db.Set<Letter>()
                    .Where(l => l.DocumentId == s.DocumentId)
                    .Select(l => (DateTime?)l.Plan.StartsOn)
                    .FirstOrDefault(),
                TextContent = s.Document.FileKind == FileKind.Text ? s.Document.TextContent : null,
                YouTubeVideoId = s.Document.FileKind == FileKind.YouTube ? s.Document.YouTubeVideoId : null,
            })
            .ToListAsync();

        var messageQuery = db.RecipientMessages
            .AsNoTracking()
            .Where(m => m.StudentId == studentId);
        messageQuery = access.SponsorId is int messageSponsorId
            ? messageQuery.Where(m => m.SponsorId == messageSponsorId)
            : messageQuery.Where(m => m.CompanyId == access.CompanyId);

        var messages = await messageQuery
            .Select(m => new RecipientThreadItemDataModel
            {
                IsMessage = true,
                OccurredOnUtc = m.SentOn,
                SortId = m.RecipientMessageId,
                Body = m.Body,
                SentOn = m.SentOn,
                MessageSharedOn = m.SharedOn,
                RecipientMessageId = m.RecipientMessageId,
            })
            .ToListAsync();

        var ordered = letters
            .Concat(messages)
            .OrderBy(i => i.OccurredOnUtc)
            .ThenBy(i => i.IsMessage)
            .ThenBy(i => i.SortId)
            .ToList();

        var available = Math.Max(0, ordered.Count - skipFromEnd);
        var start = Math.Max(0, available - take);
        var page = ordered.Skip(start).Take(available - start).ToList();

        return new RecipientThreadDataModel
        {
            IsAuthorized = true,
            HasOlder = start > 0,
            Items = page,
        };
    }

    public async Task<SendRecipientMessageDataModel> SendRecipientMessageAsync(
        Guid publicAccessToken, int studentId, bool isCompany, string? body)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var access = await TryAuthorizeHistoryAsync(db, publicAccessToken, studentId, isCompany);
        if (access is null)
        {
            return new SendRecipientMessageDataModel { IsAuthorized = false };
        }

        var trimmed = body?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > MaxLength.RecipientMessage.Body)
        {
            return new SendRecipientMessageDataModel { IsAuthorized = true, IsValid = false };
        }

        var sentOn = timeProvider.GetUtcNow().UtcDateTime;
        var message = new RecipientMessage
        {
            StudentId = studentId,
            SponsorId = access.SponsorId,
            CompanyId = access.CompanyId,
            Body = trimmed,
            SentOn = sentOn,
        };
        db.RecipientMessages.Add(message);
        await db.SaveChangesAsync();

        return new SendRecipientMessageDataModel
        {
            IsAuthorized = true,
            IsValid = true,
            Message = new RecipientThreadItemDataModel
            {
                IsMessage = true,
                OccurredOnUtc = sentOn,
                SortId = message.RecipientMessageId,
                RecipientMessageId = message.RecipientMessageId,
                Body = trimmed,
                SentOn = sentOn,
            },
        };
    }

    public async Task<bool> RecordSharedDocumentHistoryVisitAsync(
        Guid publicAccessToken, int studentId, bool isCompany)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        if (isCompany)
        {
            var company = await db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.PublicAccessToken == publicAccessToken && c.IsActive);

            if (company is null)
            {
                return false;
            }

            var utcNow = DateTime.UtcNow;
            var hasSponsorship = await db.Sponsorships
                .AsNoTracking()
                .AnyAsync(sp => sp.StudentId == studentId
                                && sp.CompanyId == company.Id
                                && sp.IsActive
                                && sp.StartDate <= utcNow
                                && (sp.EndDate == null || sp.EndDate >= utcNow));

            if (!hasSponsorship)
            {
                return false;
            }

            await UpsertHistoryVisitAsync(db, studentId, sponsorId: null, companyId: company.Id);
            return true;
        }

        var sponsor = await db.Sponsors
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.PublicAccessToken == publicAccessToken
                                      && s.IsActive
                                      && !s.IsDeleted);

        if (sponsor is null)
        {
            return false;
        }

        var sponsorUtcNow = DateTime.UtcNow;
        var sponsorHasSponsorship = await db.Sponsorships
            .AsNoTracking()
            .AnyAsync(sp => sp.StudentId == studentId
                            && sp.IsActive
                            && sp.StartDate <= sponsorUtcNow
                            && (sp.EndDate == null || sp.EndDate >= sponsorUtcNow)
                            && (sp.SponsorId == sponsor.Id
                                || (sponsor.CompanyId != null && sp.CompanyId == sponsor.CompanyId)));

        if (!sponsorHasSponsorship)
        {
            return false;
        }

        await UpsertHistoryVisitAsync(db, studentId, sponsor.Id, companyId: null);
        return true;
    }

    public async Task<DocumentBlobContextDataModel?> TryGetSharedDocumentBlobContextAsync(
        Guid publicAccessToken, int studentId, long documentId, bool isCompany)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        if (isCompany)
        {
            var company = await db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.PublicAccessToken == publicAccessToken && c.IsActive);

            if (company is null)
            {
                return null;
            }

            var utcNow = DateTime.UtcNow;
            var hasSponsorship = await db.Sponsorships
                .AsNoTracking()
                .AnyAsync(sp => sp.StudentId == studentId
                                && sp.CompanyId == company.Id
                                && sp.IsActive
                                && sp.StartDate <= utcNow
                                && (sp.EndDate == null || sp.EndDate >= utcNow));

            if (!hasSponsorship)
            {
                return null;
            }

            var hasShare = await db.DocumentShares
                .AsNoTracking()
                .AnyAsync(s => s.CompanyId == company.Id
                               && s.StudentId == studentId
                               && s.DocumentId == documentId);

            if (!hasShare)
            {
                return null;
            }
        }
        else
        {
            var sponsor = await db.Sponsors
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.PublicAccessToken == publicAccessToken
                                          && s.IsActive
                                          && !s.IsDeleted);

            if (sponsor is null)
            {
                return null;
            }

            var utcNow = DateTime.UtcNow;
            var hasSponsorship = await db.Sponsorships
                .AsNoTracking()
                .AnyAsync(sp => sp.StudentId == studentId
                                && sp.IsActive
                                && sp.StartDate <= utcNow
                                && (sp.EndDate == null || sp.EndDate >= utcNow)
                                && (sp.SponsorId == sponsor.Id
                                    || (sponsor.CompanyId != null && sp.CompanyId == sponsor.CompanyId)));

            if (!hasSponsorship)
            {
                return null;
            }

            var hasShare = await db.DocumentShares
                .AsNoTracking()
                .AnyAsync(s => s.SponsorId == sponsor.Id
                               && s.StudentId == studentId
                               && s.DocumentId == documentId);

            if (!hasShare)
            {
                return null;
            }
        }

        return await GetDocumentBlobContextAsync(documentId);
    }

    /// <summary>
    /// Same token and active-sponsorship checks as the US 125 history page.
    /// Returns null on any failure, without saying which check failed.
    /// </summary>
    private static async Task<RecipientHistoryAccessDataModel?> TryAuthorizeHistoryAsync(
        FonbecWebDbContext db, Guid publicAccessToken, int studentId, bool isCompany)
    {
        int? sponsorId = null;
        int? companyId = null;

        if (isCompany)
        {
            var company = await db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.PublicAccessToken == publicAccessToken && c.IsActive);

            if (company is null)
            {
                return null;
            }

            var utcNow = DateTime.UtcNow;
            var hasSponsorship = await db.Sponsorships
                .AsNoTracking()
                .AnyAsync(sp => sp.StudentId == studentId
                                && sp.CompanyId == company.Id
                                && sp.IsActive
                                && sp.StartDate <= utcNow
                                && (sp.EndDate == null || sp.EndDate >= utcNow));

            if (!hasSponsorship)
            {
                return null;
            }

            companyId = company.Id;
        }
        else
        {
            var sponsor = await db.Sponsors
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.PublicAccessToken == publicAccessToken
                                          && s.IsActive
                                          && !s.IsDeleted);

            if (sponsor is null)
            {
                return null;
            }

            var utcNow = DateTime.UtcNow;
            var hasSponsorship = await db.Sponsorships
                .AsNoTracking()
                .AnyAsync(sp => sp.StudentId == studentId
                                && sp.IsActive
                                && sp.StartDate <= utcNow
                                && (sp.EndDate == null || sp.EndDate >= utcNow)
                                && (sp.SponsorId == sponsor.Id
                                    || (sponsor.CompanyId != null && sp.CompanyId == sponsor.CompanyId)));

            if (!hasSponsorship)
            {
                return null;
            }

            sponsorId = sponsor.Id;
        }

        var studentExists = await db.Students
            .AsNoTracking()
            .AnyAsync(s => s.Id == studentId && !s.IsDeleted);

        if (!studentExists)
        {
            return null;
        }

        return new RecipientHistoryAccessDataModel
        {
            SponsorId = sponsorId,
            CompanyId = companyId,
        };
    }

    private async Task<SponsorDocumentHistoryDataModel> BuildSharedHistoryAsync(
        FonbecWebDbContext db,
        int studentId,
        string recipientDisplayName,
        int? sponsorId,
        int? companyId,
        int skip,
        int take,
        System.Linq.Expressions.Expression<Func<DocumentShare, bool>> shareFilter)
    {
        var student = await db.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId && !s.IsDeleted)
            .Select(s => new { Name = s.FirstName + " " + s.LastName, s.Gender })
            .FirstOrDefaultAsync();

        if (student is null)
        {
            return new SponsorDocumentHistoryDataModel { IsAuthorized = false };
        }

        var previousLastVisitedOnUtc = await GetPreviousLastVisitedOnUtcAsync(db, studentId, sponsorId, companyId);

        var page = await db.DocumentShares
            .AsNoTracking()
            .Where(shareFilter)
            .Where(s => s.Document.DocumentType != DocumentType.Letter)
            .OrderByDescending(s => s.SharedOn)
            .Skip(skip)
            .Take(take + 1)
            .Select(s => new SharedDocumentDataModel
            {
                DocumentId = s.DocumentId,
                DocumentType = s.Document.DocumentType,
                SharedOn = s.SharedOn,
                FileKind = s.Document.FileKind,
                PageCount = s.Document.Pages.Count,
                PlanStartsOn = db.Set<Letter>()
                    .Where(l => l.DocumentId == s.DocumentId)
                    .Select(l => (DateTime?)l.Plan.StartsOn)
                    .FirstOrDefault(),
                ReportCardPeriod = db.Set<ReportCard>()
                    .Where(r => r.DocumentId == s.DocumentId)
                    .Select(r => (DateOnly?)r.Period)
                    .FirstOrDefault(),
                Description = s.Document.DocumentType == DocumentType.ReportCard
                    ? db.Set<ReportCard>()
                        .Where(r => r.DocumentId == s.DocumentId)
                        .Select(r => r.Description)
                        .FirstOrDefault()
                    : s.Document.DocumentType == DocumentType.Other
                        ? db.Set<OtherDocument>()
                            .Where(o => o.DocumentId == s.DocumentId)
                            .Select(o => o.Description)
                            .FirstOrDefault()
                        : null,
                TextContent = s.Document.FileKind == FileKind.Text ? s.Document.TextContent : null,
                YouTubeVideoId = s.Document.FileKind == FileKind.YouTube ? s.Document.YouTubeVideoId : null,
            })
            .ToListAsync();

        var hasMore = page.Count > take;
        if (hasMore)
        {
            page = page.Take(take).ToList();
        }

        var ccRecipientNames = sponsorId is null
            ? []
            : await db.SendAlsoTos
                .AsNoTracking()
                .Where(r => r.SponsorId == sponsorId && !r.SendAsBcc)
                .OrderBy(r => r.RecipientName)
                .Select(r => r.RecipientName)
                .ToListAsync();

        return new SponsorDocumentHistoryDataModel
        {
            IsAuthorized = true,
            StudentDisplayName = student.Name,
            StudentGender = student.Gender,
            RecipientDisplayName = recipientDisplayName,
            CcRecipientNames = ccRecipientNames,
            HasMore = hasMore,
            PreviousLastVisitedOnUtc = previousLastVisitedOnUtc,
            Documents = page,
        };
    }

    private static async Task<DateTime?> GetPreviousLastVisitedOnUtcAsync(
        FonbecWebDbContext db,
        int studentId,
        int? sponsorId,
        int? companyId)
    {
        if (sponsorId is not null)
        {
            return await db.DocumentHistoryVisits
                .AsNoTracking()
                .Where(v => v.SponsorId == sponsorId && v.StudentId == studentId)
                .Select(v => (DateTime?)v.LastVisitedOnUtc)
                .FirstOrDefaultAsync();
        }

        return await db.DocumentHistoryVisits
            .AsNoTracking()
            .Where(v => v.CompanyId == companyId && v.StudentId == studentId)
            .Select(v => (DateTime?)v.LastVisitedOnUtc)
            .FirstOrDefaultAsync();
    }

    private async Task UpsertHistoryVisitAsync(
        FonbecWebDbContext db,
        int studentId,
        int? sponsorId,
        int? companyId)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        DocumentHistoryVisit? visit;
        if (sponsorId is not null)
        {
            visit = await db.DocumentHistoryVisits
                .FirstOrDefaultAsync(v => v.SponsorId == sponsorId && v.StudentId == studentId);
        }
        else
        {
            visit = await db.DocumentHistoryVisits
                .FirstOrDefaultAsync(v => v.CompanyId == companyId && v.StudentId == studentId);
        }

        if (visit is null)
        {
            db.DocumentHistoryVisits.Add(new DocumentHistoryVisit
            {
                SponsorId = sponsorId,
                CompanyId = companyId,
                StudentId = studentId,
                LastVisitedOnUtc = now,
            });
        }
        else
        {
            visit.LastVisitedOnUtc = now;
        }

        await db.SaveChangesAsync();
    }

    public async Task<ReviewWorkspaceDataModel?> GetReviewWorkspaceAsync(long documentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var now = DateTime.UtcNow;

        var workspace = await db.DocumentQueueItems
            .AsNoTracking()
            .Where(q => q.DocumentId == documentId)
            .Select(q => new ReviewWorkspaceDataModel
            {
                DocumentId = q.Document.DocumentId,
                DocumentType = q.Document.DocumentType,
                FileKind = q.Document.FileKind,
                TextContent = q.Document.TextContent,
                YouTubeVideoId = q.Document.YouTubeVideoId,
                PageCount = q.Document.Pages.Count,
                Pages = q.Document.Pages
                    .OrderBy(p => p.PageNumber)
                    .Select(p => new ReviewWorkspacePageDataModel
                    {
                        PageNumber = p.PageNumber,
                        MimeType = p.ImprovedBlobPath != null
                            ? p.ImprovedBlobPath.MimeType
                            : p.OriginalBlobPath.MimeType,
                    })
                    .ToList(),
                UploaderNotes = q.Document.UploaderNotes,
                StudentId = q.Document.StudentId,
                SponsorId = q.Document.SponsorId,
                CompanyId = db.Set<Letter>()
                    .Where(l => l.DocumentId == q.DocumentId)
                    .Select(l => (int?)l.CompanyId)
                    .FirstOrDefault(),
                PlanStartsOn = db.Set<Letter>()
                    .Where(l => l.DocumentId == q.DocumentId)
                    .Select(l => (DateTime?)l.Plan.StartsOn)
                    .FirstOrDefault(),
                ReportCardPeriod = db.Set<ReportCard>()
                    .Where(r => r.DocumentId == q.DocumentId)
                    .Select(r => (DateOnly?)r.Period)
                    .FirstOrDefault(),
                StudentEducationLevel = q.Document.Student.UniversityStartYear <= now
                    ? EducationLevel.University
                    : q.Document.Student.SecondarySchoolStartYear <= now
                        ? EducationLevel.SecondarySchool
                        : EducationLevel.PrimarySchool,
                ReviewLockedById = q.ReviewLockedById,
                ReviewLockedAt = q.ReviewLockedAt,
                RowVersion = q.Document.RowVersion,
            })
            .FirstOrDefaultAsync();

        if (workspace is not null && workspace.ReviewLockedAt is { } lockedAt)
        {
            workspace.LockExpiresAtUtc = lockedAt + LockTimeout;
        }

        return workspace;
    }

    public async Task<ReviewProgressDataModel> GetGlobalReviewProgressAsync(int? planId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        IQueryable<Document> query = db.Documents.AsNoTracking();

        if (planId.HasValue)
        {
            query = db.Set<Letter>()
                .AsNoTracking()
                .Where(l => l.PlanId == planId.Value);
        }

        // Aggregate on the server so we only transfer one row per (type, status)
        // combination instead of materializing every document.
        var counts = await query
            .GroupBy(d => new { d.DocumentType, d.Status })
            .Select(g => new
            {
                g.Key.DocumentType,
                g.Key.Status,
                Count = g.Count(),
            })
            .ToListAsync();

        return new ReviewProgressDataModel
        {
            PendingLetters = counts
                .Where(c => c.DocumentType == DocumentType.Letter && c.Status == DocumentStatus.ReviewPending)
                .Sum(c => c.Count),
            PendingReportCards = counts
                .Where(c => c.DocumentType == DocumentType.ReportCard && c.Status == DocumentStatus.ReviewPending)
                .Sum(c => c.Count),
            PendingOther = counts
                .Where(c => c.DocumentType == DocumentType.Other && c.Status == DocumentStatus.ReviewPending)
                .Sum(c => c.Count),
            PendingImprovement = counts
                .Where(c => c.Status == DocumentStatus.DigitalImprovementPending)
                .Sum(c => c.Count),
            ProcessingImprovement = counts
                .Where(c => c.Status == DocumentStatus.DigitalImprovementOngoing)
                .Sum(c => c.Count),
            Processing = counts
                .Where(c => c.Status == DocumentStatus.ReviewOngoing)
                .Sum(c => c.Count),
        };
    }

    public async Task<LetterPlanProgressDataModel> GetLetterPlanProgressAsync(int planId, int? chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        // Aggregate on the server so we only transfer one row per status
        // instead of materializing every letter in the plan.
        var counts = await db.Set<Letter>()
            .AsNoTracking()
            .Where(l => l.PlanId == planId
                        && (!chapterId.HasValue || l.ChapterId == chapterId))
            .GroupBy(l => l.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
            })
            .ToListAsync();

        return new LetterPlanProgressDataModel
        {
            TotalLetters = counts.Sum(c => c.Count),
            ApprovedLetters = counts
                .Where(c => c.Status == DocumentStatus.Approved)
                .Sum(c => c.Count),
            PendingLetters = counts
                .Where(c => c.Status is DocumentStatus.ReviewPending
                    or DocumentStatus.DigitalImprovementPending
                    or DocumentStatus.DigitalImprovementOngoing
                    or DocumentStatus.ReviewOngoing
                    or DocumentStatus.PendingManagerDecision)
                .Sum(c => c.Count),
            RejectedLetters = counts
                .Where(c => c.Status == DocumentStatus.Rejected)
                .Sum(c => c.Count),
        };
    }

    public async Task<List<DocumentShareNotificationDataModel>> GetUnnotifiedSharesAsync(long documentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.DocumentShares
            .AsNoTracking()
            .Where(s => s.DocumentId == documentId && s.NotificationSentOn == null)
            .Select(s => new DocumentShareNotificationDataModel
            {
                DocumentShareId = s.DocumentShareId,
                IsCompany = s.CompanyId != null,
                RecipientEmail = s.CompanyId != null
                    ? (s.Company!.Email ?? string.Empty)
                    : s.Sponsor!.Email,
                RecipientName = s.CompanyId != null ? s.Company!.Name : s.Sponsor!.FirstName,
                RecipientNickName = s.CompanyId != null ? null : s.Sponsor!.NickName,
                PublicAccessToken = s.CompanyId != null
                    ? s.Company!.PublicAccessToken
                    : s.Sponsor!.PublicAccessToken,
                StudentId = s.StudentId,
                StudentFirstName = s.Student.FirstName,
                StudentLastName = s.Student.LastName,
                StudentNickName = s.Student.NickName,
                StudentGender = s.Student.Gender,
                AdditionalRecipients = s.Sponsor == null
                    ? new List<SendAlsoToNotificationDataModel>()
                    : s.Sponsor.SendAlsoTos
                        .Select(r => new SendAlsoToNotificationDataModel
                        {
                            RecipientName = r.RecipientName,
                            RecipientEmail = r.RecipientEmail,
                            SendAsBcc = r.SendAsBcc,
                        })
                        .ToList(),
            })
            .ToListAsync();
    }

    public async Task<List<long>> GetDocumentIdsWithUnnotifiedSharesAsync()
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.DocumentShares
            .AsNoTracking()
            .Where(s => s.NotificationSentOn == null)
            .Select(s => s.DocumentId)
            .Distinct()
            .ToListAsync();
    }

    public async Task MarkShareNotifiedAsync(long documentShareId, DateTime notifiedOn)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var share = await db.DocumentShares.FindAsync(documentShareId);
        if (share is not null)
        {
            share.NotificationSentOn = notifiedOn;
            await db.SaveChangesAsync();
        }
    }

    public async Task<Document?> GetDocumentByIdAsync(long documentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await db.Documents
            .AsNoTracking()
            .Include(d => d.Student)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);
    }

    public async Task<DocumentBlobContextDataModel?> GetDocumentBlobContextAsync(long documentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var document = await db.Documents
            .AsNoTracking()
            .Include(d => d.Pages).ThenInclude(p => p.OriginalBlobPath)
            .Include(d => d.Pages).ThenInclude(p => p.ImprovedBlobPath)
            .Include(d => d.QueueItem)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document is null)
        {
            return null;
        }

        var pages = document.Pages
            .OrderBy(p => p.PageNumber)
            .Select(p => new DocumentPageBlobDataModel
            {
                DocumentPageId = p.DocumentPageId,
                PageNumber = p.PageNumber,
                Original = ToBlobPathDataModel(p.OriginalBlobPath)!,
                Active = ToBlobPathDataModel(p.ImprovedBlobPath ?? p.OriginalBlobPath)!,
            })
            .ToList();

        return new DocumentBlobContextDataModel
        {
            DocumentId = document.DocumentId,
            DocumentType = document.DocumentType,
            ChapterId = document.ChapterId,
            StudentId = document.StudentId,
            SponsorId = document.SponsorId,
            CompanyId = (document as Letter)?.CompanyId,
            PlanId = (document as Letter)?.PlanId,
            UploadedById = document.UploadedById,
            DigitalImprovementStatus = document.DigitalImprovementStatus,
            ImprovementLockedById = document.ImprovementLockedById,
            ReviewLockedById = document.QueueItem?.ReviewLockedById,
            Pages = pages,
        };
    }

    public async Task<DocumentBlobContextDataModel?> GetFacilitatorOtherDocumentBlobContextAsync(
        int facilitatorId, int studentId, long documentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var isOwned = await db.Set<OtherDocument>()
            .AsNoTracking()
            .AnyAsync(d => d.DocumentId == documentId
                           && d.StudentId == studentId
                           && d.Student.FacilitatorId == facilitatorId
                           && d.Student.IsActive
                           && !d.Student.IsDeleted);

        if (!isOwned)
        {
            return null;
        }

        return await GetDocumentBlobContextAsync(documentId);
    }

    public async Task<StudentDocumentHistoryDataModel?> GetStudentDocumentHistoryAsync(int chapterId, int studentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var studentName = await db.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId
                        && s.ChapterId == chapterId
                        && !s.IsDeleted)
            .Select(s => s.FirstName + " " + s.LastName)
            .FirstOrDefaultAsync();

        if (studentName is null)
        {
            return null;
        }

        var letters = await db.Set<Letter>()
            .AsNoTracking()
            .Include(d => d.Company)
            .Include(d => d.Sponsor)
            .Include(d => d.RejectedReason)
            .Include(d => d.RejectionOverriddenBy)
            .Include(d => d.Pages)
            .Where(d => d.StudentId == studentId)
            .ToListAsync();

        var reportCards = await db.Set<ReportCard>()
            .AsNoTracking()
            .Include(d => d.RejectedReason)
            .Include(d => d.RejectionOverriddenBy)
            .Include(d => d.Pages)
            .Where(d => d.StudentId == studentId)
            .ToListAsync();

        var others = await db.Set<OtherDocument>()
            .AsNoTracking()
            .Include(d => d.RejectedReason)
            .Include(d => d.RejectionOverriddenBy)
            .Include(d => d.Pages)
            .Where(d => d.StudentId == studentId)
            .ToListAsync();

        var items = letters
            .Select(d => ToHistoryItem(
                d,
                description: null,
                reportCardPeriod: null,
                recipientName: d.Company?.Name
                    ?? (d.Sponsor is null ? null : d.Sponsor.FirstName + " " + d.Sponsor.LastName)))
            .Concat(reportCards.Select(d => ToHistoryItem(
                d,
                d.Description,
                d.Period,
                recipientName: null)))
            .Concat(others.Select(d => ToHistoryItem(
                d,
                d.Description,
                reportCardPeriod: null,
                recipientName: null)))
            .OrderByDescending(d => d.UploadedOn)
            .ThenByDescending(d => d.DocumentId)
            .ToList();

        return new StudentDocumentHistoryDataModel
        {
            StudentId = studentId,
            StudentName = studentName,
            Items = items,
        };
    }

    public async Task<DocumentBlobContextDataModel?> GetManagerStudentDocumentBlobContextAsync(
        int chapterId, int studentId, long documentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var studentExists = await db.Students
            .AsNoTracking()
            .AnyAsync(s => s.Id == studentId
                           && s.ChapterId == chapterId
                           && !s.IsDeleted);

        if (!studentExists)
        {
            return null;
        }

        var context = await GetDocumentBlobContextAsync(documentId);
        return context is null || context.StudentId != studentId
            ? null
            : context;
    }

    private static StudentDocumentHistoryItemDataModel ToHistoryItem(
        Document document,
        string? description,
        DateOnly? reportCardPeriod,
        string? recipientName) =>
        new()
        {
            DocumentId = document.DocumentId,
            DocumentType = document.DocumentType,
            UploadedOn = document.UploadedOn,
            Description = description,
            ReportCardPeriod = reportCardPeriod,
            RecipientName = recipientName,
            FileKind = document.FileKind,
            Status = document.Status,
            RejectionReason = FormatRejectionReason(document.RejectedReason, document.RejectionNotes),
            RejectionOverriddenOn = document.RejectionOverriddenOn,
            RejectionOverriddenByName = document.RejectionOverriddenBy is null
                ? null
                : document.RejectionOverriddenBy.FirstName + " " + document.RejectionOverriddenBy.LastName,
            TextContent = document.TextContent,
            YouTubeVideoId = document.YouTubeVideoId,
            PageCount = document.Pages.Count,
        };

    private static BlobPathDataModel? ToBlobPathDataModel(BlobPath? blobPath) =>
        blobPath is null
            ? null
            : new BlobPathDataModel
            {
                StoragePath = blobPath.StoragePath,
                MimeType = blobPath.MimeType,
                FileSizeBytes = blobPath.FileSizeBytes,
                Sha256 = blobPath.Sha256,
            };

    public async Task<List<int>> GetActiveSponsorIdsForStudentAsync(int studentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await GetActiveSponsorIdsInternalAsync(db, studentId);
    }

    public async Task<List<DocumentDescriptionOptionDataModel>> GetDescriptionOptionsAsync(
        int chapterId, DocumentType documentType)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await db.DocumentDescriptionOptions
            .AsNoTracking()
            .Where(o => o.DocumentType == documentType
                        && o.IsActive
                        && (o.ChapterId == null || o.ChapterId == chapterId))
            .OrderBy(o => o.SortOrder)
            .ThenBy(o => o.Text)
            .Select(o => new DocumentDescriptionOptionDataModel
            {
                DocumentDescriptionOptionId = o.DocumentDescriptionOptionId,
                Text = o.Text,
                SortOrder = o.SortOrder,
            })
            .ToListAsync();
    }

    public async Task<List<RejectedReasonDataModel>> GetApplicableRejectedReasonsAsync(DocumentType documentType)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await db.RejectedReasons
            .AsNoTracking()
            .Where(r => r.AppliesToDocumentType == documentType || r.AppliesToDocumentType == null)
            .OrderBy(r => r.AppliesToDocumentType == null ? 1 : 0)
            .ThenBy(r => r.Id)
            .Select(r => new RejectedReasonDataModel
            {
                RejectedReasonId = r.Id,
                Code = r.Code,
                Description = r.Description,
                RequiresNotes = r.RequiresNotes,
            })
            .ToListAsync();
    }

    private static async Task<List<int>> GetActiveSponsorIdsInternalAsync(FonbecWebDbContext db, int studentId)
    {
        var targets = await ResolveStudentShareTargetsAsync(db, studentId);
        return targets.SponsorIds.ToList();
    }

    /// <summary>
    /// The set of recipients a document should be shared with for the student's active sponsorships.
    /// Each sponsorship recipient is a person-sponsor or a company; a company additionally fans out
    /// to the person-sponsors linked to it.
    /// </summary>
    private static async Task<ShareTargets> ResolveStudentShareTargetsAsync(FonbecWebDbContext db, int studentId)
    {
        var utcNow = DateTime.UtcNow;
        var sponsorships = await db.Sponsorships
            .AsNoTracking()
            .Where(sp => sp.StudentId == studentId
                         && sp.IsActive
                         && sp.StartDate <= utcNow
                         && (sp.EndDate == null || sp.EndDate >= utcNow))
            .Select(sp => new
            {
                sp.SponsorId,
                SponsorActive = sp.Sponsor != null && sp.Sponsor.IsActive && !sp.Sponsor.IsDeleted,
                sp.CompanyId,
                CompanyActive = sp.Company != null && sp.Company.IsActive,
            })
            .ToListAsync();

        var directSponsorIds = sponsorships
            .Where(x => x.SponsorId != null && x.SponsorActive)
            .Select(x => x.SponsorId!.Value);

        var companyIds = sponsorships
            .Where(x => x.CompanyId != null && x.CompanyActive)
            .Select(x => x.CompanyId!.Value);

        return await ResolveShareTargetsAsync(db, directSponsorIds, companyIds);
    }

    /// <summary>
    /// Expands an explicit set of recipient person-sponsors and companies into concrete share
    /// targets. Every company additionally fans out to its active linked person-sponsors.
    /// </summary>
    private static async Task<ShareTargets> ResolveShareTargetsAsync(
        FonbecWebDbContext db,
        IEnumerable<int> directSponsorIds,
        IEnumerable<int> companyIds)
    {
        var sponsorIds = new HashSet<int>(directSponsorIds);
        var companies = new HashSet<int>(companyIds);

        if (companies.Count > 0)
        {
            var linkedSponsorIds = await db.Sponsors
                .AsNoTracking()
                .Where(s => s.CompanyId != null
                            && companies.Contains(s.CompanyId.Value)
                            && s.IsActive
                            && !s.IsDeleted)
                .Select(s => s.Id)
                .ToListAsync();

            foreach (var id in linkedSponsorIds)
            {
                sponsorIds.Add(id);
            }
        }

        return new ShareTargets(sponsorIds, companies);
    }

    private sealed record ShareTargets(IReadOnlyCollection<int> SponsorIds, IReadOnlyCollection<int> CompanyIds);

    private static async Task<List<string>> FinalizeApprovalAsync(
        FonbecWebDbContext db,
        Document document,
        int reviewerId,
        ShareTargets targets)
    {
        var utcNow = DateTime.UtcNow;
        document.Status = DocumentStatus.Approved;
        document.ApprovedOn = utcNow;
        document.DecisionReviewLockedOn = document.QueueItem?.ReviewLockedAt;

        if (document.QueueItem is not null)
        {
            document.QueueItem.ReviewLockedById = null;
            document.QueueItem.ReviewLockedAt = null;
        }

        AddDocumentShares(db, document, reviewerId, targets, utcNow);

        try
        {
            await db.SaveChangesAsync();
            return [];
        }
        catch (DbUpdateConcurrencyException)
        {
            return [DocumentMessages.ConcurrencyConflict];
        }
    }

    private static void AddDocumentShares(
        FonbecWebDbContext db,
        Document document,
        int sharedById,
        ShareTargets targets,
        DateTime utcNow)
    {
        foreach (var sponsorId in targets.SponsorIds)
        {
            db.DocumentShares.Add(new DocumentShare
            {
                DocumentId = document.DocumentId,
                SponsorId = sponsorId,
                StudentId = document.StudentId,
                SharedOn = utcNow,
                SharedById = sharedById,
            });
        }

        foreach (var companyId in targets.CompanyIds)
        {
            db.DocumentShares.Add(new DocumentShare
            {
                DocumentId = document.DocumentId,
                CompanyId = companyId,
                StudentId = document.StudentId,
                SharedOn = utcNow,
                SharedById = sharedById,
            });
        }
    }

    private async Task<List<string>> RejectDocumentAsync(
        long documentId,
        int reviewerId,
        byte[] rowVersion,
        int? rejectedReasonId,
        string? rejectionNotes,
        DocumentType expectedType)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var document = await db.Documents
            .Include(d => d.QueueItem)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId
                                      && d.Status == DocumentStatus.ReviewOngoing
                                      && d.QueueItem!.ReviewLockedById == reviewerId);

        if (document is null)
        {
            return [DocumentMessages.DocumentNotFoundOrNotLockedForReview];
        }

        if (document.DocumentType != expectedType)
        {
            return [DocumentMessages.DocumentTypeMismatch];
        }

        db.Entry(document).Property(d => d.RowVersion).OriginalValue = rowVersion;

        var holdForManager = await db.Chapters
            .Where(c => c.Id == document.ChapterId)
            .Select(c => c.DocumentRejectionBehavior)
            .FirstOrDefaultAsync() == DocumentRejectionBehavior.HoldForManager;

        document.Status = holdForManager
            ? DocumentStatus.PendingManagerDecision
            : DocumentStatus.Rejected;
        document.RejectedOn = DateTime.UtcNow;
        document.RejectedById = reviewerId;
        document.RejectedReasonId = rejectedReasonId;
        document.RejectionNotes = rejectionNotes;
        document.DecisionReviewLockedOn = document.QueueItem?.ReviewLockedAt;

        if (document.QueueItem is not null)
        {
            document.QueueItem.ReviewLockedById = null;
            document.QueueItem.ReviewLockedAt = null;
        }

        try
        {
            await db.SaveChangesAsync();
            return [];
        }
        catch (DbUpdateConcurrencyException)
        {
            return [DocumentMessages.ConcurrencyConflict];
        }
    }

    public async Task<DocumentRejectionBehavior?> GetChapterRejectionBehaviorAsync(int chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var chapter = await db.Chapters
            .AsNoTracking()
            .Where(c => c.Id == chapterId)
            .Select(c => (DocumentRejectionBehavior?)c.DocumentRejectionBehavior)
            .FirstOrDefaultAsync();
        return chapter;
    }

    public async Task<bool> SetChapterRejectionBehaviorAsync(
        int chapterId, DocumentRejectionBehavior behavior, int updatedById)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var chapter = await db.Chapters.FirstOrDefaultAsync(c => c.Id == chapterId && c.IsActive);
        if (chapter is null)
        {
            return false;
        }

        chapter.DocumentRejectionBehavior = behavior;
        chapter.LastUpdatedById = updatedById;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<HeldRejectionDataModel>> ListHeldRejectionsAsync(int chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var documents = await db.Documents
            .AsNoTracking()
            .Include(d => d.Student)
            .ThenInclude(s => s.Facilitator)
            .Include(d => d.Sponsor)
            .Include(d => d.RejectedReason)
            .Include(d => d.RejectedBy)
            .Where(d => d.ChapterId == chapterId && d.Status == DocumentStatus.PendingManagerDecision)
            .OrderBy(d => d.RejectedOn)
            .ThenBy(d => d.DocumentId)
            .ToListAsync();

        var letterIds = documents.OfType<Letter>().Select(l => l.DocumentId).ToList();
        var companyNames = letterIds.Count == 0
            ? new Dictionary<long, string>()
            : await db.Set<Letter>()
                .AsNoTracking()
                .Where(l => letterIds.Contains(l.DocumentId) && l.CompanyId != null)
                .Select(l => new { l.DocumentId, Name = l.Company!.Name })
                .ToDictionaryAsync(l => l.DocumentId, l => l.Name);

        var documentIds = documents.Select(d => d.DocumentId).ToList();
        var pagesByDocument = await LoadHeldRejectionPagesAsync(db, documentIds);

        var studentIds = documents.Where(d => d is not Letter).Select(d => d.StudentId).Distinct().ToList();
        var sponsorsByStudent = await LoadActiveSponsorLabelsAsync(db, studentIds);

        return documents.Select(document => new HeldRejectionDataModel
        {
            DocumentId = document.DocumentId,
            StudentId = document.StudentId,
            StudentName = document.Student.FirstName + " " + document.Student.LastName,
            DocumentType = document.DocumentType,
            Detail = HeldRejectionDetail(document, companyNames),
            RejectionReason = FormatRejectionReason(document.RejectedReason, document.RejectionNotes),
            RejectedOn = document.RejectedOn,
            RejectedByName = document.RejectedBy is null
                ? null
                : document.RejectedBy.FirstName + " " + document.RejectedBy.LastName,
            FacilitatorName = document.Student.Facilitator.FirstName + " " + document.Student.Facilitator.LastName,
            Sponsors = document is Letter
                ? LetterSponsorLabels(document, companyNames)
                : sponsorsByStudent.GetValueOrDefault(document.StudentId) ?? [],
            FileKind = document.FileKind,
            Pages = pagesByDocument.GetValueOrDefault(document.DocumentId) ?? [],
            PageCount = pagesByDocument.GetValueOrDefault(document.DocumentId)?.Count ?? 0,
            TextContent = document.FileKind == FileKind.Text ? document.TextContent : null,
            YouTubeVideoId = document.FileKind == FileKind.YouTube ? document.YouTubeVideoId : null,
            RowVersion = document.RowVersion,
        }).ToList();
    }

    public async Task<int> CountHeldRejectionsAsync(int chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await db.Documents
            .AsNoTracking()
            .CountAsync(d => d.ChapterId == chapterId && d.Status == DocumentStatus.PendingManagerDecision);
    }

    public async Task<List<string>> ShareHeldRejectionAsync(
        long documentId, int managerId, int chapterId, byte[] rowVersion)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var document = await db.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId
                                      && d.Status == DocumentStatus.PendingManagerDecision
                                      && d.ChapterId == chapterId);

        if (document is null)
        {
            return [DocumentMessages.HeldRejectionNotFound];
        }

        db.Entry(document).Property(d => d.RowVersion).OriginalValue = rowVersion;

        ShareTargets targets = document is Letter letter
            ? await ResolveShareTargetsAsync(
                db,
                letter.SponsorId.HasValue ? [letter.SponsorId.Value] : [],
                letter.CompanyId.HasValue ? [letter.CompanyId.Value] : [])
            : await ResolveStudentShareTargetsAsync(db, document.StudentId);

        var utcNow = DateTime.UtcNow;
        document.Status = DocumentStatus.Approved;
        document.ApprovedOn = utcNow;
        document.RejectionOverriddenOn = utcNow;
        document.RejectionOverriddenById = managerId;
        AddDocumentShares(db, document, managerId, targets, utcNow);

        try
        {
            await db.SaveChangesAsync();
            return [];
        }
        catch (DbUpdateConcurrencyException)
        {
            return [DocumentMessages.ConcurrencyConflict];
        }
    }

    public async Task<List<string>> ReturnHeldRejectionAsync(long documentId, int chapterId, byte[] rowVersion)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var document = await db.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId
                                      && d.Status == DocumentStatus.PendingManagerDecision
                                      && d.ChapterId == chapterId);

        if (document is null)
        {
            return [DocumentMessages.HeldRejectionNotFound];
        }

        db.Entry(document).Property(d => d.RowVersion).OriginalValue = rowVersion;
        document.Status = DocumentStatus.Rejected;

        try
        {
            await db.SaveChangesAsync();
            return [];
        }
        catch (DbUpdateConcurrencyException)
        {
            return [DocumentMessages.ConcurrencyConflict];
        }
    }

    public async Task<FacilitatorRejectionNotificationDataModel?> GetFacilitatorRejectionNotificationAsync(
        long documentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Documents
            .AsNoTracking()
            .Where(d => d.DocumentId == documentId)
            .Select(d => new FacilitatorRejectionNotificationDataModel
            {
                Status = d.Status,
                FacilitatorRejectionNotifiedOn = d.FacilitatorRejectionNotifiedOn,
                FacilitatorEmail = d.Student.Facilitator.Email,
                FacilitatorFirstName = d.Student.Facilitator.FirstName,
                StudentFirstName = d.Student.FirstName,
                StudentLastName = d.Student.LastName,
                StudentNickName = d.Student.NickName,
                DocumentType = d.DocumentType,
                RejectionReasonDescription = d.RejectedReason != null ? d.RejectedReason.Description : null,
                RejectionNotes = d.RejectionNotes,
            })
            .SingleOrDefaultAsync();
    }

    public async Task<List<long>> GetDocumentIdsWithUnnotifiedFacilitatorRejectionsAsync()
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Documents
            .AsNoTracking()
            .Where(d => d.Status == DocumentStatus.Rejected && d.FacilitatorRejectionNotifiedOn == null)
            .Select(d => d.DocumentId)
            .ToListAsync();
    }

    public async Task MarkFacilitatorRejectionNotifiedAsync(long documentId, DateTime notifiedOn)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var document = await db.Documents.SingleOrDefaultAsync(d => d.DocumentId == documentId);
        if (document is null || document.FacilitatorRejectionNotifiedOn is not null)
        {
            return;
        }

        document.FacilitatorRejectionNotifiedOn = notifiedOn;
        await db.SaveChangesAsync();
    }

    public async Task<HeldRejectionManagerNotificationDataModel?> GetHeldRejectionManagerNotificationAsync(
        long documentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Documents
            .AsNoTracking()
            .Where(d => d.DocumentId == documentId)
            .Select(d => new HeldRejectionManagerNotificationDataModel
            {
                Status = d.Status,
                ManagersNotifiedOfHeldRejectionOn = d.ManagersNotifiedOfHeldRejectionOn,
                ChapterId = d.ChapterId,
                StudentFirstName = d.Student.FirstName,
                StudentLastName = d.Student.LastName,
                StudentNickName = d.Student.NickName,
                DocumentType = d.DocumentType,
                RejectionReasonDescription = d.RejectedReason != null ? d.RejectedReason.Description : null,
                RejectionNotes = d.RejectionNotes,
            })
            .SingleOrDefaultAsync();
    }

    public async Task<List<long>> GetDocumentIdsWithUnnotifiedHeldRejectionsAsync()
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Documents
            .AsNoTracking()
            .Where(d => d.Status == DocumentStatus.PendingManagerDecision
                        && d.ManagersNotifiedOfHeldRejectionOn == null)
            .Select(d => d.DocumentId)
            .ToListAsync();
    }

    public async Task MarkManagersNotifiedOfHeldRejectionAsync(long documentId, DateTime notifiedOn)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var document = await db.Documents.SingleOrDefaultAsync(d => d.DocumentId == documentId);
        if (document is null || document.ManagersNotifiedOfHeldRejectionOn is not null)
        {
            return;
        }

        document.ManagersNotifiedOfHeldRejectionOn = notifiedOn;
        await db.SaveChangesAsync();
    }

    private static async Task<Dictionary<long, List<ReviewWorkspacePageDataModel>>> LoadHeldRejectionPagesAsync(
        FonbecWebDbContext db, List<long> documentIds)
    {
        if (documentIds.Count == 0)
        {
            return [];
        }

        var pages = await db.Set<DocumentPage>()
            .AsNoTracking()
            .Where(page => documentIds.Contains(page.DocumentId))
            .OrderBy(page => page.PageNumber)
            .Select(page => new
            {
                page.DocumentId,
                page.PageNumber,
                MimeType = page.ImprovedBlobPath != null
                    ? page.ImprovedBlobPath.MimeType
                    : page.OriginalBlobPath.MimeType,
            })
            .ToListAsync();

        return pages
            .GroupBy(page => page.DocumentId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(page => new ReviewWorkspacePageDataModel
                {
                    PageNumber = page.PageNumber,
                    MimeType = page.MimeType,
                }).ToList());
    }

    private static List<HeldRejectionSponsorDataModel> LetterSponsorLabels(
        Document document, IReadOnlyDictionary<long, string> companyNames)
    {
        if (document is not Letter letter)
        {
            return [];
        }

        if (letter.CompanyId is not null && companyNames.TryGetValue(letter.DocumentId, out var companyName))
        {
            return [new HeldRejectionSponsorDataModel { Name = companyName, IsCompany = true }];
        }

        if (letter.Sponsor is null)
        {
            return [];
        }

        return
        [
            new HeldRejectionSponsorDataModel
            {
                Name = letter.Sponsor.FirstName + " " + letter.Sponsor.LastName,
                Gender = letter.Sponsor.Gender,
            },
        ];
    }

    private static async Task<Dictionary<int, List<HeldRejectionSponsorDataModel>>> LoadActiveSponsorLabelsAsync(
        FonbecWebDbContext db, List<int> studentIds)
    {
        if (studentIds.Count == 0)
        {
            return [];
        }

        var utcNow = DateTime.UtcNow;
        var sponsorships = await db.Sponsorships
            .AsNoTracking()
            .Where(sp => studentIds.Contains(sp.StudentId)
                         && sp.IsActive
                         && sp.StartDate <= utcNow
                         && (sp.EndDate == null || sp.EndDate >= utcNow))
            .Select(sp => new
            {
                sp.StudentId,
                sp.SponsorId,
                SponsorName = sp.Sponsor == null ? null : sp.Sponsor.FirstName + " " + sp.Sponsor.LastName,
                SponsorGender = sp.Sponsor == null ? (Gender?)null : sp.Sponsor.Gender,
                SponsorActive = sp.Sponsor != null && sp.Sponsor.IsActive && !sp.Sponsor.IsDeleted,
                sp.CompanyId,
                CompanyName = sp.Company == null ? null : sp.Company.Name,
                CompanyActive = sp.Company != null && sp.Company.IsActive,
            })
            .ToListAsync();

        return sponsorships
            .GroupBy(sp => sp.StudentId)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(sp =>
                {
                    var labels = new List<HeldRejectionSponsorDataModel>();
                    if (sp.SponsorId is not null && sp.SponsorActive && sp.SponsorName is not null)
                    {
                        labels.Add(new HeldRejectionSponsorDataModel
                        {
                            Name = sp.SponsorName,
                            Gender = sp.SponsorGender,
                        });
                    }

                    if (sp.CompanyId is not null && sp.CompanyActive && sp.CompanyName is not null)
                    {
                        labels.Add(new HeldRejectionSponsorDataModel
                        {
                            Name = sp.CompanyName,
                            IsCompany = true,
                        });
                    }

                    return labels;
                }).ToList());
    }

    private static string? FormatRejectionReason(RejectedReason? reason, string? notes) =>
        reason is null
            ? notes
            : string.IsNullOrEmpty(notes)
                ? reason.Description
                : reason.Description + ": " + notes;

    private static string? HeldRejectionDetail(Document document, IReadOnlyDictionary<long, string> companyNames)
    {
        if (document is Letter letter)
        {
            if (letter.CompanyId is not null && companyNames.TryGetValue(letter.DocumentId, out var companyName))
            {
                return companyName;
            }

            return letter.Sponsor is null
                ? null
                : letter.Sponsor.FirstName + " " + letter.Sponsor.LastName;
        }

        return document switch
        {
            ReportCard reportCard => reportCard.Description,
            OtherDocument other => other.Description,
            _ => null,
        };
    }

    private static bool IsEligibleForSelectedReview(DocumentQueueItem queueItem, DateTime lockExpiredBefore)
    {
        var improvementReady = queueItem.Document.DigitalImprovementStatus is DigitalImprovementStatus.NotApplicable
            or DigitalImprovementStatus.Complete;
        if (!improvementReady)
        {
            return false;
        }

        var unlockedPending = queueItem.ReviewLockedById is null
            && queueItem.Document.Status == DocumentStatus.ReviewPending;
        var expiredOngoing = queueItem.ReviewLockedById is not null
            && queueItem.Document.Status == DocumentStatus.ReviewOngoing
            && queueItem.ReviewLockedAt is not null
            && queueItem.ReviewLockedAt < lockExpiredBefore;
        return unlockedPending || expiredOngoing;
    }

    private static ManagerQueueEntryDataModel ToManagerQueueEntry(QueueEntryRow row, DateTime lockValidFrom)
    {
        var isLocked = row.LockedById is not null
            && row.LockedAt is not null
            && row.LockedAt >= lockValidFrom;

        return new ManagerQueueEntryDataModel
        {
            DocumentId = row.DocumentId,
            DocumentType = row.DocumentType,
            StudentName = row.StudentName,
            ChapterName = row.ChapterName,
            EnqueuedAt = row.EnqueuedAt,
            LockedById = isLocked ? row.LockedById : null,
            LockedByName = isLocked ? row.LockedByName : null,
            IsLocked = isLocked,
        };
    }

    private sealed class QueueEntryRow
    {
        public long DocumentId { get; init; }
        public DocumentType DocumentType { get; init; }
        public string StudentName { get; init; } = string.Empty;
        public string ChapterName { get; init; } = string.Empty;
        public DateTime EnqueuedAt { get; init; }
        public int? LockedById { get; init; }
        public string? LockedByName { get; init; }
        public DateTime? LockedAt { get; init; }
    }

    private static DocumentQueueItemDataModel MapQueueItem(DocumentQueueItem queueItem, Document? document = null)
    {
        document ??= queueItem.Document;
        return new DocumentQueueItemDataModel
        {
            QueueItemId = queueItem.QueueItemId,
            DocumentId = document.DocumentId,
            DocumentType = document.DocumentType,
            Status = document.Status,
            DigitalImprovementStatus = document.DigitalImprovementStatus,
            EnqueuedAt = queueItem.EnqueuedAt,
            RowVersion = document.RowVersion,
        };
    }
}