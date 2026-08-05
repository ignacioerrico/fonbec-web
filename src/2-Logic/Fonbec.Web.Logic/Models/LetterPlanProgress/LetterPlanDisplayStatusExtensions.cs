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
}