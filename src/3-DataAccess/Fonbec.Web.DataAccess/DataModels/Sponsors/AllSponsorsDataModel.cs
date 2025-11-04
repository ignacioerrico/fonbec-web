using Fonbec.Web.DataAccess.Entities.Abstract;
using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Sponsors;

public class AllSponsorsDataModel(Auditable auditable) : AuditableDataModel(auditable)
{
    public int SponsorId { get; set; }

    public string SponsorFirstName { get; set; } = null!;

    public string SponsorLastName { get; set; } = null!;

    public string? SponsorNickName { get; set; }

    public Gender SponsorGender { get; set; }

    public bool IsSponsorActive { get; set; }

    public string SponsorEmail { get; set; } = null!;

    public string? SponsorPhoneNumber { get; set; }
}