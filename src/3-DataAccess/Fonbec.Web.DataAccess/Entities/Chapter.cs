using Fonbec.Web.DataAccess.Entities.Abstract;
using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.Entities;

public class Chapter : Auditable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Applies to every document type. Existing decisions are left as they are.
    /// </summary>
    public DocumentRejectionBehavior DocumentRejectionBehavior { get; set; }

    /// <summary>
    /// Text placed before every email subject for this chapter. A space is added before the subject.
    /// </summary>
    public string EmailSubjectPrefix { get; set; } = "FONBEC |";

    public List<ChapterEmailTemplate> EmailTemplates { get; set; } = [];
}