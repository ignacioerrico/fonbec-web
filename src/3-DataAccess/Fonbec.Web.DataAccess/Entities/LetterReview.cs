namespace Fonbec.Web.DataAccess.Entities;

public class LetterReview
{
    public long LetterReviewId { get; set; }

    public long DocumentId { get; set; }
    public Letter Document { get; set; } = null!;

    public bool ConfirmedIsLetter { get; set; }

    public DateTime ConfirmedWrittenDate { get; set; }

    public bool ConfirmedAddressee { get; set; }

    public bool ConfirmedSignerMatchesStudent { get; set; }

    public long AssessmentId { get; set; }
    public Assessment Assessment { get; set; } = null!;

    public int ReviewedById { get; set; }
    public FonbecWebUser ReviewedBy { get; set; } = null!;

    public DateTime ReviewedOn { get; set; }
}