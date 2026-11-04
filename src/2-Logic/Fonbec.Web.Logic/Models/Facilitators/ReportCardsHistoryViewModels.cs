using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities.Enums;
using Mapster;

namespace Fonbec.Web.Logic.Models.Facilitators;

public class ReportCardsHistoryViewModel
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = null!;

    public EducationLevel EducationLevel { get; set; }

    public List<ReportCardHistoryItemViewModel> Items { get; set; } = [];
}

public class ReportCardHistoryItemViewModel
{
    public long DocumentId { get; set; }

    public DateOnly Period { get; set; }

    public string PeriodLabel => Period.ToString("MMM/yy");

    public string? Description { get; set; }

    public DateOnly Date { get; set; }

    public FileKind FileKind { get; set; }

    public DocumentStatus Status { get; set; }

    public string? RejectionReason { get; set; }

    public bool CanDownload { get; set; }

    public string? TextContent { get; set; }

    public string? YouTubeVideoId { get; set; }

    public int PageCount { get; set; }
}

public class ReportCardsHistoryViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FacilitatorReportCardHistoryItemDataModel, ReportCardHistoryItemViewModel>()
            .Map(dest => dest.Date, src => DateOnly.FromDateTime(src.UploadedOn))
            .Map(dest => dest.CanDownload, src => src.FileKind == FileKind.Blob && src.PageCount > 0)
            .Map(dest => dest.RejectionReason, src =>
                src.Status == DocumentStatus.Rejected ? src.RejectionReason : null);
    }
}