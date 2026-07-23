using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Models.Managers;

/// <summary>
/// Read-only context for the manager backup upload page. Resolved server-side from the
/// logged-in manager's chapter, the target student, and the trigger query parameters.
/// </summary>
public class ManagerUploadContextViewModel
{
    public int StudentId { get; init; }

    public string StudentFullName { get; init; } = string.Empty;

    public int ChapterId { get; init; }

    public DocumentType DocumentType { get; init; }

    public EducationLevel EducationLevel { get; init; }

    /// <summary>Whether plain-text content is allowed for this document type.</summary>
    public bool AllowsTextContent => DocumentType is DocumentType.Letter or DocumentType.Other;

    /// <summary>Assigned facilitator's full name, shown so the manager can confirm they are acting as backup.</summary>
    public string FacilitatorFullName { get; init; } = string.Empty;

    // --- Letter-only context (read-only display) ---

    public int? PlanId { get; init; }

    public string? PlanPeriodLabel { get; init; }

    public int? SponsorId { get; init; }

    public int? CompanyId { get; init; }

    /// <summary>Recipient (sponsor or company) display name for letters.</summary>
    public string? RecipientName { get; init; }
}

/// <summary>A candidate recipient (sponsor or company) for a manager backup letter upload.</summary>
public class ManagerLetterRecipientOptionViewModel
{
    public int? SponsorId { get; init; }

    public int? CompanyId { get; init; }

    public string RecipientName { get; init; } = string.Empty;
}

/// <summary>
/// Options available to a manager choosing a letter recipient for a student, resolved from
/// the chapter's current plan and the student's active sponsorships. Used by the type-picker
/// dialog opened from the Students list, before navigating to the upload page.
/// </summary>
public class ManagerLetterRecipientOptionsViewModel
{
    public int? PlanId { get; init; }

    public string? PlanPeriodLabel { get; init; }

    /// <summary>True when the student is exempt from submitting a letter for the current plan.</summary>
    public bool IsExempt { get; init; }

    public List<ManagerLetterRecipientOptionViewModel> Options { get; init; } = [];
}
