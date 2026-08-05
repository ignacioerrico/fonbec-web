namespace Fonbec.Web.Logic.Models.Students;

public class SponsorLetterStatusViewModel
{
    public int SponsorshipId { get; set; }

    public int? SponsorId { get; set; }

    public int? CompanyId { get; set; }

    public bool IsCompany { get; set; }

    public string RecipientName { get; set; } = null!;

    public LetterSlotStatus Status { get; set; }

    public string? RejectionReason { get; set; }
}
