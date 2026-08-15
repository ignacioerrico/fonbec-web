using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Models.LetterPlanProgress;

public static class LetterPlanDisplayStatusExtensions
{
    public static LetterPlanDisplayStatus FromRow(bool isExempt, DocumentStatus? letterStatus)
    {
        if (isExempt)
        {
            return LetterPlanDisplayStatus.Exempt;
        }

        if (letterStatus is null)
        {
            return LetterPlanDisplayStatus.Missing;
        }

        return letterStatus.Value switch
        {
            DocumentStatus.PendingImprovement => LetterPlanDisplayStatus.PendingImprovement,
            DocumentStatus.ProcessingImprovement => LetterPlanDisplayStatus.ProcessingImprovement,
            DocumentStatus.Pending => LetterPlanDisplayStatus.PendingReview,
            DocumentStatus.Processing => LetterPlanDisplayStatus.ProcessingReview,
            DocumentStatus.Approved => LetterPlanDisplayStatus.Approved,
            DocumentStatus.Rejected => LetterPlanDisplayStatus.Rejected,
            _ => LetterPlanDisplayStatus.Missing,
        };
    }

    public static string ToStatusLabel(this LetterPlanDisplayStatus status) => status switch
    {
        LetterPlanDisplayStatus.Missing => "Falta carta",
        LetterPlanDisplayStatus.PendingImprovement => "Pendiente de mejora digital",
        LetterPlanDisplayStatus.ProcessingImprovement => "Mejora digital en curso",
        LetterPlanDisplayStatus.PendingReview => "En cola",
        LetterPlanDisplayStatus.ProcessingReview => "En revisión",
        LetterPlanDisplayStatus.Approved => "Aprobada",
        LetterPlanDisplayStatus.Rejected => "Rechazada",
        LetterPlanDisplayStatus.Exempt => "Eximido",
        _ => string.Empty,
    };

    public static bool CountsAsApproved(this LetterPlanDisplayStatus status) =>
        status == LetterPlanDisplayStatus.Approved;

    public static bool CountsAsInProgress(this LetterPlanDisplayStatus status) =>
        status is LetterPlanDisplayStatus.PendingImprovement
            or LetterPlanDisplayStatus.ProcessingImprovement
            or LetterPlanDisplayStatus.PendingReview
            or LetterPlanDisplayStatus.ProcessingReview;

    public static bool CountsAsMissingOrRejected(this LetterPlanDisplayStatus status) =>
        status is LetterPlanDisplayStatus.Missing or LetterPlanDisplayStatus.Rejected;

    /// <summary>
    /// Single definition of the required-slot counts. Exempt slots are excluded from the required
    /// set; every other slot is classified via the <c>CountsAs*</c> rules above. Shared by the progress
    /// UI projection and the plan-completion evaluation so their counts cannot drift.
    /// </summary>
    public static LetterPlanProgressSummaryViewModel ToSummary(this IEnumerable<LetterPlanDisplayStatus> statuses)
    {
        var required = statuses
            .Where(status => status != LetterPlanDisplayStatus.Exempt)
            .ToList();

        var totalRequired = required.Count;
        var approved = required.Count(status => status.CountsAsApproved());

        return new LetterPlanProgressSummaryViewModel
        {
            TotalRequired = totalRequired,
            Approved = approved,
            InProgress = required.Count(status => status.CountsAsInProgress()),
            MissingOrRejected = required.Count(status => status.CountsAsMissingOrRejected()),
            CompletionPercent = totalRequired == 0
                ? 0m
                : Math.Round(100m * approved / totalRequired, 0),
        };
    }
}