namespace Fonbec.Web.DataAccess.DataModels.Chapters.Input;

public class UpdateChapterInputDataModel
{
    public int ChapterId { get; set; }

    public string ChapterUpdatedName { get; set; } = null!;
    
    public string? ChapterUpdatedNotes { get; set; }

    public int UpdatedById { get; set; }
}