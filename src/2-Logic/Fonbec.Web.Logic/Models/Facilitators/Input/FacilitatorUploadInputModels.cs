using Fonbec.Web.Logic.Models.Documents.Input;

namespace Fonbec.Web.Logic.Models.Facilitators.Input;

/// <summary>How the document content is supplied by the facilitator.</summary>
public enum UploadContentMode
{
    File = 1,
    Text = 2,
    YouTube = 3,
}

/// <summary>
/// Upload request for a letter. Exactly one content source must be provided,
/// matching <see cref="ContentMode"/>. In <see cref="UploadContentMode.File"/> mode,
/// <see cref="Files"/> may contain several images (pages) in the facilitator-specified order;
/// non-image files are always a single entry.
/// </summary>
public record FacilitatorUploadLetterInputModel(
    int StudentId,
    int PlanId,
    int? SponsorId,
    int? CompanyId,
    UploadContentMode ContentMode,
    IReadOnlyList<UploadFileInputModel>? Files,
    string? TextContent,
    string? YouTubeUrlOrId,
    string? UploaderNotes);

/// <summary>
/// Upload request for a report card / transcript. Plain text is not allowed.
/// </summary>
public record FacilitatorUploadReportCardInputModel(
    int StudentId,
    DateOnly Period,
    string Description,
    UploadContentMode ContentMode,
    IReadOnlyList<UploadFileInputModel>? Files,
    string? YouTubeUrlOrId,
    string? UploaderNotes);

/// <summary>
/// Upload request for an "other" document.
/// </summary>
public record FacilitatorUploadOtherInputModel(
    int StudentId,
    string Description,
    UploadContentMode ContentMode,
    IReadOnlyList<UploadFileInputModel>? Files,
    string? TextContent,
    string? YouTubeUrlOrId,
    string? UploaderNotes);
