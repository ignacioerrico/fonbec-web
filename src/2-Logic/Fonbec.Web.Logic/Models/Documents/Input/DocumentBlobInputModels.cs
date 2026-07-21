namespace Fonbec.Web.Logic.Models.Documents.Input;

/// <summary>
/// A single uploaded file. Only image documents (JPG/PNG) may consist of several files (pages);
/// PDF and text/YouTube documents are always a single file. List order defines page order.
/// </summary>
public record UploadFileInputModel(Stream Content, string MimeType);

public record CreateLetterWithBlobInputModel(
    int StudentId,
    int PlanId,
    int? SponsorId,
    CreateDocumentUserContext User,
    IReadOnlyList<UploadFileInputModel> Files,
    string? UploaderNotes = null,
    int? CompanyId = null);

public record CreateReportCardWithBlobInputModel(
    int StudentId,
    CreateDocumentUserContext User,
    IReadOnlyList<UploadFileInputModel> Files,
    DateOnly Period,
    string Description,
    string? UploaderNotes = null);

public record CreateOtherDocumentWithBlobInputModel(
    int StudentId,
    CreateDocumentUserContext User,
    IReadOnlyList<UploadFileInputModel> Files,
    string Description,
    string? UploaderNotes = null);

public record SubmitDigitalImprovementWithBlobInputModel(
    long DocumentId,
    int UserId,
    string UserRole,
    string? FonbecAuthClaim,
    IReadOnlyList<UploadFileInputModel> Files,
    byte[] RowVersion);
