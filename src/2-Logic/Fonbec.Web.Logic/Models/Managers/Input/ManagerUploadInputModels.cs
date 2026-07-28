using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Models.Facilitators.Input;

namespace Fonbec.Web.Logic.Models.Managers.Input;

/// <summary>
/// Upload request for a letter. Exactly one content source must be provided,
/// matching <see cref="ContentMode"/>. In <see cref="UploadContentMode.File"/> mode,
/// <see cref="Files"/> may contain several images (pages) in the manager-specified order;
/// non-image files are always a single entry. <see cref="ManagerChapterId"/> travels with the
/// request so the document services can enforce the server-side chapter check.
/// </summary>
public record ManagerUploadLetterInputModel(
    int StudentId,
    int ManagerChapterId,
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
public record ManagerUploadReportCardInputModel(
    int StudentId,
    int ManagerChapterId,
    DateOnly Period,
    string Description,
    UploadContentMode ContentMode,
    IReadOnlyList<UploadFileInputModel>? Files,
    string? YouTubeUrlOrId,
    string? UploaderNotes);

/// <summary>
/// Upload request for an "other" document.
/// </summary>
public record ManagerUploadOtherInputModel(
    int StudentId,
    int ManagerChapterId,
    string Description,
    UploadContentMode ContentMode,
    IReadOnlyList<UploadFileInputModel>? Files,
    string? TextContent,
    string? YouTubeUrlOrId,
    string? UploaderNotes);
