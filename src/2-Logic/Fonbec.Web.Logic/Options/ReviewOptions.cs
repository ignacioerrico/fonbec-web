namespace Fonbec.Web.Logic.Options;

public class ReviewOptions
{
    public const string SectionName = "Review";

    /// <summary>
    /// Number of names presented by the candidate-name picker for a multiple-choice confirmation:
    /// the correct one plus (count - 1) distractors. Global; default 5. The set and order are
    /// deterministic for a given document.
    /// </summary>
    public int CandidateNameCount { get; set; } = 5;

    /// <summary>
    /// When true, a wrong name choice on the review page shows a hint under that list
    /// ("no es el destinatario correcto", "no es el firmante correcto", or the report-card
    /// equivalent). Off by default so the challenge does not reveal the correct person.
    /// </summary>
    public bool ShowNameMismatchHints { get; set; }
}