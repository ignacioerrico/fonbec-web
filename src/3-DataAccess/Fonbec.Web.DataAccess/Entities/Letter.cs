namespace Fonbec.Web.DataAccess.Entities;

public class Letter : Document
{
    public int PlanId { get; set; }
    public PlannedDelivery Plan { get; set; } = null!;

    public LetterReview? Review { get; set; }
}