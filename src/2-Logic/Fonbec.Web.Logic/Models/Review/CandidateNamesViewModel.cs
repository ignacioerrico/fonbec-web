namespace Fonbec.Web.Logic.Models.Review;

/// <summary>
/// Identifies a candidate name. Numeric ids are not unique across sponsors, students, and companies,
/// so the kind is part of the identity.
/// </summary>
public readonly record struct CandidateNameKey(bool IsCompany, int Id);

/// <summary>
/// A shuffled multiple-choice set of names for a review confirmation. Includes the correct name
/// (identified by <see cref="CorrectKey"/>) plus random distractors. The consuming panel adds the
/// explicit "Falta / No figura" ("missing / not shown") option.
/// </summary>
public class CandidateNamesViewModel
{
    public bool CorrectIsCompany { get; init; }

    public int CorrectId { get; init; }

    public CandidateNameKey CorrectKey => new(CorrectIsCompany, CorrectId);

    public IReadOnlyList<CandidateNameViewModel> Names { get; init; } = [];
}

public record CandidateNameViewModel(bool IsCompany, int Id, string DisplayName)
{
    public CandidateNameKey Key => new(IsCompany, Id);
}

/// <summary>The reviewer's pick from a candidate-name list, including the displayed name.</summary>
public readonly record struct CandidateNamePick(CandidateNameSelection Selection, string? DisplayName);