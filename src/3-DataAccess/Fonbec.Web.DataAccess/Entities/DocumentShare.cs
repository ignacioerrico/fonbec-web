namespace Fonbec.Web.DataAccess.Entities;

public class DocumentShare
{
    public long DocumentShareId { get; set; }

    public long DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    /// <summary>
    /// Recipient person-sponsor. Mutually exclusive with <see cref="CompanyId"/>:
    /// a share is addressed to exactly one sponsor, which is either a person or a company.
    /// </summary>
    public int? SponsorId { get; set; }
    public Sponsor? Sponsor { get; set; }

    /// <summary>
    /// Recipient company-sponsor. Mutually exclusive with <see cref="SponsorId"/>.
    /// </summary>
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public DateTime SharedOn { get; set; }

    public int SharedById { get; set; }
    public FonbecWebUser SharedBy { get; set; } = null!;

    public DateTime? NotificationSentOn { get; set; }
}