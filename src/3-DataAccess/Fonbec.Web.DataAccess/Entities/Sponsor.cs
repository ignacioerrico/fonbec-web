using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.Entities;

public class Sponsor : UserWithoutAccount
{
    public string Email { get; set; } = string.Empty;

    public List<Sponsorship> Sponsorships { get; set; } = [];
}