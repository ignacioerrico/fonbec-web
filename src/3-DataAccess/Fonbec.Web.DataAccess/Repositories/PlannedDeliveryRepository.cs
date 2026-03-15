using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IPlannedDeliveryRepository
{
    Task<int> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputDataModel dataModel);
    Task<List<DateTime>> GetPlannedDeliveryDatesAsync(int chapterId);

}

public class PlannedDeliveryRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : IPlannedDeliveryRepository
{
    public async Task<List<DateTime>> GetPlannedDeliveryDatesAsync(int chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await db.PlannedDeliveries
            .Where(pd => pd.ChapterId == chapterId)
            .Select(pd => pd.StartsOn)
            .ToListAsync();
    }

    public async Task<int> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputDataModel dataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var plannedDelivery = new PlannedDelivery
        {
            ChapterId = dataModel.ChapterId,
            StartsOn = dataModel.DeliverableStartsOn,
            Completed = dataModel.IsCompleted,
            CreatedById = dataModel.CreatedById,
            Notes = dataModel.PlannedDeliveryNotes
        };

        db.PlannedDeliveries.Add(plannedDelivery);
        return await db.SaveChangesAsync();
    }

}

