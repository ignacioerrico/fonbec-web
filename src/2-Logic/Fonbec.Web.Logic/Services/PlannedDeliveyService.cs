using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.PlannedDeliveries.Input;
using Fonbec.Web.Logic.Models.Results;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface IPlannedDeliveryService
{
	Task<CrudResult> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputModel inputModel);
}
public class PlannedDeliveyService(IPlannedDeliveryRepository plannedDeliveryRepository) : IPlannedDeliveryService
{
	public async Task<CrudResult> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputModel inputModel)
	{
		var incomplete = await plannedDeliveryRepository.HasActivePlannedDeliveryAsync();
		if (incomplete)
		{
			return new CrudResult(0, "A new plan cannot be created until the current plan is completed.");
		}

		var dateExists = await plannedDeliveryRepository.PlannedDeliveryDateExistsAsync(inputModel.DeliverableStartsOn);
		if (dateExists)
		{
			return new CrudResult(0, "Planned delivery already exists for that period.");
		}

		var chapterExists = await plannedDeliveryRepository.ChapterExistsAsync(inputModel.ChapterId);
		if (!chapterExists)
		{
			return new CrudResult(0, "Enter a chapter Id that exists");
		}

		var inputDataModel = inputModel.Adapt<CreatePlannedDeliveryInputDataModel>();
		var affectedRows = await plannedDeliveryRepository.CreatePlannedDeliveryAsync(inputDataModel);
		return new CrudResult(affectedRows);
	}
}
