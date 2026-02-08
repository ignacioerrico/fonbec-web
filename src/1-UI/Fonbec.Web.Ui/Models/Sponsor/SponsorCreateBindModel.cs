using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Ui.Models.Sponsor;

public class SponsorCreateBindModel
{
    public int ChapterId { get; set; }

    public string SponsorFirstName { get; set; } = null!;

    public string SponsorLastName { get; set; } = null!;

    public string SponsorNickName { get; set; } = null!;

    public Gender SponsorGender { get; set; }

    public string SponsorPhoneNumber { get; set; } = null!;

    public string SponsorNotes { get; set; } = null!;

    public string SponsorEmail { get; set; } = null!;
}