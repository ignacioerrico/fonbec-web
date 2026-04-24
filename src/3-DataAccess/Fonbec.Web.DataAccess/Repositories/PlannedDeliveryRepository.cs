using Fonbec.Web.DataAccess.DataModels.PlannedDelivery;
using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IPlannedDeliveryRepository
{
    Task<List<DateTime>> GetPlannedDeliveryDatesAsync(int chapterId, DateTime? from);
    Task<int> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputDataModel dataModel);
    Task<int> UpdatePlannedDeliveryAsync(UpdatePlannedDeliveryInputDataModel dataModel);
    Task<List<AllPlannedDeliveriesDataModel>> GetAllPlannedDeliveriesAsync();


}

public class PlannedDeliveryRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : IPlannedDeliveryRepository
{
    public async Task<List<DateTime>> GetPlannedDeliveryDatesAsync(int chapterId, DateTime? from)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chapterId);

        await using var db = await dbContext.CreateDbContextAsync();

        var plannedDeliveryDates = await db.PlannedDeliveries
            .AsNoTracking()
            .Where(pd =>
                pd.ChapterId == chapterId
                && (!from.HasValue || pd.StartsOn >= from.Value))
            .Select(pd => pd.StartsOn)
            .ToListAsync();

        return plannedDeliveryDates;
    }

    public async Task<int> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputDataModel dataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var plannedDelivery = new PlannedDelivery
        {
            ChapterId = dataModel.ChapterId,
            StartsOn = dataModel.PlanStartsOn,
            Completed = false,
            Notes = dataModel.PlanNotes,
            CreatedById = dataModel.CreatedById,
        };

        db.PlannedDeliveries.Add(plannedDelivery);
        return await db.SaveChangesAsync();
    }

    public async Task<int> UpdatePlannedDeliveryAsync(UpdatePlannedDeliveryInputDataModel dataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var plannedDelivery = await db.PlannedDeliveries.FindAsync(dataModel.PlannedDeliveryId);

        if (plannedDelivery == null)
        {
            return 0;
        }

        plannedDelivery.StartsOn = dataModel.PlanStartsOn;
        plannedDelivery.Notes = dataModel.PlanNotes;
        plannedDelivery.LastUpdatedById = dataModel.UpdatedById;

        db.PlannedDeliveries.Update(plannedDelivery);
        return await db.SaveChangesAsync();

    }

    public async Task<List<AllPlannedDeliveriesDataModel>> GetAllPlannedDeliveriesAsync()
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var allPlannedDeliveries = await db.PlannedDeliveries
            .Include(ch => ch.CreatedBy)
            .Include(ch => ch.LastUpdatedBy)
            .Include(ch => ch.DisabledBy)
            .Include(ch => ch.ReenabledBy)
            .Select(pd => new AllPlannedDeliveriesDataModel(pd)
            {
                PlanStartsOn = pd.StartsOn
            })
            .ToListAsync();
        return allPlannedDeliveries;
    }
}

