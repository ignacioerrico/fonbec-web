using Fonbec.Web.Logic.Models.Facilitators.Input;

namespace Fonbec.Web.Ui.Models.Documents;

/// <summary>
/// Form state for the shared document upload form, used by both the facilitator and manager
/// upload pages.
/// </summary>
public class UploadDocumentBindModel
{
    public UploadContentMode ContentMode { get; set; } = UploadContentMode.File;

    /// <summary>Predefined chapter-scoped description option text (report card / other).</summary>
    public string? SelectedDescriptionOption { get; set; }

    /// <summary>Free-text description overriding the predefined option when provided.</summary>
    public string? FreeTextDescription { get; set; }

    /// <summary>Effective description: free text takes precedence over the predefined option.</summary>
    public string EffectiveDescription =>
        !string.IsNullOrWhiteSpace(FreeTextDescription)
            ? FreeTextDescription.Trim()
            : SelectedDescriptionOption?.Trim() ?? string.Empty;

    /// <summary>Period (month/year) for report cards; only month + year are relevant.</summary>
    public DateTime? Period { get; set; }

    public string? TextContent { get; set; }

    public string? YouTubeUrlOrId { get; set; }

    public string? UploaderNotes { get; set; }
}