using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Mapster;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fonbec.Web.Logic.Models.PlannedDeliveries.Input;

public record UpdatePlannedDeliveryInputModel(
    int PlannedDeliveryId,
    DateTime PlannedDeliveryUpdateStartsOn,
    bool PlannedDeliveryUpdateIsCompleted,
    string? PlannedDeliveryUpdateNotes,
    int UpdatedById
    );

public class UpdatePlannedDeliveryInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UpdatePlannedDeliveryInputModel, UpdatePlannedDeliveryInputDataModel>()
            .Map(dest => dest.PlannedDeliveryId, src => src.PlannedDeliveryId)
            .Map(dest => dest.PlanStartsOn, src => src.PlannedDeliveryUpdateStartsOn)
            .Map(dest => dest.PlanNotes, src => src.PlannedDeliveryUpdateNotes)
            .Map(dest => dest.UpdatedById, src => src.UpdatedById);
    }
}

