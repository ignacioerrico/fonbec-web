using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Models.Students;

/// <summary>
/// Pure helpers for the letter-status column (us111): per-slot status mapping, the aggregate
/// upload-completeness rule, and the "Solo carta pendiente o rechazada" filter predicate.
/// </summary>
public static class LetterAggregation
{
    public static LetterSlotStatus ToSlotStatus(DocumentStatus? letterStatus) => letterStatus switch
    {
        null => LetterSlotStatus.None,
        DocumentStatus.Approved => LetterSlotStatus.Approved,
        DocumentStatus.Rejected => LetterSlotStatus.Rejected,
        _ => LetterSlotStatus.InReview, // Pending, PendingImprovement, ProcessingImprovement, Processing
    };

    /// <summary>
    /// A slot is "satisfied" when it has a current letter that does not need (re)uploading,
    /// i.e. the letter is in review or approved. Missing and rejected letters are not satisfied.
    /// </summary>
    public static bool IsSatisfied(LetterSlotStatus status) =>
        status is LetterSlotStatus.InReview or LetterSlotStatus.Approved;

    /// <summary>A slot needs a (re)upload when no letter exists yet or the last one was rejected.</summary>
    public static bool NeedsUpload(LetterSlotStatus status) =>
        status is LetterSlotStatus.None or LetterSlotStatus.Rejected;

    public static LetterAggregateStatus Aggregate(bool hasActivePlan, bool isExempt, IReadOnlyList<LetterSlotStatus> slotStatuses)
    {
        if (!hasActivePlan)
        {
            return LetterAggregateStatus.NoPlan;
        }

        if (isExempt)
        {
            return LetterAggregateStatus.Exempt;
        }

        if (slotStatuses.Count == 0)
        {
            return LetterAggregateStatus.Complete;
        }

        var satisfied = slotStatuses.Count(IsSatisfied);

        if (satisfied == 0)
        {
            return LetterAggregateStatus.NotUploaded;
        }

        return satisfied == slotStatuses.Count
            ? LetterAggregateStatus.Complete
            : LetterAggregateStatus.Partial;
    }

    /// <summary>
    /// Filter keeps students who still owe at least one letter (NotUploaded or Partial).
    /// Exempt, NoPlan and Complete are excluded by construction.
    /// </summary>
    public static bool MatchesLetterFilter(LetterAggregateStatus aggregate, bool filterActive) =>
        !filterActive || aggregate is LetterAggregateStatus.NotUploaded or LetterAggregateStatus.Partial;
}