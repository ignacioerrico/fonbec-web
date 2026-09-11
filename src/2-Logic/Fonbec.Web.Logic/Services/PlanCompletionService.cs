using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.LetterPlanProgress;
using Fonbec.Web.Logic.Models.PlannedDeliveries;

namespace Fonbec.Web.Logic.Services;

public interface IPlanCompletionService
{
    Task<PlanReadinessResult> GetReadinessAsync(
        int planId,
        int chapterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the plan complete when it is ready. Idempotent if already complete.
    /// Never reopens a completed plan.
    /// </summary>
    Task<CompletePlanResult> CompletePlanAsync(
        int planId,
        int chapterId,
        int completedByUserId,
        CancellationToken cancellationToken = default);
}

public class PlanCompletionService(
    ILetterPlanProgressRepository letterPlanProgressRepository,
    IPlannedDeliveryRepository plannedDeliveryRepository) : IPlanCompletionService
{
    public const string PlanNotFound = "No se encontró la planificación.";
    public const string PlanNotReady = "La campaña aún no está lista para completar.";

    public async Task<PlanReadinessResult> GetReadinessAsync(
        int planId,
        int chapterId,
        CancellationToken cancellationToken = default)
    {
        var progress = await letterPlanProgressRepository.GetProgressAsync(planId, chapterId);
        if (progress is null)
        {
            return new PlanReadinessResult();
        }

        return BuildReadiness(progress);
    }

    public async Task<CompletePlanResult> CompletePlanAsync(
        int planId,
        int chapterId,
        int completedByUserId,
        CancellationToken cancellationToken = default)
    {
        var progress = await letterPlanProgressRepository.GetProgressAsync(planId, chapterId);
        if (progress is null)
        {
            return new CompletePlanResult { Errors = [PlanNotFound] };
        }

        if (progress.IsPlanCompleted)
        {
            return new CompletePlanResult { Success = true, AlreadyCompleted = true };
        }

        var readiness = BuildReadiness(progress);
        if (!readiness.IsReadyToComplete)
        {
            return new CompletePlanResult { Errors = [PlanNotReady] };
        }

        var changed = await plannedDeliveryRepository
            .SetPlanCompletedAsync(planId, completed: true, completedByUserId);

        return new CompletePlanResult
        {
            Success = true,
            StatusChanged = changed,
        };
    }

    private static PlanReadinessResult BuildReadiness(
        DataAccess.DataModels.LetterPlanProgress.LetterPlanProgressQueryResultDataModel progress)
    {
        var statuses = progress.Rows
            .Select(row => LetterPlanDisplayStatusExtensions.FromRow(row.IsExempt, row.LetterStatus))
            .ToList();
        var summary = statuses.ToSummary();

        return new PlanReadinessResult
        {
            PlanFound = true,
            IsCompleted = progress.IsPlanCompleted,
            IsReadyToComplete = statuses.IsReadyToComplete(),
            PlanStartsOn = progress.PlanStartsOn,
            SlotCount = statuses.Count,
            TotalRequired = summary.TotalRequired,
            ApprovedCount = summary.Approved,
        };
    }
}