namespace Fonbec.Web.DataAccess.DataModels.Review;

public class CandidateNameDataModel
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;

    /// <summary>True when this name is a company; false for a person (sponsor or student).</summary>
    public bool IsCompany { get; init; }
}