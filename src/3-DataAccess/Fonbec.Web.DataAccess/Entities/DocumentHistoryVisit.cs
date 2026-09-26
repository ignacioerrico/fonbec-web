namespace Fonbec.Web.DataAccess.Entities;

/// <summary>
/// Last time a person-sponsor or company opened the anonymous document-history page for a student.
/// Used to draw the unread / anteriores divider.
/// </summary>
public class DocumentHistoryVisit
{
    public long DocumentHistoryVisitId { get; set; }

    /// <summary>
    /// Visiting person-sponsor. Mutually exclusive with <see cref="CompanyId"/>.
    /// </summary>
    public int? SponsorId { get; set; }
    public Sponsor? Sponsor { get; set; }

    /// <summary>
    /// Visiting company. Mutually exclusive with <see cref="SponsorId"/>.
    /// </summary>
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public DateTime LastVisitedOnUtc { get; set; }
}