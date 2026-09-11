namespace Fonbec.Web.Logic.Models.PlannedDeliveries;

public class PlanReadinessResult
{
    public bool PlanFound { get; init; }

    public bool IsCompleted { get; init; }

    /// <summary>
    /// True when the plan has at least one computed slot and every slot is approved or exempt.
    /// </summary>
    public bool IsReadyToComplete { get; init; }

    public DateTime PlanStartsOn { get; init; }

    public int SlotCount { get; init; }

    public int TotalRequired { get; init; }

    public int ApprovedCount { get; init; }
}

public class CompletePlanResult
{
    public bool Success { get; init; }

    public bool AlreadyCompleted { get; init; }

    public bool StatusChanged { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];
}
