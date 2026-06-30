namespace Fonbec.Web.DataAccess.Entities;

public class ReportCardReview
{
    public long ReportCardReviewId { get; set; }

    public long DocumentId { get; set; }
    public ReportCard Document { get; set; } = null!;

    public bool ConfirmedIsReportCardOrTranscript { get; set; }

    public bool ConfirmedStudentNameCorrect { get; set; }

    public int ReviewedById { get; set; }
    public FonbecWebUser ReviewedBy { get; set; } = null!;

    public DateTime ReviewedOn { get; set; }
}