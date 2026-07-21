namespace Fonbec.Web.DataAccess.DataModels.Facilitators;

public class FacilitatorUploadContextDataModel
{
    public int StudentId { get; init; }

    public string StudentFirstName { get; init; } = string.Empty;

    public string StudentLastName { get; init; } = string.Empty;

    public int ChapterId { get; init; }

    public int FacilitatorId { get; init; }

    public bool IsActive { get; init; }

    public DateTime? SecondarySchoolStartYear { get; init; }

    public DateTime? UniversityStartYear { get; init; }

    /// <summary>Populated only when a <c>planId</c> is supplied (letter uploads).</summary>
    public DateTime? PlanStartsOn { get; init; }

    /// <summary>Populated only when a <c>sponsorId</c> is supplied (sponsor-based letter).</summary>
    public string? SponsorFirstName { get; init; }

    public string? SponsorLastName { get; init; }

    /// <summary>Populated only when a <c>companyId</c> is supplied (company-based letter).</summary>
    public string? CompanyName { get; init; }
}