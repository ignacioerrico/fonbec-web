namespace Fonbec.Web.DataAccess.Entities;

public class Letter : Document
{
    public int PlanId { get; set; }
    public PlannedDelivery Plan { get; set; } = null!;

    /// <summary>
    /// Recipient company when the letter is addressed to a company sponsorship
    /// (mutually exclusive with <see cref="Document.SponsorId"/>).
    /// </summary>
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public LetterReview? Review { get; set; }
}