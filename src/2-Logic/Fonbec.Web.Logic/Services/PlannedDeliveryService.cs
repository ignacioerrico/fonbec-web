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

public class PlannedDeliveryService(
    IPlannedDeliveryRepository plannedDeliveryRepository,
    ILetterPlanProgressRepository letterPlanProgressRepository) : IPlannedDeliveryService
{
    public const string IncompletePlanAlreadyExists =
        "Ya existe una planificación en curso. Debe completarse antes de crear una nueva.";

    public const string NoSlotsForMonth =
        "No hay apadrinamientos vigentes para ese mes.";

    public const string CannotChangeCompletedPlanDate =
        "No se puede cambiar el mes de una campaña completada.";

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

        var slotCount = await letterPlanProgressRepository.CountRequiredSlotsAsync(
            inputModel.ChapterId,
            inputModel.PlanStartsOn);
        if (slotCount == 0)
        {
            return new CrudResult(Errors: [NoSlotsForMonth]);
        }

        var inputDataModel = inputModel.Adapt<CreatePlannedDeliveryInputDataModel>();
        var affectedRows = await plannedDeliveryRepository.CreatePlannedDeliveryAsync(inputDataModel);
        return new CrudResult(affectedRows);
    }

    public async Task<CrudResult> UpdatePlannedDeliveryAsync(UpdatePlannedDeliveryInputModel inputModel)
    {
        var existing = await plannedDeliveryRepository.GetPlanMetadataAsync(inputModel.PlannedDeliveryId);
        if (existing is null)
        {
            return new CrudResult();
        }

        var dateChanged = existing.StartsOn.Year != inputModel.PlannedDeliveryStartsOn.Year
                          || existing.StartsOn.Month != inputModel.PlannedDeliveryStartsOn.Month;

        if (dateChanged && existing.Completed)
        {
            return new CrudResult(Errors: [CannotChangeCompletedPlanDate]);
        }

        if (dateChanged && existing.ChapterId is int chapterId)
        {
            var slotCount = await letterPlanProgressRepository.CountRequiredSlotsAsync(
                chapterId,
                inputModel.PlannedDeliveryStartsOn);
            if (slotCount == 0)
            {
                return new CrudResult(Errors: [NoSlotsForMonth]);
            }
        }

        var updatePlannedDeliveryInputDataModel = inputModel.Adapt<UpdatePlannedDeliveryInputDataModel>();
        if (existing.Completed)
        {
            updatePlannedDeliveryInputDataModel.PlannedDeliveryStartsOn = existing.StartsOn;
        }

        var affectedRows = await plannedDeliveryRepository.UpdatePlannedDeliveryAsync(updatePlannedDeliveryInputDataModel);
        return new CrudResult(affectedRows);
    }
}
