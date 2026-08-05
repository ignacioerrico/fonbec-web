using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Models.Students;

/// <summary>
/// Pure helpers for the letter-status column (us111): per-slot status mapping, the aggregate
/// precedence rule, and the "Solo carta pendiente o rechazada" filter predicate.
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

        if (slotStatuses.Any(status => status == LetterSlotStatus.Rejected))
        {
            return LetterAggregateStatus.Rejected;
        }

        return slotStatuses.Any(status => status != LetterSlotStatus.Approved)
            ? LetterAggregateStatus.Pending
            : LetterAggregateStatus.Approved;
    }

    /// <summary>Exempt and NoPlan are excluded by construction: neither is Rejected nor Pending.</summary>
    public static bool MatchesLetterFilter(LetterAggregateStatus aggregate, bool filterActive) =>
        !filterActive || aggregate is LetterAggregateStatus.Rejected or LetterAggregateStatus.Pending;
}
