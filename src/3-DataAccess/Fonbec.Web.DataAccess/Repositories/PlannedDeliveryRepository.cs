using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IPlannedDeliveryRepository
{
	Task<int> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputDataModel dataModel);
	Task<bool> HasActivePlannedDeliveryAsync();
	Task<bool> PlannedDeliveryDateExistsAsync(DateTime? date);
	Task<bool> ChapterExistsAsync(int chapterId);

}

public class PlannedDeliveryRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : IPlannedDeliveryRepository
{
	public async Task<bool> HasActivePlannedDeliveryAsync()
	{
		await using var db = await dbContext.CreateDbContextAsync();
		bool hasActiveDelivery = await db.PlannedDeliveries.AnyAsync(pd => !pd.Completed);
		return hasActiveDelivery;
	}

	public async Task<bool> PlannedDeliveryDateExistsAsync(DateTime? date)
	{
		if (date == null) return false;
		await using var db = await dbContext.CreateDbContextAsync();

		var firstDayOfMonth = new DateTime(date.Value.Year, date.Value.Month, 1);
		var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
		bool dateExists = await db.PlannedDeliveries.AnyAsync(pd =>
						pd.StartsOn >= firstDayOfMonth &&
						pd.StartsOn <= lastDayOfMonth);
		return dateExists;
	}

	public async Task<bool> ChapterExistsAsync(int chapterId)
	{
		await using var db = await dbContext.CreateDbContextAsync();
		bool chapterExists = await db.Chapters.AnyAsync(chapter => chapter.Id == chapterId);

		return chapterExists;
	}

	public async Task<int> CreatePlannedDeliveryAsync(CreatePlannedDeliveryInputDataModel dataModel)
	{
		await using var db = await dbContext.CreateDbContextAsync();

		var plannedDelivery = new PlannedDelivery
		{
			ChapterId = dataModel.ChapterId,
			StartsOn = dataModel.DeliverableStartsOn,
			Completed = dataModel.IsCompleted,
			CreatedById = dataModel.CreatedById

		};

		db.PlannedDeliveries.Add(plannedDelivery);
		return await db.SaveChangesAsync();
	}

}

