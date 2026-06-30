namespace Fonbec.Web.DataAccess.Entities;

public class Assessment
{
    public long AssessmentId { get; set; }

    public int SpellingScore { get; set; }

    public int PenmanshipScore { get; set; }

    public int ContentScore { get; set; }

    public bool HasRedFlags { get; set; }

    public bool HasGreenFlags { get; set; }

    public string? IssuesNotes { get; set; }

    public string? Appraisal { get; set; }

    public LetterReview? LetterReview { get; set; }
}