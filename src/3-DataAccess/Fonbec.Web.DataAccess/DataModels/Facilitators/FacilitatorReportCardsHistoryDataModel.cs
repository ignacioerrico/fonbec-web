using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Facilitators;

public class FacilitatorReportCardsHistoryDataModel
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = null!;

    public EducationLevel EducationLevel { get; set; }

    public List<FacilitatorReportCardHistoryItemDataModel> Items { get; set; } = [];
}

public class FacilitatorReportCardHistoryItemDataModel
{
    public long DocumentId { get; set; }

    public DateOnly Period { get; set; }

    public string? Description { get; set; }

    public DateTime UploadedOn { get; set; }

    public FileKind FileKind { get; set; }

    public DocumentStatus Status { get; set; }

    public string? RejectionReason { get; set; }

    public string? TextContent { get; set; }

    public string? YouTubeVideoId { get; set; }

    public int PageCount { get; set; }
}