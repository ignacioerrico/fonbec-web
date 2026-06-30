using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Models.Documents.Input;

public record CreateBlobPathInputModel(
    string StoragePath,
    string MimeType,
    long? FileSizeBytes = null,
    byte[]? Sha256 = null);

public record CreateDocumentUserContext(
    int UserId,
    string UserRole,
    int? ChapterId,
    string? FonbecAuthClaim);

public record CreateDocumentBaseInputModel(
    int StudentId,
    CreateDocumentUserContext User,
    FileKind FileKind,
    CreateBlobPathInputModel? Blob = null,
    string? YouTubeVideoId = null,
    string? TextContent = null,
    string? UploaderNotes = null);

public record CreateLetterInputModel(
    int StudentId,
    int PlanId,
    int SponsorId,
    CreateDocumentUserContext User,
    FileKind FileKind,
    CreateBlobPathInputModel? Blob = null,
    string? YouTubeVideoId = null,
    string? TextContent = null,
    string? UploaderNotes = null) : CreateDocumentBaseInputModel(
    StudentId, User, FileKind, Blob, YouTubeVideoId, TextContent, UploaderNotes);

public record CreateReportCardInputModel(
    int StudentId,
    CreateDocumentUserContext User,
    FileKind FileKind,
    DateOnly Period,
    string Description,
    CreateBlobPathInputModel? Blob = null,
    string? YouTubeVideoId = null,
    string? TextContent = null,
    string? UploaderNotes = null) : CreateDocumentBaseInputModel(
    StudentId, User, FileKind, Blob, YouTubeVideoId, TextContent, UploaderNotes);

public record CreateOtherDocumentInputModel(
    int StudentId,
    CreateDocumentUserContext User,
    FileKind FileKind,
    string Description,
    CreateBlobPathInputModel? Blob = null,
    string? YouTubeVideoId = null,
    string? TextContent = null,
    string? UploaderNotes = null) : CreateDocumentBaseInputModel(
    StudentId, User, FileKind, Blob, YouTubeVideoId, TextContent, UploaderNotes);

public record SubmitDigitalImprovementInputModel(
    long DocumentId,
    int UserId,
    string UserRole,
    string? FonbecAuthClaim,
    CreateBlobPathInputModel ImprovedBlob,
    byte[] RowVersion);

public record ApproveLetterInputModel(
    long DocumentId,
    int ReviewerId,
    string ReviewerRole,
    byte[] RowVersion,
    bool ConfirmedIsLetter,
    DateTime ConfirmedWrittenDate,
    bool ConfirmedAddressee,
    bool ConfirmedSignerMatchesStudent,
    int SpellingScore,
    int PenmanshipScore,
    int ContentScore,
    bool HasRedFlags,
    bool HasGreenFlags,
    string? IssuesNotes,
    string? Appraisal);

public record RejectLetterInputModel(
    long DocumentId,
    int ReviewerId,
    string ReviewerRole,
    byte[] RowVersion,
    int RejectedReasonId,
    string? RejectionNotes);

public record ApproveReportCardInputModel(
    long DocumentId,
    int ReviewerId,
    string ReviewerRole,
    byte[] RowVersion,
    bool ConfirmedIsReportCardOrTranscript,
    bool ConfirmedStudentNameCorrect);

public record RejectReportCardInputModel(
    long DocumentId,
    int ReviewerId,
    string ReviewerRole,
    byte[] RowVersion,
    int RejectedReasonId,
    string? RejectionNotes);

public record ApproveOtherDocumentInputModel(
    long DocumentId,
    int ReviewerId,
    string ReviewerRole,
    byte[] RowVersion);

public record RejectOtherDocumentInputModel(
    long DocumentId,
    int ReviewerId,
    string ReviewerRole,
    byte[] RowVersion,
    int? RejectedReasonId,
    string? RejectionNotes);