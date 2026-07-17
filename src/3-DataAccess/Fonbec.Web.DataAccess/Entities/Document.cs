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

    public DateTime UploadedOn { get; set; }

    public int UploadedById { get; set; }
    public FonbecWebUser UploadedBy { get; set; } = null!;

    public DateTime? ApprovedOn { get; set; }

    public DateTime? RejectedOn { get; set; }

    public int? RejectedReasonId { get; set; }
    public RejectedReason? RejectedReason { get; set; }

    public string? RejectionNotes { get; set; }

    public DocumentStatus Status { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public DocumentQueueItem? QueueItem { get; set; }

    public List<DocumentShare> Shares { get; set; } = [];
}