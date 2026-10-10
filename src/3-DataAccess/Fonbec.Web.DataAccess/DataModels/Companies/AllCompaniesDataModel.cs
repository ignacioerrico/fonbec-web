using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.DataModels.Companies;

public class AllCompaniesDataModel(Auditable auditable) : AuditableDataModel(auditable)
{
    public int CompanyId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string? CompanyEmail { get; set; }

    public string? CompanyPhoneNumber { get; set; }

    public List<Sponsor>? CompanySponsors { get; set; } = [];

    public List<PointOfContact>? CompanyPointsOfContact { get; set; } = [];

    public List<CompanySponsoredStudentDataModel> SponsoredStudents { get; set; } = [];
}

public class CompanySponsoredStudentDataModel
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Null when the company sponsors the student directly.</summary>
    public string? SponsorName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}