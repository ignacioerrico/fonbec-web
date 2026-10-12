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

    /// <summary>Populated only when the supplied <c>planId</c> is an open plan for the student's chapter (letter uploads).</summary>
    public DateTime? PlanStartsOn { get; init; }

    /// <summary>Populated only when the supplied <c>sponsorId</c> sponsors the student on that plan.</summary>
    public string? SponsorFirstName { get; init; }

    public string? SponsorLastName { get; init; }

    /// <summary>Populated only when the supplied <c>companyId</c> sponsors the student on that plan.</summary>
    public string? CompanyName { get; init; }
}