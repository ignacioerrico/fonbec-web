using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.DataModels.Sponsorships;

public class AllSponsorshipsDataModel
{
    public string? StudentFullName { get; set; }

    public List<AllSponsorshipsSponsorshipsDataModel> Sponsorships { get; set; } = [];
}

public class AllSponsorshipsSponsorshipsDataModel(Auditable auditable) : AuditableDataModel(auditable)
{
    public int SponsorshipId { get; set; }
    public Sponsor? Sponsor { get; set; }
    public Company? Company { get; set; }
    public DateTime SponsorshipStartDate { get; set; }
    public DateTime? SponsorshipEndDate { get; set; }

    /// <summary>
    /// Plan <c>StartsOn</c> dates with an uploaded letter for this student + recipient that
    /// this row uniquely covers. The edit UI must keep these months inside the period.
    /// </summary>
    public List<DateTime> LockedPlanStartsOn { get; set; } = [];
}