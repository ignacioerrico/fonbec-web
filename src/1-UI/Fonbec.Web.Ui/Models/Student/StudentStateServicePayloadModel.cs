namespace Fonbec.Web.Ui.Models.Student;
public class StudentStateServicePayloadModel
{
    public int ChapterId { get; private set; }
    public void SetChapter(int chapterId)
    { 
        ChapterId = chapterId;
    }
}

