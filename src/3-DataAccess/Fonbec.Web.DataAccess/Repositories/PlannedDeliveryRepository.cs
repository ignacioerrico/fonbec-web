using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IPlannedDeliveryRepository
{
    Task<List<DateTime>> GetPlannedDeliveryDatesAsync(int chapterId, DateTime? from);
    Task<int> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputDataModel dataModel);
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
}