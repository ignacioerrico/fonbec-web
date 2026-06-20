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
}