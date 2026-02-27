using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Sponsors;

namespace Fonbec.Web.Ui.Models.Sponsor;

public class SponsorEditBindModel
{
    public string SponsorFirstName { get; set; } = string.Empty;
    public string SponsorLastName { get; set; } = string.Empty;
    public string SponsorNickName { get; set; } = string.Empty;
    public Gender SponsorGender { get; set; }
    public string SponsorPhoneNumber { get; set; } = string.Empty;
    public string SponsorEmail { get; set; } = string.Empty;

    public SponsorEditBindModel() { }

    public SponsorEditBindModel(SponsorsListViewModel vm)
    {
        SponsorFirstName = vm.SponsorFirstName;
        SponsorLastName = vm.SponsorLastName;
        SponsorNickName = vm.SponsorNickName;
        SponsorPhoneNumber = vm.SponsorPhoneNumber;
        SponsorEmail = vm.SponsorEmail;
    }

    public bool HasChangesComparedTo(SponsorEditBindModel other) =>
        SponsorFirstName != other.SponsorFirstName ||
        SponsorLastName != other.SponsorLastName ||
        SponsorNickName != other.SponsorNickName ||
        SponsorPhoneNumber != other.SponsorPhoneNumber ||
        SponsorEmail != other.SponsorEmail ||
        SponsorGender != other.SponsorGender;
}