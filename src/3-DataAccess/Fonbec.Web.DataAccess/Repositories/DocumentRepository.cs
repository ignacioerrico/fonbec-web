using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.DataModels.Documents.Input;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IDocumentRepository
{
    Task<StudentUploadContextDataModel?> GetStudentUploadContextAsync(int studentId);
    Task<bool> IsActivePlanAsync(int planId, int chapterId);
    Task<bool> HasActiveSponsorshipAsync(int studentId, int sponsorId);
    Task<bool> HasDuplicateLetterAsync(int studentId, int sponsorId, int planId);
    Task<CreateDocumentResultDataModel> CreateLetterAsync(CreateLetterInputDataModel input);
    Task<CreateDocumentResultDataModel> CreateReportCardAsync(CreateReportCardInputDataModel input);
    Task<CreateDocumentResultDataModel> CreateOtherDocumentAsync(CreateOtherDocumentInputDataModel input);
    Task<DocumentQueueItemDataModel?> TakeNextForReviewAsync(int userId);
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
    Task<ReviewProgressDataModel> GetGlobalReviewProgressAsync(int? planId);
    Task<LetterPlanProgressDataModel> GetLetterPlanProgressAsync(int planId, int? chapterId);
    Task<List<DocumentShareNotificationDataModel>> GetUnnotifiedSharesAsync(long documentId);
    Task MarkShareNotifiedAsync(long documentShareId, DateTime notifiedOn);
    Task<Document?> GetDocumentByIdAsync(long documentId);
    Task<List<int>> GetActiveSponsorIdsForStudentAsync(int studentId);
}

public class DocumentRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : IDocumentRepository
{
    private static readonly string[] ImageMimeTypes = ["image/jpeg", "image/png"];

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

    public async Task<bool> HasDuplicateLetterAsync(int studentId, int sponsorId, int planId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await db.Set<Letter>()
            .AnyAsync(l => l.StudentId == studentId
                           && l.SponsorId == sponsorId
                           && l.PlanId == planId
                           && l.Status != DocumentStatus.Rejected);
    }

    public Task<CreateDocumentResultDataModel> CreateLetterAsync(CreateLetterInputDataModel input) =>
        CreateDocumentAsync(input, (blobPathId, requiresImprovement) => new Letter
        {
            ChapterId = 0, // set below
            StudentId = input.StudentId,
            SponsorId = input.SponsorId,
            PlanId = input.PlanId,
            FileKind = input.FileKind,
            BlobPathId = blobPathId,
            OriginalBlobPathId = input.FileKind == FileKind.Blob ? blobPathId : null,
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
        }, input);

    public Task<CreateDocumentResultDataModel> CreateReportCardAsync(CreateReportCardInputDataModel input) =>
        CreateDocumentAsync(input, (blobPathId, requiresImprovement) => new ReportCard
        {
            StudentId = input.StudentId,
            FileKind = input.FileKind,
            BlobPathId = blobPathId,
            OriginalBlobPathId = input.FileKind == FileKind.Blob ? blobPathId : null,
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
        }, input);

    public Task<CreateDocumentResultDataModel> CreateOtherDocumentAsync(CreateOtherDocumentInputDataModel input) =>
        CreateDocumentAsync(input, (blobPathId, requiresImprovement) => new OtherDocument
        {
            StudentId = input.StudentId,
            FileKind = input.FileKind,
            BlobPathId = blobPathId,
            OriginalBlobPathId = input.FileKind == FileKind.Blob ? blobPathId : null,
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
        }, input);

    private async Task<CreateDocumentResultDataModel> CreateDocumentAsync<TDocument>(
        CreateDocumentBaseInputDataModel input,
        Func<long?, bool, TDocument> createEntity,
        CreateDocumentBaseInputDataModel _)
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

        long? blobPathId = null;
        var requiresImprovement = false;

        if (input.FileKind == FileKind.Blob)
        {
            if (input.Blob is null)
            {
                return new CreateDocumentResultDataModel { Errors = [DocumentMessages.BlobContentRequired] };
            }

            var blob = new BlobPath
            {
                StoragePath = input.Blob.StoragePath,
                MimeType = input.Blob.MimeType,
                FileSizeBytes = input.Blob.FileSizeBytes,
                Sha256 = input.Blob.Sha256,
            };
            db.BlobPaths.Add(blob);
            await db.SaveChangesAsync();
            blobPathId = blob.BlobPathId;
            requiresImprovement = ImageMimeTypes.Contains(input.Blob.MimeType, StringComparer.OrdinalIgnoreCase);
        }

        var document = createEntity(blobPathId, requiresImprovement);
        document.ChapterId = student.ChapterId;
        document.RowVersion = new byte[8];

        db.Documents.Add(document);
        await db.SaveChangesAsync();

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
        await using var db = await dbContext.CreateDbContextAsync();

        var queueItem = await db.DocumentQueueItems
            .Include(q => q.Document)
            .Where(q => q.Document.Status == DocumentStatus.Pending
                        && (q.Document.DigitalImprovementStatus == DigitalImprovementStatus.NotApplicable
                            || q.Document.DigitalImprovementStatus == DigitalImprovementStatus.Complete)
                        && q.ReviewLockedById == null)
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.EnqueuedAt)
            .FirstOrDefaultAsync();

        if (queueItem is null)
        {
            return null;
        }

        var utcNow = DateTime.UtcNow;
        queueItem.ReviewLockedById = userId;
        queueItem.ReviewLockedAt = utcNow;
        queueItem.DequeueCount++;
        queueItem.Document.Status = DocumentStatus.Processing;

        await db.SaveChangesAsync();

        return MapQueueItem(queueItem);
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
        await using var db = await dbContext.CreateDbContextAsync();

        var document = await db.Documents
            .Include(d => d.QueueItem)
            .Where(d => d.DigitalImprovementStatus == DigitalImprovementStatus.Required
                        && d.ImprovementLockedById == null)
            .OrderBy(d => d.QueueItem!.EnqueuedAt)
            .FirstOrDefaultAsync();

        if (document?.QueueItem is null)
        {
            return null;
        }

        var utcNow = DateTime.UtcNow;
        document.ImprovementLockedById = userId;
        document.ImprovementLockedAt = utcNow;
        document.DigitalImprovementStatus = DigitalImprovementStatus.InProgress;
        document.Status = DocumentStatus.ProcessingImprovement;

        await db.SaveChangesAsync();

        return MapQueueItem(document.QueueItem, document);
    }

    public async Task<List<string>> SubmitDigitalImprovementAsync(SubmitDigitalImprovementInputDataModel input)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var document = await db.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == input.DocumentId
                                      && d.ImprovementLockedById == input.UserId);

        if (document is null)
        {
            return [DocumentMessages.DocumentNotFoundOrImprovementLockNotHeld];
        }

        db.Entry(document).Property(d => d.RowVersion).OriginalValue = input.RowVersion;

        var improvedBlob = new BlobPath
        {
            StoragePath = input.ImprovedBlob.StoragePath,
            MimeType = input.ImprovedBlob.MimeType,
            FileSizeBytes = input.ImprovedBlob.FileSizeBytes,
            Sha256 = input.ImprovedBlob.Sha256,
        };
        db.BlobPaths.Add(improvedBlob);
        await db.SaveChangesAsync();

        document.ImprovedBlobPathId = improvedBlob.BlobPathId;
        document.BlobPathId = improvedBlob.BlobPathId;
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

        return await FinalizeApprovalAsync(db, letter, input.ReviewerId, [letter.SponsorId!.Value]);
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

        var sponsorIds = await GetActiveSponsorIdsInternalAsync(db, reportCard.StudentId);
        return await FinalizeApprovalAsync(db, reportCard, input.ReviewerId, sponsorIds);
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

        var sponsorIds = await GetActiveSponsorIdsInternalAsync(db, other.StudentId);
        return await FinalizeApprovalAsync(db, other, input.ReviewerId, sponsorIds);
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
                            && sp.SponsorId == sponsor.Id
                            && sp.IsActive
                            && sp.StartDate <= utcNow
                            && (sp.EndDate == null || sp.EndDate >= utcNow));

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
            })
            .ToListAsync();

        return new SponsorDocumentHistoryDataModel
        {
            IsAuthorized = true,
            Documents = documents,
        };
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

        var documents = await query.ToListAsync();

        return new ReviewProgressDataModel
        {
            PendingLetters = documents.Count(d => d.DocumentType == DocumentType.Letter
                                                && d.Status == DocumentStatus.Pending),
            PendingReportCards = documents.Count(d => d.DocumentType == DocumentType.ReportCard
                                                      && d.Status == DocumentStatus.Pending),
            PendingOther = documents.Count(d => d.DocumentType == DocumentType.Other
                                                && d.Status == DocumentStatus.Pending),
            PendingImprovement = documents.Count(d => d.Status == DocumentStatus.PendingImprovement
                                                      || d.Status == DocumentStatus.ProcessingImprovement),
            Processing = documents.Count(d => d.Status == DocumentStatus.Processing),
        };
    }

    public async Task<LetterPlanProgressDataModel> GetLetterPlanProgressAsync(int planId, int? chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var letters = await db.Set<Letter>()
            .AsNoTracking()
            .Where(l => l.PlanId == planId
                        && (!chapterId.HasValue || l.ChapterId == chapterId))
            .ToListAsync();

        return new LetterPlanProgressDataModel
        {
            TotalLetters = letters.Count,
            ApprovedLetters = letters.Count(l => l.Status == DocumentStatus.Approved),
            PendingLetters = letters.Count(l => l.Status is DocumentStatus.Pending
                or DocumentStatus.PendingImprovement
                or DocumentStatus.ProcessingImprovement
                or DocumentStatus.Processing),
            RejectedLetters = letters.Count(l => l.Status == DocumentStatus.Rejected),
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
                SponsorEmail = s.Sponsor.Email,
                SponsorFirstName = s.Sponsor.FirstName,
                SponsorNickName = s.Sponsor.NickName,
                PublicAccessToken = s.Sponsor.PublicAccessToken,
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

    public async Task<List<int>> GetActiveSponsorIdsForStudentAsync(int studentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await GetActiveSponsorIdsInternalAsync(db, studentId);
    }

    private static async Task<List<int>> GetActiveSponsorIdsInternalAsync(FonbecWebDbContext db, int studentId)
    {
        var utcNow = DateTime.UtcNow;
        return await db.Sponsorships
            .AsNoTracking()
            .Where(sp => sp.StudentId == studentId
                         && sp.SponsorId != null
                         && sp.IsActive
                         && sp.StartDate <= utcNow
                         && (sp.EndDate == null || sp.EndDate >= utcNow)
                         && sp.Sponsor != null
                         && sp.Sponsor.IsActive
                         && !sp.Sponsor.IsDeleted)
            .Select(sp => sp.SponsorId!.Value)
            .Distinct()
            .ToListAsync();
    }

    private static async Task<List<string>> FinalizeApprovalAsync(
        FonbecWebDbContext db,
        Document document,
        int reviewerId,
        IReadOnlyList<int> sponsorIds)
    {
        var utcNow = DateTime.UtcNow;
        document.Status = DocumentStatus.Approved;
        document.ApprovedOn = utcNow;

        if (document.QueueItem is not null)
        {
            document.QueueItem.ReviewLockedById = null;
            document.QueueItem.ReviewLockedAt = null;
        }

        foreach (var sponsorId in sponsorIds)
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