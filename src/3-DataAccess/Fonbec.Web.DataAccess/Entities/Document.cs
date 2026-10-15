using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.Entities;

public abstract class Document
{
    public long DocumentId { get; set; }

    public DocumentType DocumentType { get; set; }

    public int ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int? SponsorId { get; set; }
    public Sponsor? Sponsor { get; set; }

    public FileKind FileKind { get; set; }

    /// <summary>
    /// Ordered file pages for a <see cref="FileKind.Blob"/> document. A PDF or single image is one page;
    /// a multi-image document has one page per image. Empty for Text/YouTube documents.
    /// </summary>
    public List<DocumentPage> Pages { get; set; } = [];

    public string? YouTubeVideoId { get; set; }

    public string? TextContent { get; set; }

    public string? UploaderNotes { get; set; }

    public DigitalImprovementStatus DigitalImprovementStatus { get; set; }

    public int? ImprovementLockedById { get; set; }
    public FonbecWebUser? ImprovementLockedBy { get; set; }

    public DateTime? ImprovementLockedAt { get; set; }

    /// <summary>First time someone locked this document for digital improvement. Not cleared when the lock is released.</summary>
    public DateTime? ImprovementFirstLockedOn { get; set; }

    /// <summary>Start of the improvement lock that ended in a submission.</summary>
    public DateTime? DecisionImprovementLockedOn { get; set; }

    /// <summary>When digital improvement was submitted and the document became ready for review.</summary>
    public DateTime? ImprovementCompletedOn { get; set; }

    public DateTime UploadedOn { get; set; }

    public int UploadedById { get; set; }
    public FonbecWebUser UploadedBy { get; set; } = null!;

    public DateTime? ApprovedOn { get; set; }

    /// <summary>First time a reviewer locked this document. Not cleared when the lock is released.</summary>
    public DateTime? ReviewFirstLockedOn { get; set; }

    /// <summary>Start of the review lock that ended in approval or rejection.</summary>
    public DateTime? DecisionReviewLockedOn { get; set; }

    public DateTime? RejectedOn { get; set; }

    public int? RejectedById { get; set; }
    public FonbecWebUser? RejectedBy { get; set; }

    public int? RejectedReasonId { get; set; }
    public RejectedReason? RejectedReason { get; set; }

    public string? RejectionNotes { get; set; }

    /// <summary>
    /// When the facilitator was emailed about this rejection, or when that email was skipped
    /// because the facilitator has no address. Null while a returned rejection still needs mail.
    /// </summary>
    public DateTime? FacilitatorRejectionNotifiedOn { get; set; }

    /// <summary>
    /// When the chapter managers were emailed that this rejection is waiting for their decision,
    /// or when that email was skipped because no manager has an address.
    /// </summary>
    public DateTime? ManagersNotifiedOfHeldRejectionOn { get; set; }

    /// <summary>When a manager shared this rejected document with the sponsor anyway.</summary>
    public DateTime? RejectionOverriddenOn { get; set; }

    public int? RejectionOverriddenById { get; set; }
    public FonbecWebUser? RejectionOverriddenBy { get; set; }

    public DocumentStatus Status { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public DocumentQueueItem? QueueItem { get; set; }

    public List<DocumentShare> Shares { get; set; } = [];
}