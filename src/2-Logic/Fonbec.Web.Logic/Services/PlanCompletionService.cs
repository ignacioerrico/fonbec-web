using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.LetterPlanProgress;
using Fonbec.Web.Logic.Models.PlannedDeliveries;

namespace Fonbec.Web.Logic.Services;

public interface IPlanCompletionService
{
    /// <summary>
    /// Re-evaluates plan completion for <paramref name="planId"/> in <paramref name="chapterId"/> and
    /// automatically completes or reopens the plan when the stored state no longer matches the letter
    /// approval reality. Auto-complete requires at least one required letter slot (empty plans are never
    /// auto-completed). When the flag changes, <c>LastUpdatedById</c> is set to
    /// <paramref name="triggeredByUserId"/>.
    /// </summary>
    Task<EvaluatePlanCompletionResult> EvaluateAndUpdateAsync(
        int planId,
        int chapterId,
        int triggeredByUserId,
        CancellationToken cancellationToken = default);
}

public class PlanCompletionService(
    ILetterPlanProgressRepository letterPlanProgressRepository,
    IPlannedDeliveryRepository plannedDeliveryRepository) : IPlanCompletionService
{
    public async Task<EvaluatePlanCompletionResult> EvaluateAndUpdateAsync(
        int planId,
        int chapterId,
        int triggeredByUserId,
        CancellationToken cancellationToken = default)
    {
        var progress = await letterPlanProgressRepository.GetProgressAsync(planId, chapterId);
        if (progress is null)
        {
            return new EvaluatePlanCompletionResult();
        }

        var wasComplete = progress.IsPlanCompleted;

        // Reuse the shared required-slot / current-letter / exemption rules: exempt slots are
        // excluded from the required set and treated as satisfied for completion purposes. Using the
        // same summary builder as the progress UI guarantees the counts cannot drift.
        var summary = progress.Rows
            .Select(row => LetterPlanDisplayStatusExtensions.FromRow(row.IsExempt, row.LetterStatus))
            .ToSummary();

        var totalRequired = summary.TotalRequired;
        var approvedCount = summary.Approved;

        // Empty-plan rule: never auto-complete a plan with no required letter slots, and leave the
        // stored flag untouched.
        if (totalRequired == 0)
        {
            return new EvaluatePlanCompletionResult
            {
                WasComplete = wasComplete,
                IsComplete = false,
                TotalRequired = 0,
                ApprovedCount = 0,
                StatusChanged = false,
            };
        }

        var isComplete = summary.AllApproved;

        var statusChanged = false;
        if (isComplete && !wasComplete)
        {
            statusChanged = await plannedDeliveryRepository
                .SetPlanCompletedAsync(planId, completed: true, triggeredByUserId);
        }
        else if (!isComplete && wasComplete)
        {
            statusChanged = await plannedDeliveryRepository
                .SetPlanCompletedAsync(planId, completed: false, triggeredByUserId);
        }

        return new EvaluatePlanCompletionResult
        {
            WasComplete = wasComplete,
            IsComplete = isComplete,
            TotalRequired = totalRequired,
            ApprovedCount = approvedCount,
            StatusChanged = statusChanged,
        };
    }
}