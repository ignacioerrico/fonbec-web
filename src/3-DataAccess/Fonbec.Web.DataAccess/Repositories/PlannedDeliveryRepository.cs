using Fonbec.Web.DataAccess.DataModels.PlannedDelivery;
using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IPlannedDeliveryRepository
{
    Task<CurrentPlannedDeliveryDataModel?> GetCurrentPlanAsync(int chapterId);
    Task<CurrentPlannedDeliveryDataModel?> GetLatestCompletedPlanAsync(int chapterId);
    Task<List<AllPlannedDeliveriesDataModel>> GetCompletedPlansAsync(int chapterId);
    Task<bool> HasIncompletePlanAsync(int chapterId);
    Task<List<DateTime>> GetPlannedDeliveryDatesAsync(int chapterId, DateTime? from);
    Task<int> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputDataModel dataModel);
    Task<int> UpdatePlannedDeliveryAsync(UpdatePlannedDeliveryInputDataModel dataModel);
    Task<bool> MarkPlanCompletedAsync(int planId);
}

public class PlannedDeliveryRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : IPlannedDeliveryRepository
{
    public async Task<CurrentPlannedDeliveryDataModel?> GetCurrentPlanAsync(int chapterId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chapterId);

        await using var db = await dbContext.CreateDbContextAsync();

        return await db.PlannedDeliveries
            .AsNoTracking()
            .Where(pd => pd.IsActive
                         && pd.ChapterId == chapterId
                         && !pd.Completed)
            .OrderByDescending(pd => pd.StartsOn)
            .Select(pd => new CurrentPlannedDeliveryDataModel
            {
                PlannedDeliveryId = pd.Id,
                PlannedDeliveryStartsOn = pd.StartsOn,
                IsPlannedDeliveryCompleted = pd.Completed,
            })
            .FirstOrDefaultAsync();
    }

    public async Task<CurrentPlannedDeliveryDataModel?> GetLatestCompletedPlanAsync(int chapterId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chapterId);

        await using var db = await dbContext.CreateDbContextAsync();

        return await db.PlannedDeliveries
            .AsNoTracking()
            .Where(pd => pd.IsActive
                         && pd.ChapterId == chapterId
                         && pd.Completed)
            .OrderByDescending(pd => pd.StartsOn)
            .Select(pd => new CurrentPlannedDeliveryDataModel
            {
                PlannedDeliveryId = pd.Id,
                PlannedDeliveryStartsOn = pd.StartsOn,
                IsPlannedDeliveryCompleted = pd.Completed,
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<AllPlannedDeliveriesDataModel>> GetCompletedPlansAsync(int chapterId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chapterId);

        await using var db = await dbContext.CreateDbContextAsync();

        return await db.PlannedDeliveries
            .AsNoTracking()
            .Include(pd => pd.CreatedBy)
            .Include(pd => pd.LastUpdatedBy)
            .Include(pd => pd.DisabledBy)
            .Include(pd => pd.ReenabledBy)
            .Where(pd => pd.IsActive
                         && pd.ChapterId == chapterId
                         && pd.Completed)
            .OrderByDescending(pd => pd.StartsOn)
            .Select(pd => new AllPlannedDeliveriesDataModel(pd)
            {
                PlannedDeliveryId = pd.Id,
                PlannedDeliveryStartsOn = pd.StartsOn,
                IsPlannedDeliveryCompleted = pd.Completed,
                LettersDelivered = db.Set<Letter>()
                    .Count(l => l.PlanId == pd.Id && l.Status == DocumentStatus.Approved),
                ExemptStudents = db.LetterExemptions
                    .Count(e => e.PlannedDeliveryId == pd.Id && !e.IsRevoked),
            })
            .ToListAsync();
    }

    public async Task<bool> HasIncompletePlanAsync(int chapterId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chapterId);

        await using var db = await dbContext.CreateDbContextAsync();

        return await db.PlannedDeliveries
            .AsNoTracking()
            .AnyAsync(pd => pd.IsActive
                            && pd.ChapterId == chapterId
                            && !pd.Completed);
    }

    public async Task<List<DateTime>> GetPlannedDeliveryDatesAsync(int chapterId, DateTime? from)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chapterId);

        await using var db = await dbContext.CreateDbContextAsync();

        var plannedDeliveryDates = await db.PlannedDeliveries
            .AsNoTracking()
            .Where(pd =>
                pd.ChapterId == chapterId
                && pd.IsActive
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

        plannedDelivery.StartsOn = dataModel.PlannedDeliveryStartsOn;
        plannedDelivery.Notes = dataModel.PlannedDeliveryNotes;
        plannedDelivery.LastUpdatedById = dataModel.UpdatedById;

        db.PlannedDeliveries.Update(plannedDelivery);
        return await db.SaveChangesAsync();
    }

    public async Task<bool> MarkPlanCompletedAsync(int planId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var plannedDelivery = await db.PlannedDeliveries.FindAsync(planId);
        if (plannedDelivery is null || plannedDelivery.Completed)
        {
            return false;
        }

        plannedDelivery.Completed = true;
        await db.SaveChangesAsync();
        return true;
    }
}