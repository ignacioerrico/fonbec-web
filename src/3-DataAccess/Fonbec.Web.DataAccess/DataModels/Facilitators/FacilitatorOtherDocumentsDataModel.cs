using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Facilitators;

public class FacilitatorOtherDocumentsHistoryDataModel
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = null!;

    public List<FacilitatorOtherDocumentItemDataModel> Items { get; set; } = [];
}

public class FacilitatorOtherDocumentItemDataModel
{
    public long DocumentId { get; set; }

    public DateTime UploadedOn { get; set; }

    public string? Description { get; set; }

    public FileKind FileKind { get; set; }

    public DocumentStatus Status { get; set; }

    public string? RejectionReason { get; set; }

    public string? TextContent { get; set; }

    public string? YouTubeVideoId { get; set; }

    public int PageCount { get; set; }
}