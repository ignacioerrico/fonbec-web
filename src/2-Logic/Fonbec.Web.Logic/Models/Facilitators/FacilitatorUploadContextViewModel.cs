using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Models.Facilitators;

/// <summary>
/// Read-only context for the facilitator upload page. Resolved server-side from the
/// logged-in facilitator, the target student, and the trigger query parameters.
/// Carries no document status/aggregation data (that belongs to the dashboard story).
/// </summary>
public class FacilitatorUploadContextViewModel
{
    public int StudentId { get; init; }

    public string StudentFullName { get; init; } = string.Empty;

    public int ChapterId { get; init; }

    public DocumentType DocumentType { get; init; }

    public EducationLevel EducationLevel { get; init; }

    /// <summary>Whether plain-text content is allowed for this document type.</summary>
    public bool AllowsTextContent => DocumentType is DocumentType.Letter or DocumentType.Other;

    // --- Letter-only context (read-only display) ---

    public int? PlanId { get; init; }

    public string? PlanPeriodLabel { get; init; }

    public int? SponsorId { get; init; }

    public int? CompanyId { get; init; }

    /// <summary>Recipient (sponsor or company) display name for letters.</summary>
    public string? RecipientName { get; init; }
}