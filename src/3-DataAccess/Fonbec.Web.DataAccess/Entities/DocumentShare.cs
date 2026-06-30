namespace Fonbec.Web.DataAccess.Entities;

public class DocumentShare
{
    public long DocumentShareId { get; set; }

    public long DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public int SponsorId { get; set; }
    public Sponsor Sponsor { get; set; } = null!;

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public DateTime SharedOn { get; set; }

    public int SharedById { get; set; }
    public FonbecWebUser SharedBy { get; set; } = null!;

    public DateTime? NotificationSentOn { get; set; }
}