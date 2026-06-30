using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.Entities;

/// <summary>
/// A predefined description a facilitator can pick when uploading a report card or other document.
/// A null <see cref="ChapterId"/> denotes a global default available to every chapter.
/// </summary>
public class DocumentDescriptionOption
{
    public int DocumentDescriptionOptionId { get; set; }

    public int? ChapterId { get; set; }
    public Chapter? Chapter { get; set; }

    public DocumentType DocumentType { get; set; }

    public string Text { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }
}
