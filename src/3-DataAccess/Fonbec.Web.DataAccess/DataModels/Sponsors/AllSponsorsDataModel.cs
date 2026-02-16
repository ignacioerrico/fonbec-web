using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.DataModels.Sponsors;

public class AllSponsorsDataModel(Auditable auditable) : AuditableDataModel(auditable)
{
    public int SponsorId { get; set; }

    public string SponsorFirstName { get; set; } = null!;

    public string SponsorLastName { get; set; } = null!;

    public string? SponsorNickName { get; set; }

    public string? SponsorPhoneNumber { get; set; }
    public string SponsorEmail { get; set; } = null!;

}
