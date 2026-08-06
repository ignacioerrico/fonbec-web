using Fonbec.Web.DataAccess.DataModels.LetterExemptions;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ILetterExemptionRepository
{
    /// <summary>
    /// Returns <c>true</c> when the student has an active (non-revoked) letter exemption for the plan.
    /// </summary>
    Task<bool> IsActiveExemptionAsync(int studentId, int plannedDeliveryId);

    /// <summary>
    /// Returns the active (non-revoked) letter exemptions for the plan, each with its student and reason.
    /// </summary>
    Task<List<LetterExemptionReasonDataModel>> GetActiveExemptionsForPlanAsync(int plannedDeliveryId);
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

    public async Task<List<LetterExemptionReasonDataModel>> GetActiveExemptionsForPlanAsync(int plannedDeliveryId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await db.LetterExemptions
            .AsNoTracking()
            .Where(e => e.PlannedDeliveryId == plannedDeliveryId && !e.IsRevoked)
            .Select(e => new LetterExemptionReasonDataModel
            {
                StudentId = e.StudentId,
                Reason = e.Reason,
            })
            .ToListAsync();
    }
}