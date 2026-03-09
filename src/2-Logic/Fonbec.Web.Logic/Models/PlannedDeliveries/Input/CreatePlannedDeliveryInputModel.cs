using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.PlannedDeliveries.Input;

public record CreatePlannedDeliveryInputModel(
	DateTime? DeliverableStartsOn,
	bool IsCompleted,
	int ChapterId,
	int CreatedById
);
public class CreatePlannedDeliveryInputModelMappingDefinitions : IRegister
{
	public void Register(TypeAdapterConfig config)
	{
		config.NewConfig<CreatePlannedDeliveryInputModel, CreatePlannedDeliveryInputDataModel>()
			.Map(dest => dest.DeliverableStartsOn, src => src.DeliverableStartsOn)
			.Map(dest => dest.IsCompleted, src => src.IsCompleted)
			.Map(dest => dest.ChapterId, src => src.ChapterId)
			.Map(dest => dest.CreatedById, src => src.CreatedById);
	}
}


