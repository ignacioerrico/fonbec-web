using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.PlannedDeliveries;
using Fonbec.Web.Logic.Models.PlannedDeliveries.Input;
using Fonbec.Web.Logic.Models.Results;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface IPlannedDeliveryService
{
    Task<List<DateTime>> GetPlannedDeliveryDatesAsync(int? chapterId, DateTime? from = null);
    Task<CrudResult> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputModel inputModel);
    Task<List<PlannedDeliveriesListViewModel>> GetAllPlannedDeliveriesAsync();
    Task<CrudResult> UpdatePlannedDeliveryAsync(UpdatePlannedDeliveryInputModel inputModel);
}

public class PlannedDeliveryService(IPlannedDeliveryRepository plannedDeliveryRepository) : IPlannedDeliveryService
{
    public async Task<List<DateTime>> GetPlannedDeliveryDatesAsync(int? chapterId, DateTime? from = null)
    {
        if (chapterId is null or <= 0)
        {
            throw new ArgumentNullException(nameof(chapterId));
        }

        var plannedDeliveryDates = await plannedDeliveryRepository.GetPlannedDeliveryDatesAsync(chapterId.Value, from);
        return plannedDeliveryDates;
    }

    public async Task<CrudResult> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputModel inputModel)
    {
        var inputDataModel = inputModel.Adapt<CreatePlannedDeliveryInputDataModel>();
		var affectedRows = await plannedDeliveryRepository.CreatePlannedDeliveryAsync(inputDataModel);
		return new CrudResult(affectedRows);
	}
    public async Task<List<PlannedDeliveriesListViewModel>> GetAllPlannedDeliveriesAsync()
    {
        var allPlannedDeliveriesDataModel = await plannedDeliveryRepository.GetAllPlannedDeliveriesAsync();
        var plannedDeliveriesListViewModel = allPlannedDeliveriesDataModel.Adapt<List<PlannedDeliveriesListViewModel>>();
        return plannedDeliveriesListViewModel;
    }
    public async Task<CrudResult> UpdatePlannedDeliveryAsync(UpdatePlannedDeliveryInputModel inputModel)
    {
        var updatePlannedDeliveryInputDataModel = inputModel.Adapt<UpdatePlannedDeliveryInputDataModel>();
        var affectedRows = await plannedDeliveryRepository.UpdatePlannedDeliveryAsync(updatePlannedDeliveryInputDataModel);
        return new CrudResult(affectedRows);
    }

}
