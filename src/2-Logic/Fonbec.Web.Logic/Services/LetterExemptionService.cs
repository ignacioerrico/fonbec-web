using Fonbec.Web.DataAccess.Repositories;

namespace Fonbec.Web.Logic.Services;

public interface ILetterExemptionService
{
    /// <summary>
    /// Returns <c>true</c> when the student is exempt from submitting a letter for the plan.
    /// Single source of truth consumed by the upload flow (us101) and the dashboard (us103/us107).
    /// </summary>
    Task<bool> IsExemptAsync(int studentId, int planId);

    /// <summary>
    /// Returns the set of student ids exempt from letters for the given plan.
    /// </summary>
    Task<HashSet<int>> GetExemptStudentIdsForPlanAsync(int planId);
}

public class LetterExemptionService(ILetterExemptionRepository letterExemptionRepository) : ILetterExemptionService
{
    public Task<bool> IsExemptAsync(int studentId, int planId) =>
        letterExemptionRepository.IsActiveExemptionAsync(studentId, planId);

    public async Task<HashSet<int>> GetExemptStudentIdsForPlanAsync(int planId)
    {
        var ids = await letterExemptionRepository.GetActiveExemptStudentIdsForPlanAsync(planId);
        return ids.ToHashSet();
    }
}