using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.LetterPlanStatistics;

public class LetterPlanStatisticsQueryResultDataModel
{
    public DateTime PlanStartsOn { get; set; }

    public int ExemptionCount { get; set; }

    public List<LetterPlanStatisticsLetterDataModel> Letters { get; set; } = [];
}

public class LetterPlanStatisticsLetterDataModel
{
    public DigitalImprovementStatus DigitalImprovementStatus { get; set; }

    public DocumentStatus Status { get; set; }

    public DateTime UploadedOn { get; set; }

    public DateTime? ImprovementFirstLockedOn { get; set; }

    public DateTime? DecisionImprovementLockedOn { get; set; }

    public DateTime? ImprovementCompletedOn { get; set; }

    public DateTime? ReviewFirstLockedOn { get; set; }

    public DateTime? DecisionReviewLockedOn { get; set; }

    public DateTime? ReviewedOn { get; set; }

    public string? ReviewerName { get; set; }

    public DateTime? RejectedOn { get; set; }

    public string? RejectedByName { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime? ConfirmedWrittenDate { get; set; }

    public DateTime? AddresseeNotificationSentOn { get; set; }

    public int? SpellingScore { get; set; }

    public int? PenmanshipScore { get; set; }

    public int? ContentScore { get; set; }
}