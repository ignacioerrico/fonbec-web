using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.Entities;

public class Sponsorship : Auditable
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int? SponsorId { get; set; }
    public Sponsor? Sponsor { get; set; }

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}