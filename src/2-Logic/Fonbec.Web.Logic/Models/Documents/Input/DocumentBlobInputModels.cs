namespace Fonbec.Web.Logic.Models.Documents.Input;

public record CreateLetterWithBlobInputModel(
    int StudentId,
    int PlanId,
    int SponsorId,
    CreateDocumentUserContext User,
    Stream Content,
    string MimeType,
    string? UploaderNotes = null);

public record CreateReportCardWithBlobInputModel(
    int StudentId,
    CreateDocumentUserContext User,
    Stream Content,
    string MimeType,
    DateOnly Period,
    string Description,
    string? UploaderNotes = null);

public record CreateOtherDocumentWithBlobInputModel(
    int StudentId,
    CreateDocumentUserContext User,
    Stream Content,
    string MimeType,
    string Description,
    string? UploaderNotes = null);

public record SubmitDigitalImprovementWithBlobInputModel(
    long DocumentId,
    int UserId,
    string UserRole,
    string? FonbecAuthClaim,
    Stream Content,
    string MimeType,
    byte[] RowVersion);
