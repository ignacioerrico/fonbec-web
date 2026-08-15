using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.PlannedDeliveries;
using Fonbec.Web.Logic.Models.PlannedDeliveries.Input;
using Fonbec.Web.Logic.Models.Results;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface IPlannedDeliveryService
{
    Task<CurrentPlannedDeliveryViewModel?> GetCurrentPlanAsync(int chapterId);
    Task<CurrentPlannedDeliveryViewModel?> GetLatestCompletedPlanAsync(int chapterId);
    Task<List<PlannedDeliveriesListViewModel>> GetCompletedPlansAsync(int chapterId);
    Task<List<DateTime>> GetPlannedDeliveryDatesAsync(int? chapterId, DateTime? from = null);
    Task<CrudResult> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputModel inputModel);
    Task<CrudResult> UpdatePlannedDeliveryAsync(UpdatePlannedDeliveryInputModel inputModel);
}

public class PlannedDeliveryService(IPlannedDeliveryRepository plannedDeliveryRepository) : IPlannedDeliveryService
{
    public const string IncompletePlanAlreadyExists =
        "Ya existe una planificación en curso. Debe completarse antes de crear una nueva.";

    public async Task<CurrentPlannedDeliveryViewModel?> GetCurrentPlanAsync(int chapterId)
    {
        var dataModel = await plannedDeliveryRepository.GetCurrentPlanAsync(chapterId);
        return dataModel?.Adapt<CurrentPlannedDeliveryViewModel>();
    }

    public async Task<CurrentPlannedDeliveryViewModel?> GetLatestCompletedPlanAsync(int chapterId)
    {
        var dataModel = await plannedDeliveryRepository.GetLatestCompletedPlanAsync(chapterId);
        return dataModel?.Adapt<CurrentPlannedDeliveryViewModel>();
    }

    public async Task<List<PlannedDeliveriesListViewModel>> GetCompletedPlansAsync(int chapterId)
    {
        var dataModels = await plannedDeliveryRepository.GetCompletedPlansAsync(chapterId);
        return dataModels.Adapt<List<PlannedDeliveriesListViewModel>>();
    }

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
        if (await plannedDeliveryRepository.HasIncompletePlanAsync(inputModel.ChapterId))
        {
            return new CrudResult(Errors: [IncompletePlanAlreadyExists]);
        }

        var inputDataModel = inputModel.Adapt<CreatePlannedDeliveryInputDataModel>();
        var affectedRows = await plannedDeliveryRepository.CreatePlannedDeliveryAsync(inputDataModel);
        return new CrudResult(affectedRows);
    }

    public async Task<CrudResult> UpdatePlannedDeliveryAsync(UpdatePlannedDeliveryInputModel inputModel)
    {
        var updatePlannedDeliveryInputDataModel = inputModel.Adapt<UpdatePlannedDeliveryInputDataModel>();
        var affectedRows = await plannedDeliveryRepository.UpdatePlannedDeliveryAsync(updatePlannedDeliveryInputDataModel);
        return new CrudResult(affectedRows);
    }
}