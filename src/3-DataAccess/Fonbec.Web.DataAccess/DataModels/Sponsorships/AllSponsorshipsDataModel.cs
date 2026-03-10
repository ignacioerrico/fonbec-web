using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.DataModels.Sponsorships;

public class AllSponsorshipsDataModel(Auditable auditable) : AuditableDataModel(auditable)
{
    public int StudentId { get; set; }
    public int? SponsorId { get; set; }
    public int? CompanyId { get; set; }
    public DateTime SponsorshipStartDate { get; set; }
    public DateTime? SponsorshipEndDate { get; set; }
    public string? SponsorshipNotes { get; set; }
    public int CreatedById { get; set; }
    public string SponsorshipState { get; set; }
    public string? SponsorFullName { get; set; }
    public string StudentFullName { get; set; }
    public string? CompanyName { get; set; }
}