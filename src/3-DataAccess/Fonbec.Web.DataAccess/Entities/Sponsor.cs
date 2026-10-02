using Fonbec.Web.DataAccess.Entities.Abstract;
using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.Entities;

public class Sponsor : UserWithoutAccount
{
    public string Email { get; set; } = string.Empty;

    public Guid PublicAccessToken { get; set; }

    public SponsorNotificationPreference NotificationPreference { get; set; } = SponsorNotificationPreference.EveryDocument;

    public List<Sponsorship> Sponsorships { get; set; } = [];

    public List<SendAlsoTo> SendAlsoTos { get; set; } = [];

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
}