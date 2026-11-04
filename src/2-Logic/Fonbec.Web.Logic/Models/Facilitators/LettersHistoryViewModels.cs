using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Facilitators;

public class LettersHistoryViewModel
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = null!;

    public List<LetterHistoryItemViewModel> Items { get; set; } = [];
}

public class LetterHistoryItemViewModel
{
    public long DocumentId { get; set; }

    public DateOnly Date { get; set; }

    public DateTime PlanStartsOn { get; set; }

    public string CampaignLabel => PlanStartsOn.ToSpanishMonthYear();

    public string RecipientName { get; set; } = null!;

    public FileKind FileKind { get; set; }

    public DocumentStatus Status { get; set; }

    public string? RejectionReason { get; set; }

    public bool CanDownload { get; set; }

    public string? TextContent { get; set; }

    public string? YouTubeVideoId { get; set; }

    public int PageCount { get; set; }
}

public class LettersHistoryViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FacilitatorLetterHistoryItemDataModel, LetterHistoryItemViewModel>()
            .Map(dest => dest.Date, src => DateOnly.FromDateTime(src.UploadedOn))
            .Map(dest => dest.CanDownload, src => src.FileKind == FileKind.Blob && src.PageCount > 0)
            .Map(dest => dest.RejectionReason, src =>
                src.Status == DocumentStatus.Rejected ? src.RejectionReason : null);
    }
}