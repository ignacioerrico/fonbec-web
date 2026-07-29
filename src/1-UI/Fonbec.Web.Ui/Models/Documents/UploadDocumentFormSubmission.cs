using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Models.Facilitators.Input;

namespace Fonbec.Web.Ui.Models.Documents;

/// <summary>
/// Values captured by the shared upload form and handed back to the host page, which maps them
/// to the appropriate (facilitator or manager) upload service call. Content that does not match
/// the selected <see cref="ContentMode"/> is left null.
/// </summary>
public record UploadDocumentFormSubmission(
    UploadContentMode ContentMode,
    IReadOnlyList<UploadFileInputModel>? Files,
    string? TextContent,
    string? YouTubeUrlOrId,
    DateOnly? Period,
    string Description,
    string? UploaderNotes);
