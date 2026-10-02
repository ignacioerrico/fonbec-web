using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.Entities;

public class SendAlsoTo : Auditable
{
    public int Id { get; set; }

    public string RecipientName { get; set; } = null!;

    public string RecipientEmail { get; set; } = null!;

    public bool SendAsBcc { get; set; }

    public int SponsorId { get; set; }
    public Sponsor Sponsor { get; set; } = null!;
}