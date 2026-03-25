using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.PlannedDeliveries.Input;
using Fonbec.Web.Logic.Models.Results;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface IPlannedDeliveryService
{
    Task<List<DateTime>> GetPlannedDeliveryDatesAsync(int? chapterId, DateTime? from = null);
    Task<CrudResult> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputModel inputModel);
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
}