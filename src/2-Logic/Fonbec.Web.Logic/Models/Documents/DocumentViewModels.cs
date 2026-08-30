using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Models.Documents;

public class DocumentQueueItemViewModel
{
    public long QueueItemId { get; init; }
    public long DocumentId { get; init; }
    public DocumentType DocumentType { get; init; }
    public DocumentStatus Status { get; init; }
    public DigitalImprovementStatus DigitalImprovementStatus { get; init; }
    public DateTime EnqueuedAt { get; init; }
    public byte[] RowVersion { get; init; } = null!;
}

public class SharedDocumentViewModel
{
    public long DocumentId { get; init; }
    public DocumentType DocumentType { get; init; }
    public DateTime SharedOn { get; init; }
    public FileKind FileKind { get; init; }
}

public class SponsorDocumentHistoryViewModel
{
    public bool IsAuthorized { get; init; }
    public List<SharedDocumentViewModel> Documents { get; init; } = [];
}

/// <summary>A single readable page of a blob-backed document under review.</summary>
public class ReviewWorkspacePageViewModel
{
    public int PageNumber { get; init; }

    /// <summary>MIME type of the active (improved when available) file, used to render it inline.</summary>
    public string MimeType { get; init; } = string.Empty;
}

public class ReviewWorkspaceViewModel
{
    public long DocumentId { get; init; }
    public DocumentType DocumentType { get; init; }
    public FileKind FileKind { get; init; }
    public string? TextContent { get; init; }
    public string? YouTubeVideoId { get; init; }

    /// <summary>Number of file pages (0 for Text/YouTube; 1 for a PDF/single image; N for a multi-image document).</summary>
    public int PageCount { get; init; }

    public List<ReviewWorkspacePageViewModel> Pages { get; init; } = [];

    /// <summary>Start of the planned delivery a letter belongs to; <c>null</c> for other document types.</summary>
    public DateTime? PlanStartsOn { get; init; }

    public string? UploaderNotes { get; init; }

    public int StudentId { get; init; }
    public int? SponsorId { get; init; }
    public int? CompanyId { get; init; }

    /// <summary>UTC instant the current review lock expires; the workspace countdown ticks down to this.</summary>
    public DateTime LockExpiresAtUtc { get; init; }

    public byte[] RowVersion { get; init; } = null!;
}

public class ReviewProgressViewModel
{
    public int PendingLetters { get; init; }
    public int PendingReportCards { get; init; }
    public int PendingOther { get; init; }
    public int PendingImprovement { get; init; }
    public int Processing { get; init; }
}

public class LetterPlanProgressViewModel
{
    public int TotalLetters { get; init; }
    public int ApprovedLetters { get; init; }
    public int PendingLetters { get; init; }
    public int RejectedLetters { get; init; }
}

public class DocumentDescriptionOptionViewModel
{
    public int DocumentDescriptionOptionId { get; init; }
    public string Text { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public class RejectedReasonViewModel
{
    public int RejectedReasonId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool RequiresNotes { get; init; }
}