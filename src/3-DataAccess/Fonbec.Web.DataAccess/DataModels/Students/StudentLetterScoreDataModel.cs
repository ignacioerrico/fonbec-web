namespace Fonbec.Web.DataAccess.DataModels.Students;

public class StudentLetterScoreDataModel
{
    public int StudentId { get; init; }

    public int PlanId { get; init; }

    public DateTime PlanStartsOn { get; init; }

    public int SpellingScore { get; init; }

    public int PenmanshipScore { get; init; }

    public int ContentScore { get; init; }
}