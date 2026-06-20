using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.Entities;

public class Company : Auditable
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public List<Sponsor>? Sponsors { get; set; }
    public List<Sponsorship> Sponsorships { get; set; } = [];
    public List<PointOfContact> PointsOfContact { get; set; } = [];

}