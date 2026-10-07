using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities.Enums;
using Mapster;

namespace Fonbec.Web.Logic.Models.Facilitators;

public class OtherDocumentsColumnViewModel
{
    public int Count { get; set; }

    public bool HasAny => Count > 0;
}

public class OtherDocumentsHistoryViewModel
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = null!;

    public List<OtherDocumentHistoryItemViewModel> Items { get; set; } = [];
}

public class OtherDocumentHistoryItemViewModel
{
    public long DocumentId { get; set; }

    public DateOnly Date { get; set; }

    public string? Description { get; set; }

    public FileKind FileKind { get; set; }

    public DocumentStatus Status { get; set; }

    public string? RejectionReason { get; set; }

    public bool CanDownload { get; set; }

    public string? TextContent { get; set; }

    public string? YouTubeVideoId { get; set; }

    public int PageCount { get; set; }
}

public class OtherDocumentsViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FacilitatorOtherDocumentItemDataModel, OtherDocumentHistoryItemViewModel>()
            .Map(dest => dest.Date, src => DateOnly.FromDateTime(src.UploadedOn))
            .Map(dest => dest.CanDownload, src => src.FileKind == FileKind.Blob && src.PageCount > 0);
    }
}