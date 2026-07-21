namespace Fonbec.Web.DataAccess.Entities;

/// <summary>
/// One page (file) of a blob-backed document. Documents supplied as files store their content here:
/// a PDF or a single image is one page; a multi-image document (only images may span several files)
/// has one page per image. <see cref="PageNumber"/> defines the facilitator-specified order.
/// </summary>
public class DocumentPage
{
    public long DocumentPageId { get; set; }

    public long DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    /// <summary>1-based page order within the document.</summary>
    public int PageNumber { get; set; }

    /// <summary>The originally uploaded file for this page (immutable).</summary>
    public long OriginalBlobPathId { get; set; }
    public BlobPath OriginalBlobPath { get; set; } = null!;

    /// <summary>The digitally improved replacement for this page, when improvement has completed.</summary>
    public long? ImprovedBlobPathId { get; set; }
    public BlobPath? ImprovedBlobPath { get; set; }
}