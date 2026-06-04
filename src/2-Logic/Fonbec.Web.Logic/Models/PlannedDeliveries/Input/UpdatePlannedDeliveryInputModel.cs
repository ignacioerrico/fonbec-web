using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Mapster;

namespace Fonbec.Web.Logic.Models.PlannedDeliveries.Input;

public record UpdatePlannedDeliveryInputModel(
    int PlannedDeliveryId,
    DateTime PlannedDeliveryStartsOn,
    string? PlannedDeliveryNotes,
    int UpdatedById
);

public class UpdatePlannedDeliveryInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UpdatePlannedDeliveryInputModel, UpdatePlannedDeliveryInputDataModel>()
            .Map(dest => dest.PlannedDeliveryId, src => src.PlannedDeliveryId)
            .Map(dest => dest.PlannedDeliveryStartsOn, src => src.PlannedDeliveryStartsOn)
            .Map(dest => dest.PlannedDeliveryNotes, src => src.PlannedDeliveryNotes)
            .Map(dest => dest.UpdatedById, src => src.UpdatedById);
    }
}