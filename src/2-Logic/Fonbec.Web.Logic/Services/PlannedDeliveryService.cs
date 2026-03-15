using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.PlannedDeliveries.Input;
using Fonbec.Web.Logic.Models.Results;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface IPlannedDeliveryService
{
	Task<CrudResult> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputModel inputModel);
    Task<List<DateTime>> GetPlannedDeliveryDatesAsync(int? chapterId);
}
public class PlannedDeliveryService(IPlannedDeliveryRepository plannedDeliveryRepository) : IPlannedDeliveryService
{
	public async Task<List<DateTime>> GetPlannedDeliveryDatesAsync(int? chapterId)
	{
        if (chapterId is null or 0)
        {
            return [];
        }
        var presentDates = await plannedDeliveryRepository.GetPlannedDeliveryDatesAsync(chapterId.Value);
		return presentDates.ToList();
	}
    public async Task<CrudResult> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputModel inputModel)
	{
        var inputDataModel = inputModel.Adapt<CreatePlannedDeliveryInputDataModel>();
		var affectedRows = await plannedDeliveryRepository.CreatePlannedDeliveryAsync(inputDataModel);
		return new CrudResult(affectedRows);
	}
}
