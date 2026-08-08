using System.Globalization;

namespace Fonbec.Web.Logic.Models.LetterPlanProgress;

public class LetterPlanProgressViewModel
{
    public string PlanLabel { get; set; } = null!;
    public bool IsPlanCompleted { get; set; }
    public LetterPlanProgressSummaryViewModel Summary { get; set; } = null!;
    public List<LetterPlanProgressRowViewModel> Rows { get; set; } = [];
}

public class LetterPlanProgressSummaryViewModel
{
    public int TotalRequired { get; set; }
    public int Approved { get; set; }
    public int InProgress { get; set; }
    public int MissingOrRejected { get; set; }
    public decimal CompletionPercent { get; set; }

    public bool AllApproved =>
        TotalRequired > 0 && Approved == TotalRequired;
}

public class LetterPlanProgressRowViewModel
{
    public int StudentId { get; set; }
    public string StudentFirstName { get; set; } = null!;
    public string StudentLastName { get; set; } = null!;
    public string? StudentNickName { get; set; }
    public string FacilitatorFullName { get; set; } = null!;
    public int SponsorshipId { get; set; }
    public int? SponsorId { get; set; }
    public int? CompanyId { get; set; }
    public string RecipientName { get; set; } = null!;
    public bool IsCompanySponsorship { get; set; }
    public LetterPlanDisplayStatus Status { get; set; }
    public string StatusLabel { get; set; } = null!;
    public string? RejectionReason { get; set; }
    public string? ExemptionReason { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public bool IsFirstRowForStudent { get; set; }
    public bool IsStudentExempt { get; set; }
}

public static class LetterPlanProgressFormatting
{
    public static string FormatPlanLabel(DateTime planStartsOn) =>
        planStartsOn.ToString(@"MMMM \d\e yyyy", new CultureInfo("es-AR"));
}