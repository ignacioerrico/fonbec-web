using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Facilitators;

public class FacilitatorLettersHistoryDataModel
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = null!;

    public List<FacilitatorLetterHistoryItemDataModel> Items { get; set; } = [];
}

public class FacilitatorLetterHistoryItemDataModel
{
    public long DocumentId { get; set; }

    public DateTime UploadedOn { get; set; }

    public DateTime PlanStartsOn { get; set; }

    public string RecipientName { get; set; } = null!;

    public FileKind FileKind { get; set; }

    public DocumentStatus Status { get; set; }

    public string? RejectionReason { get; set; }

    public string? TextContent { get; set; }

    public string? YouTubeVideoId { get; set; }

    public int PageCount { get; set; }
}