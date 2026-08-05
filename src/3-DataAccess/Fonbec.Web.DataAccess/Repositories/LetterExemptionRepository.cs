using Fonbec.Web.DataAccess.Entities;
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

    /// <summary>
    /// Creates an active letter exemption for the student and plan. Returns false when already exempt.
    /// </summary>
    Task<bool> CreateExemptionAsync(
        int studentId, int plannedDeliveryId, int chapterId, string reason, int createdByUserId, DateTime createdOnUtc);

    /// <summary>
    /// Revokes the active exemption for the student and plan. Returns false when no active exemption exists.
    /// </summary>
    Task<bool> RevokeExemptionAsync(
        int studentId, int plannedDeliveryId, int revokedByUserId, DateTime revokedOnUtc);
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

    public async Task<bool> CreateExemptionAsync(
        int studentId, int plannedDeliveryId, int chapterId, string reason, int createdByUserId, DateTime createdOnUtc)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var alreadyExempt = await db.LetterExemptions
            .AnyAsync(e => e.StudentId == studentId
                           && e.PlannedDeliveryId == plannedDeliveryId
                           && !e.IsRevoked);

        if (alreadyExempt)
        {
            return false;
        }

        db.LetterExemptions.Add(new LetterExemption
        {
            StudentId = studentId,
            PlannedDeliveryId = plannedDeliveryId,
            ChapterId = chapterId,
            Reason = reason,
            CreatedByFonbecUserId = createdByUserId,
            CreatedOnUtc = createdOnUtc,
            IsRevoked = false,
        });

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RevokeExemptionAsync(
        int studentId, int plannedDeliveryId, int revokedByUserId, DateTime revokedOnUtc)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var exemption = await db.LetterExemptions
            .FirstOrDefaultAsync(e => e.StudentId == studentId
                                      && e.PlannedDeliveryId == plannedDeliveryId
                                      && !e.IsRevoked);

        if (exemption is null)
        {
            return false;
        }

        exemption.IsRevoked = true;
        exemption.RevokedByFonbecUserId = revokedByUserId;
        exemption.RevokedOnUtc = revokedOnUtc;

        await db.SaveChangesAsync();
        return true;
    }
}