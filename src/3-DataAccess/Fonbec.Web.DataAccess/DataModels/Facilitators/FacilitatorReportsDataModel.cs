using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Facilitators;

public class FacilitatorReportsDataModel
{
    public long ReportCardId { get; set; }

    public int StudentId { get; set; }

    public DateOnly Period { get; set; }

    public string Description { get; set; } = string.Empty;

    public DocumentStatus Status { get; set; }

    public string? RejectionReason { get; set; }
}