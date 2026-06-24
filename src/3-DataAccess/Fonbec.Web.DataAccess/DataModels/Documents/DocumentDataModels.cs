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
}

public class SponsorDocumentHistoryDataModel
{
    public bool IsAuthorized { get; init; }
    public List<SharedDocumentDataModel> Documents { get; init; } = [];
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

public class DocumentShareNotificationDataModel
{
    public long DocumentShareId { get; init; }
    public string SponsorEmail { get; init; } = string.Empty;
    public string SponsorFirstName { get; init; } = string.Empty;
    public string? SponsorNickName { get; init; }
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