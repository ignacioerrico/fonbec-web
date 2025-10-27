using Fonbec.Web.DataAccess.DataModels;
using Mapster;

namespace Fonbec.Web.Logic.Models;

public abstract class AuditableViewModel
{
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedOnUtc { get; set; }

    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdatedOnUtc { get; set; }

    public string? DisabledBy { get; set; }
    public DateTime? DisabledOnUtc { get; set; }

    public string? ReenabledBy { get; set; }
    public DateTime? ReenabledOnUtc { get; set; }

    public string Notes { get; set; } = null!;
}

public class AuditableViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AuditableDataModel, AuditableViewModel>()
            .Map(dest => dest.CreatedBy, src => src.CreatedBy.FullName())
            .Map(dest => dest.CreatedOnUtc, src => src.CreatedOnUtc)
            .Map(dest => dest.LastUpdatedBy, src => src.LastUpdatedBy!.FullName(), src => src.LastUpdatedBy != null)
            .Map(dest => dest.LastUpdatedOnUtc, src => src.LastUpdatedOnUtc)
            .Map(dest => dest.DisabledBy, src => src.DisabledBy!.FullName(), src => src.DisabledBy != null)
            .Map(dest => dest.DisabledOnUtc, src => src.DisabledOnUtc)
            .Map(dest => dest.ReenabledBy, src => src.ReenabledBy!.FullName(), src => src.ReenabledBy != null)
            .Map(dest => dest.ReenabledOnUtc, src => src.ReenabledOnUtc)
            .Map(dest => dest.Notes, src => src.Notes ?? string.Empty);
    }
}