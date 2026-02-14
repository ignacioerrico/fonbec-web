namespace Fonbec.Web.Ui.Models.Sponsorship;
public class SponsorshipCreateBindModel
{
    public int StudentId { get; set; }

    public int SponsorId { get; set; }

    // issue with Notes...
    public string SponsorshipNotes { get; set; } = null!;

    public DateTime? SponsorshipStartDate { get; set; }

    public DateTime? SponsorshipEndDate { get; set; }

}
