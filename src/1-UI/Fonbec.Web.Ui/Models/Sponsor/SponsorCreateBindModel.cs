using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Ui.Models.Sponsor;
public class SponsorCreateBindModel
{
    public int ChapterId { get; set; }

    public string SponsorFirstName { get; set; } = null!;

    public string SponsorLastName { get; set; } = null!;

    public string? SponsorNickName { get; set; }

    public Gender SponsorGender { get; set; }

    public string SponsorPhoneNumber { get; set; } = null!;

    public string? SponsorNotes { get; set; }

    public string SponsorEmail { get; set; } = null!;

    public string? SponsorSendAlsoTo { get; set; }

    public string SponsorBranchOffice { get; set; } = null!;
}
