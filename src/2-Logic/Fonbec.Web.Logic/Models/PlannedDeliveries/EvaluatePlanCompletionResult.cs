namespace Fonbec.Web.Logic.Models.PlannedDeliveries;

/// <summary>
/// Outcome of evaluating whether a plan (<c>PlannedDelivery</c>) should be
/// automatically completed or reopened based on its required letter slots.
/// </summary>
public class EvaluatePlanCompletionResult
{
    /// <summary>Whether the plan was flagged as completed before evaluation.</summary>
    public bool WasComplete { get; init; }

    /// <summary>Whether the plan satisfies the completion rules after evaluation.</summary>
    public bool IsComplete { get; init; }

    /// <summary>Number of required (non-exempt) letter slots for the plan.</summary>
    public int TotalRequired { get; init; }

    /// <summary>Number of required slots whose current letter is approved.</summary>
    public int ApprovedCount { get; init; }

    /// <summary>Whether the stored <c>Completed</c> flag was changed by this evaluation.</summary>
    public bool StatusChanged { get; init; }
}