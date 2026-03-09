using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Sponsors.Input;

public class UpdateSponsorInputDataModel
{
    public int SponsorId { get; set; }
    public string SponsorFirstName { get; set; } = string.Empty;
    public string SponsorLastName { get; set; } = string.Empty;
    public string? SponsorNickName { get; set; }
    public Gender SponsorGender { get; set; }
    public string? SponsorPhoneNumber { get; set; }
    public string SponsorEmail { get; set; } = string.Empty;

    public int? SponsorCompanyId { get; set; }
    public int UpdatedById { get; set; }
}