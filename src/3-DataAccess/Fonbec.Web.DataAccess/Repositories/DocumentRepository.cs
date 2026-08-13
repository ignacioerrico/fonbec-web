using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.DataModels.Documents.Input;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IDocumentRepository
{
    Task<StudentUploadContextDataModel?> GetStudentUploadContextAsync(int studentId);
    Task<bool> IsActivePlanAsync(int planId, int chapterId);
    Task<bool> HasActiveSponsorshipAsync(int studentId, int sponsorId);
    Task<bool> HasActiveCompanySponsorshipAsync(int studentId, int companyId);
    Task<bool> HasDuplicateLetterAsync(int studentId, int sponsorId, int planId);
    Task<bool> HasDuplicateCompanyLetterAsync(int studentId, int companyId, int planId);
    Task<CreateDocumentResultDataModel> CreateLetterAsync(CreateLetterInputDataModel input);
    Task<CreateDocumentResultDataModel> CreateReportCardAsync(CreateReportCardInputDataModel input);
    Task<CreateDocumentResultDataModel> CreateOtherDocumentAsync(CreateOtherDocumentInputDataModel input);
    Task<DocumentQueueItemDataModel?> TakeNextForReviewAsync(int userId);

    /// <summary>Document id the user currently holds a valid (non-expired) review lock on, or <c>null</c>.</summary>
    Task<long?> GetActiveReviewLockedDocumentIdAsync(int userId);

    /// <summary>Releases every review lock whose timeout has elapsed, returning those documents to the queue.</summary>
    Task ReleaseExpiredReviewLocksAsync();

    Task ReleaseReviewLockAsync(long documentId, int userId);
    Task<DocumentQueueItemDataModel?> TakeNextForDigitalImprovementAsync(int userId);
    Task<List<string>> SubmitDigitalImprovementAsync(SubmitDigitalImprovementInputDataModel input);
    Task ReleaseImprovementLockAsync(long documentId, int userId);
    Task<List<string>> ApproveLetterAsync(ApproveLetterInputDataModel input);
    Task<List<string>> RejectLetterAsync(RejectLetterInputDataModel input);
    Task<List<string>> ApproveReportCardAsync(ApproveReportCardInputDataModel input);
    Task<List<string>> RejectReportCardAsync(RejectReportCardInputDataModel input);
    Task<List<string>> ApproveOtherDocumentAsync(ApproveOtherDocumentInputDataModel input);
    Task<List<string>> RejectOtherDocumentAsync(RejectOtherDocumentInputDataModel input);
    Task<SponsorDocumentHistoryDataModel> GetSharedDocumentsAsync(Guid sponsorPublicAccessToken, int studentId);
    Task<SponsorDocumentHistoryDataModel> GetSharedDocumentsForCompanyAsync(Guid companyPublicAccessToken, int studentId);
    Task<ReviewWorkspaceDataModel?> GetReviewWorkspaceAsync(long documentId);
    Task<ReviewProgressDataModel> GetGlobalReviewProgressAsync(int? planId);
    Task<LetterPlanProgressDataModel> GetLetterPlanProgressAsync(int planId, int? chapterId);
    Task<List<DocumentShareNotificationDataModel>> GetUnnotifiedSharesAsync(long documentId);
    Task MarkShareNotifiedAsync(long documentShareId, DateTime notifiedOn);
    Task<Document?> GetDocumentByIdAsync(long documentId);
    Task<DocumentBlobContextDataModel?> GetDocumentBlobContextAsync(long documentId);
    Task<List<int>> GetActiveSponsorIdsForStudentAsync(int studentId);
    Task<List<DocumentDescriptionOptionDataModel>> GetDescriptionOptionsAsync(int chapterId, DocumentType documentType);
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
        return await db.PlannedDeliveries
            .AsNoTracking()
            .AnyAsync(p => p.Id == planId
                           && p.IsActive
                           && !p.Completed
                           && (p.ChapterId == null || p.ChapterId == chapterId));
    }

    public async Task<bool> HasActiveSponsorshipAsync(int studentId, int sponsorId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var utcNow = DateTime.UtcNow;
        return await db.Sponsorships
            .AsNoTracking()
            .AnyAsync(sp => sp.StudentId == studentId
                            && sp.SponsorId == sponsorId
                            && sp.IsActive
                            && sp.StartDate <= utcNow
                            && (sp.EndDate == null || sp.EndDate >= utcNow)
                            && sp.Sponsor != null
                            && sp.Sponsor.IsActive
                            && !sp.Sponsor.IsDeleted);
    }

    public async Task<bool> HasActiveCompanySponsorshipAsync(int studentId, int companyId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var utcNow = DateTime.UtcNow;
        return await db.Sponsorships
            .AsNoTracking()
            .AnyAsync(sp => sp.StudentId == studentId
                            && sp.CompanyId == companyId
                            && sp.IsActive
                            && sp.StartDate <= utcNow
                            && (sp.EndDate == null || sp.EndDate >= utcNow)
                            && sp.Company != null
                            && sp.Company.IsActive);
    }

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
                ? DocumentStatus.PendingImprovement
                : DocumentStatus.Pending,
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
                ? DocumentStatus.PendingImprovement
                : DocumentStatus.Pending,
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
                ? DocumentStatus.PendingImprovement
                : DocumentStatus.Pending,
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

        // Lock the first review-eligible document whose lock is free: either never locked
        // (Status Pending) or taken but abandoned past the timeout (Status Processing with a
        // stale ReviewLockedAt). Ordering is Priority then EnqueuedAt, so an expired lock is
        // re-taken ahead of later documents that are still validly locked. The document's
        // RowVersion arbitrates concurrent takes; the loser retries and picks the next free one.
        while (true)
        {
            await using var db = await dbContext.CreateDbContextAsync();

            var utcNow = timeProvider.GetUtcNow().UtcDateTime;
            var lockExpiredBefore = utcNow - LockTimeout;

            var queueItem = await db.DocumentQueueItems
                .Include(q => q.Document)
                .Where(q => (q.Document.DigitalImprovementStatus == DigitalImprovementStatus.NotApplicable
                             || q.Document.DigitalImprovementStatus == DigitalImprovementStatus.Complete)
                            && ((q.ReviewLockedById == null && q.Document.Status == DocumentStatus.Pending)
                                || (q.ReviewLockedById != null
                                    && q.Document.Status == DocumentStatus.Processing
                                    && q.ReviewLockedAt != null
                                    && q.ReviewLockedAt < lockExpiredBefore)))
                .OrderBy(q => q.Priority)
                .ThenBy(q => q.EnqueuedAt)
                .FirstOrDefaultAsync();

            if (queueItem is null)
            {
                return null;
            }

            var isExpiredRetake = queueItem.Document.Status == DocumentStatus.Processing;

            queueItem.ReviewLockedById = userId;
            queueItem.ReviewLockedAt = utcNow;
            queueItem.DequeueCount++;

            if (isExpiredRetake)
            {
                // Status is already Processing, so nothing on the document changes; force a
                // guarded update so its RowVersion still arbitrates concurrent re-takes.
                db.Entry(queueItem.Document).State = EntityState.Modified;
            }
            else
            {
                queueItem.Document.Status = DocumentStatus.Processing;
            }

            try
            {
                await db.SaveChangesAsync();
                return MapQueueItem(queueItem);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another reviewer took this document first; loop and pick the next free one.
            }
        }
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
    /// document to <see cref="DocumentStatus.Pending"/>. Best-effort — a concurrent take-next that
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
            if (queueItem.Document.Status == DocumentStatus.Processing)
            {
                queueItem.Document.Status = DocumentStatus.Pending;
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
        if (queueItem.Document.Status == DocumentStatus.Processing)
        {
            queueItem.Document.Status = DocumentStatus.Pending;
        }

        await db.SaveChangesAsync();
    }

    public async Task<DocumentQueueItemDataModel?> TakeNextForDigitalImprovementAsync(int userId)
    {
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
            document.DigitalImprovementStatus = DigitalImprovementStatus.InProgress;
            document.Status = DocumentStatus.ProcessingImprovement;

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

        // Improvement is submitted for the whole document: exactly one improved file per page,
        // provided in page order.
        var pages = document.Pages.OrderBy(p => p.PageNumber).ToList();
        if (input.ImprovedBlobs.Count != pages.Count)
        {
            return [DocumentMessages.ImprovedPageCountMismatch];
        }

        db.Entry(document).Property(d => d.RowVersion).OriginalValue = input.RowVersion;

        for (var i = 0; i < pages.Count; i++)
        {
            var source = input.ImprovedBlobs[i];
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

        document.DigitalImprovementStatus = DigitalImprovementStatus.Complete;
        document.Status = DocumentStatus.Pending;
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
        document.Status = DocumentStatus.PendingImprovement;

        await db.SaveChangesAsync();
    }

    public async Task<List<string>> ApproveLetterAsync(ApproveLetterInputDataModel input)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var letter = await db.Set<Letter>()
            .Include(l => l.QueueItem)
            .FirstOrDefaultAsync(l => l.DocumentId == input.DocumentId
                                      && l.Status == DocumentStatus.Processing
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
                                      && r.Status == DocumentStatus.Processing
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
            ConfirmedStudentNameCorrect = input.ConfirmedStudentNameCorrect,
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
                                      && o.Status == DocumentStatus.Processing
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

    public async Task<SponsorDocumentHistoryDataModel> GetSharedDocumentsAsync(Guid sponsorPublicAccessToken, int studentId)
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

        var documents = await db.DocumentShares
            .AsNoTracking()
            .Where(s => s.SponsorId == sponsor.Id && s.StudentId == studentId)
            .OrderByDescending(s => s.SharedOn)
            .Select(s => new SharedDocumentDataModel
            {
                DocumentId = s.DocumentId,
                DocumentType = s.Document.DocumentType,
                SharedOn = s.SharedOn,
                FileKind = s.Document.FileKind,
                PageCount = s.Document.Pages.Count,
            })
            .ToListAsync();

        return new SponsorDocumentHistoryDataModel
        {
            IsAuthorized = true,
            Documents = documents,
        };
    }

    public async Task<SponsorDocumentHistoryDataModel> GetSharedDocumentsForCompanyAsync(
        Guid companyPublicAccessToken, int studentId)
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

        var documents = await db.DocumentShares
            .AsNoTracking()
            .Where(s => s.CompanyId == company.Id && s.StudentId == studentId)
            .OrderByDescending(s => s.SharedOn)
            .Select(s => new SharedDocumentDataModel
            {
                DocumentId = s.DocumentId,
                DocumentType = s.Document.DocumentType,
                SharedOn = s.SharedOn,
                FileKind = s.Document.FileKind,
                PageCount = s.Document.Pages.Count,
            })
            .ToListAsync();

        return new SponsorDocumentHistoryDataModel
        {
            IsAuthorized = true,
            Documents = documents,
        };
    }

    public async Task<ReviewWorkspaceDataModel?> GetReviewWorkspaceAsync(long documentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

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
                UploaderNotes = q.Document.UploaderNotes,
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
                .Where(c => c.DocumentType == DocumentType.Letter && c.Status == DocumentStatus.Pending)
                .Sum(c => c.Count),
            PendingReportCards = counts
                .Where(c => c.DocumentType == DocumentType.ReportCard && c.Status == DocumentStatus.Pending)
                .Sum(c => c.Count),
            PendingOther = counts
                .Where(c => c.DocumentType == DocumentType.Other && c.Status == DocumentStatus.Pending)
                .Sum(c => c.Count),
            PendingImprovement = counts
                .Where(c => c.Status is DocumentStatus.PendingImprovement or DocumentStatus.ProcessingImprovement)
                .Sum(c => c.Count),
            Processing = counts
                .Where(c => c.Status == DocumentStatus.Processing)
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
                .Where(c => c.Status is DocumentStatus.Pending
                    or DocumentStatus.PendingImprovement
                    or DocumentStatus.ProcessingImprovement
                    or DocumentStatus.Processing)
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
            })
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

        if (document.QueueItem is not null)
        {
            document.QueueItem.ReviewLockedById = null;
            document.QueueItem.ReviewLockedAt = null;
        }

        foreach (var sponsorId in targets.SponsorIds)
        {
            db.DocumentShares.Add(new DocumentShare
            {
                DocumentId = document.DocumentId,
                SponsorId = sponsorId,
                StudentId = document.StudentId,
                SharedOn = utcNow,
                SharedById = reviewerId,
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
                SharedById = reviewerId,
            });
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
                                      && d.Status == DocumentStatus.Processing
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

        document.Status = DocumentStatus.Rejected;
        document.RejectedOn = DateTime.UtcNow;
        document.RejectedReasonId = rejectedReasonId;
        document.RejectionNotes = rejectionNotes;

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