using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.Entities;

public class Sponsorship : Auditable
{
    // pk 
    public int Id { get; set; }

    // fk
    public int StudentId { get; set; }

    // fk
    public int SponsorId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}