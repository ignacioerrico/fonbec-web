using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.PlannedDeliveries.Input;

public record CreatePlannedDeliveryInputModel(
    int ChapterId,
    DateTime PlanStartsOn,
    string PlanNotes,
    int CreatedById);

public class CreatePlannedDeliveryInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreatePlannedDeliveryInputModel, CreatePlannedDeliveryInputDataModel>()
            .Map(dest => dest.ChapterId, src => src.ChapterId)
            .Map(dest => dest.PlanStartsOn, src => src.PlanStartsOn)
            .Map(dest => dest.PlanNotes, src => src.PlanNotes.NullOrTrimmed())
            .Map(dest => dest.CreatedById, src => src.CreatedById);
    }
}