namespace Fonbec.Web.DataAccess.DataModels.Chapters.Input;

public class CreateChapterInputDataModel
{
    public string ChapterName { get; set; } = string.Empty;

    public int ChapterCreatedById { get; set; }
    public string ChapterDescription { get; set; } = string.Empty;
}