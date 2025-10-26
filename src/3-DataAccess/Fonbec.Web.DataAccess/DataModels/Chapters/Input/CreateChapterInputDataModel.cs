namespace Fonbec.Web.DataAccess.DataModels.Chapters.Input;

public class CreateChapterInputDataModel
{
    public string ChapterName { get; set; } = string.Empty;

    public string? ChapterDescription { get; set; }

    public int ChapterCreatedById { get; set; }
}