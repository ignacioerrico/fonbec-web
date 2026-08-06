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
    /// Returns the exemption reason keyed by student id for every student exempt from letters for the plan.
    /// A present key means the student is exempt; the value is the reason. Duplicate exemptions keep the first.
    /// </summary>
    Task<Dictionary<int, string>> GetActiveExemptionReasonsForPlanAsync(int planId);
}

public class LetterExemptionService(ILetterExemptionRepository letterExemptionRepository) : ILetterExemptionService
{
    public Task<bool> IsExemptAsync(int studentId, int planId) =>
        letterExemptionRepository.IsActiveExemptionAsync(studentId, planId);

    public async Task<Dictionary<int, string>> GetActiveExemptionReasonsForPlanAsync(int planId)
    {
        var exemptions = await letterExemptionRepository.GetActiveExemptionsForPlanAsync(planId);
        return exemptions
            .GroupBy(e => e.StudentId)
            .ToDictionary(g => g.Key, g => g.First().Reason);
    }
}