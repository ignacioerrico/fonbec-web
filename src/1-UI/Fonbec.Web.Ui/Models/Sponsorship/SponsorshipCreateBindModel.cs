namespace Fonbec.Web.Ui.Models.Sponsorship;

public class SponsorshipCreateBindModel
{ 
    public int StudentId { get; set; }
    public int SponsorId { get; set; }
    public DateTime SponsorshipStartDate { get; set; }
    public DateTime? SponsorshipEndDate { get; set; }
    public int CreatedById { get; set; }
}
