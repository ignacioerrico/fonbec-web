namespace Fonbec.Web.Logic.Models.Review;

/// <summary>
/// A shuffled multiple-choice set of names for a review confirmation. Includes the correct name
/// (identified by <see cref="CorrectId"/>) plus random distractors. The consuming panel adds the
/// explicit "Falta / No figura" ("missing / not shown") option.
/// </summary>
public class CandidateNamesViewModel
{
    public int CorrectId { get; init; }

    public IReadOnlyList<CandidateNameViewModel> Names { get; init; } = [];
}

public record CandidateNameViewModel(int Id, string DisplayName);
