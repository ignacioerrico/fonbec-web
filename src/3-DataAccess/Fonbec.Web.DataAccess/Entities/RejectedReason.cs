using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.Entities;

public class RejectedReason
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DocumentType? AppliesToDocumentType { get; set; }

    public bool RequiresNotes { get; set; }
}