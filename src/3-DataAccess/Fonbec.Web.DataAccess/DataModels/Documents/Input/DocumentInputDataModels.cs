namespace Fonbec.Web.DataAccess.DataModels.Documents.Input;

public class CreateBlobPathInputDataModel
{
    public string StoragePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long? FileSizeBytes { get; set; }
    public byte[]? Sha256 { get; set; }
}

public class CreateDocumentBaseInputDataModel
{
    public int StudentId { get; set; }
    public int UploadedById { get; set; }
    public Entities.Enums.FileKind FileKind { get; set; }
    public CreateBlobPathInputDataModel? Blob { get; set; }
    public string? YouTubeVideoId { get; set; }
    public string? TextContent { get; set; }
    public string? UploaderNotes { get; set; }
}

public class CreateLetterInputDataModel : CreateDocumentBaseInputDataModel
{
    public int PlanId { get; set; }
    public int SponsorId { get; set; }
}

public class CreateReportCardInputDataModel : CreateDocumentBaseInputDataModel
{
    public DateOnly Period { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class CreateOtherDocumentInputDataModel : CreateDocumentBaseInputDataModel
{
    public string Description { get; set; } = string.Empty;
}

public class SubmitDigitalImprovementInputDataModel
{
    public long DocumentId { get; set; }
    public int UserId { get; set; }
    public CreateBlobPathInputDataModel ImprovedBlob { get; set; } = null!;
    public byte[] RowVersion { get; set; } = null!;
}

public class ApproveLetterInputDataModel
{
    public long DocumentId { get; set; }
    public int ReviewerId { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public bool ConfirmedIsLetter { get; set; }
    public DateTime ConfirmedWrittenDate { get; set; }
    public bool ConfirmedAddressee { get; set; }
    public bool ConfirmedSignerMatchesStudent { get; set; }
    public int SpellingScore { get; set; }
    public int PenmanshipScore { get; set; }
    public int ContentScore { get; set; }
    public bool HasRedFlags { get; set; }
    public bool HasGreenFlags { get; set; }
    public string? IssuesNotes { get; set; }
    public string? Appraisal { get; set; }
}

public class RejectLetterInputDataModel
{
    public long DocumentId { get; set; }
    public int ReviewerId { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public int RejectedReasonId { get; set; }
    public string? RejectionNotes { get; set; }
}

public class ApproveReportCardInputDataModel
{
    public long DocumentId { get; set; }
    public int ReviewerId { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public bool ConfirmedIsReportCardOrTranscript { get; set; }
    public bool ConfirmedStudentNameCorrect { get; set; }
}

public class RejectReportCardInputDataModel
{
    public long DocumentId { get; set; }
    public int ReviewerId { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public int RejectedReasonId { get; set; }
    public string? RejectionNotes { get; set; }
}

public class ApproveOtherDocumentInputDataModel
{
    public long DocumentId { get; set; }
    public int ReviewerId { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}

public class RejectOtherDocumentInputDataModel
{
    public long DocumentId { get; set; }
    public int ReviewerId { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public int? RejectedReasonId { get; set; }
    public string? RejectionNotes { get; set; }
}