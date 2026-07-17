using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ILetterExemptionRepository
{
    /// <summary>
    /// Returns <c>true</c> when the student has an active (non-revoked) letter exemption for the plan.
    /// </summary>
    Task<bool> IsActiveExemptionAsync(int studentId, int plannedDeliveryId);

    /// <summary>
    /// Returns the ids of students with an active (non-revoked) letter exemption for the plan.
    /// </summary>
    Task<List<int>> GetActiveExemptStudentIdsForPlanAsync(int plannedDeliveryId);
}

public class LetterExemptionRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : ILetterExemptionRepository
{
    public async Task<bool> IsActiveExemptionAsync(int studentId, int plannedDeliveryId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await db.LetterExemptions
            .AsNoTracking()
            .AnyAsync(e => e.StudentId == studentId
                           && e.PlannedDeliveryId == plannedDeliveryId
                           && !e.IsRevoked);
    }

    public async Task<List<int>> GetActiveExemptStudentIdsForPlanAsync(int plannedDeliveryId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await db.LetterExemptions
            .AsNoTracking()
            .Where(e => e.PlannedDeliveryId == plannedDeliveryId && !e.IsRevoked)
            .Select(e => e.StudentId)
            .Distinct()
            .ToListAsync();
    }
}