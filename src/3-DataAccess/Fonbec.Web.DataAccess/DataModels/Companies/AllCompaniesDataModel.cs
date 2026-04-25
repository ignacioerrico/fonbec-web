using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.DataModels.Companies;

public class AllCompaniesDataModel(Auditable auditable) : AuditableDataModel(auditable)
{
    public int CompanyId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string? CompanyEmail { get; set; }

    public string? CompanyPhoneNumber { get; set; }

    public string? CompanyNotes { get; set; }

    public List<Sponsor>? CompanySponsors { get; set; } = [];

    public List<PointOfContact>? CompanyPOCs { get; set; } = [];
}