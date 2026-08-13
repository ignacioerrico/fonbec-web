using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Documents;

public class DocumentQueueItemDataModel
{
    public long QueueItemId { get; init; }
    public long DocumentId { get; init; }
    public DocumentType DocumentType { get; init; }
    public DocumentStatus Status { get; init; }
    public DigitalImprovementStatus DigitalImprovementStatus { get; init; }
    public DateTime EnqueuedAt { get; init; }
    public byte[] RowVersion { get; init; } = null!;
}

public class SharedDocumentDataModel
{
    public long DocumentId { get; init; }
    public DocumentType DocumentType { get; init; }
    public DateTime SharedOn { get; init; }
    public FileKind FileKind { get; init; }

    /// <summary>Number of file pages (0 for Text/YouTube; 1 for a PDF/single image; N for a multi-image document).</summary>
    public int PageCount { get; init; }
}

public class SponsorDocumentHistoryDataModel
{
    public bool IsAuthorized { get; init; }
    public List<SharedDocumentDataModel> Documents { get; init; } = [];
}

public class ReviewWorkspaceDataModel
{
    public long DocumentId { get; init; }
    public DocumentType DocumentType { get; init; }
    public FileKind FileKind { get; init; }
    public string? TextContent { get; init; }
    public string? YouTubeVideoId { get; init; }

    /// <summary>Number of file pages (0 for Text/YouTube; 1 for a PDF/single image; N for a multi-image document).</summary>
    public int PageCount { get; init; }

    public string? UploaderNotes { get; init; }
    public int? ReviewLockedById { get; init; }
    public DateTime? ReviewLockedAt { get; init; }

    /// <summary>UTC instant the current review lock expires, or <c>null</c> when the item is not locked.</summary>
    public DateTime? LockExpiresAtUtc { get; set; }

    public byte[] RowVersion { get; init; } = null!;
}

public class ReviewProgressDataModel
{
    public int PendingLetters { get; init; }
    public int PendingReportCards { get; init; }
    public int PendingOther { get; init; }
    public int PendingImprovement { get; init; }
    public int Processing { get; init; }
}

public class LetterPlanProgressDataModel
{
    public int TotalLetters { get; init; }
    public int ApprovedLetters { get; init; }
    public int PendingLetters { get; init; }
    public int RejectedLetters { get; init; }
}

/// <summary>
/// A pending notification for a single document share. The recipient is either a person-sponsor
/// or a company (both are first-class sponsors and are notified the same way, each via its own
/// <see cref="PublicAccessToken"/>-based history page).
/// </summary>
public class DocumentShareNotificationDataModel
{
    public long DocumentShareId { get; init; }

    /// <summary>True when the recipient is a company; false for a person-sponsor.</summary>
    public bool IsCompany { get; init; }

    /// <summary>Recipient email address. May be empty for a company that opted out of an address.</summary>
    public string RecipientEmail { get; init; } = string.Empty;

    /// <summary>Sponsor first name or company name.</summary>
    public string RecipientName { get; init; } = string.Empty;

    /// <summary>Sponsor nickname; always null for a company.</summary>
    public string? RecipientNickName { get; init; }

    /// <summary>Token for the recipient's public document-history page.</summary>
    public Guid PublicAccessToken { get; init; }

    public int StudentId { get; init; }
    public string StudentFirstName { get; init; } = string.Empty;
    public string StudentLastName { get; init; } = string.Empty;
    public string? StudentNickName { get; init; }
    public Gender StudentGender { get; init; }
}

public class StudentUploadContextDataModel
{
    public int StudentId { get; init; }
    public int ChapterId { get; init; }
    public int FacilitatorId { get; init; }
    public bool IsActive { get; init; }
}

public class CreateDocumentResultDataModel
{
    public long DocumentId { get; init; }
    public List<string> Errors { get; init; } = [];
    public bool IsSuccess => Errors.Count == 0;
}

public class DocumentDescriptionOptionDataModel
{
    public int DocumentDescriptionOptionId { get; init; }
    public string Text { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public class BlobPathDataModel
{
    public string StoragePath { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public long? FileSizeBytes { get; init; }
    public byte[]? Sha256 { get; init; }
}

public class DocumentBlobContextDataModel
{
    public long DocumentId { get; init; }
    public DocumentType DocumentType { get; init; }
    public int ChapterId { get; init; }
    public int StudentId { get; init; }
    public int? SponsorId { get; init; }
    public int? CompanyId { get; init; }
    public int? PlanId { get; init; }
    public int UploadedById { get; init; }
    public DigitalImprovementStatus DigitalImprovementStatus { get; init; }
    public int? ImprovementLockedById { get; init; }
    public int? ReviewLockedById { get; init; }

    /// <summary>All file pages of the document, ordered by <see cref="DocumentPageBlobDataModel.PageNumber"/>.</summary>
    public List<DocumentPageBlobDataModel> Pages { get; init; } = [];
}

public class DocumentPageBlobDataModel
{
    public long DocumentPageId { get; init; }
    public int PageNumber { get; init; }

    /// <summary>The originally uploaded file for this page.</summary>
    public BlobPathDataModel Original { get; init; } = null!;

    /// <summary>The active file for this page (improved when available, otherwise the original).</summary>
    public BlobPathDataModel Active { get; init; } = null!;
}