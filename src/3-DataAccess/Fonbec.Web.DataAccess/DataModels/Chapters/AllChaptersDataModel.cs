using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.DataModels.Chapters;

public class AllChaptersDataModel(Auditable auditable) : AuditableDataModel(auditable)
{
    public int ChapterId { get; set; }

    public string ChapterName { get; set; } = string.Empty;
    
    public bool IsChapterActive { get; set; }
}