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
}