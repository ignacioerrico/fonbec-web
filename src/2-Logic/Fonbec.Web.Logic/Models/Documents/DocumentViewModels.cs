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