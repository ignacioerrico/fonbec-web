using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.LetterPlanProgress;

public class LetterPlanProgressQueryResultDataModel
{
    public DateTime PlanStartsOn { get; init; }
    public bool IsPlanCompleted { get; init; }
    public List<LetterPlanProgressRowDataModel> Rows { get; init; } = [];
}

public class LetterPlanProgressRowDataModel
{
    public int StudentId { get; init; }
    public string StudentFirstName { get; init; } = null!;
    public string StudentLastName { get; init; } = null!;
    public string? StudentNickName { get; init; }
    public string FacilitatorFirstName { get; init; } = null!;
    public string FacilitatorLastName { get; init; } = null!;
    public int SponsorshipId { get; init; }
    public int? SponsorId { get; init; }
    public int? CompanyId { get; init; }
    public string RecipientName { get; init; } = null!;
    public bool IsCompanySponsorship { get; init; }
    public bool IsExempt { get; init; }
    public string? ExemptionReason { get; init; }
    public DocumentStatus? LetterStatus { get; init; }
    public string? RejectionReasonDescription { get; init; }
    public string? RejectionNotes { get; init; }
    public DateTime? ApprovedOn { get; init; }
}