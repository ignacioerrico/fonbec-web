using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities.Enums;
using Mapster;

namespace Fonbec.Web.Logic.Models.Students;

public class ReportCardChipViewModel : AuditableViewModel
{
    public long ReportCardId { get; set; }
    public string PeriodLabel { get; set; } = null!;
    public DateOnly Period { get; set; }
    public string? Description { get; set; }
    public DocumentStatus Status { get; set; }
    public string? RejectionReason { get; set; }
}

public class ReportCardChipViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FacilitatorReportsDataModel, ReportCardChipViewModel>()
            .Map(dest => dest.ReportCardId, src => src.ReportCardId)
            .Map(dest => dest.Period, src => src.Period)
            .Map(dest => dest.PeriodLabel, src => src.Period.ToString("MMM/yy"))
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.Status, src => src.Status)
            .Map(dest => dest.RejectionReason, src => src.RejectionReason);
    }
}



