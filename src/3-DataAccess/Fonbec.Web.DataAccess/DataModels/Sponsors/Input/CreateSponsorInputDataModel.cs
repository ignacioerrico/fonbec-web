using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
public class CreateSponsorInputDataModel
{
    public int ChapterId { get; set; } 
    public string SponsorFirstName { get; set; } = string.Empty;
    public string SponsorLastName { get; set; } = string.Empty;
    public string? SponsorNickName { get; set; }
    public Gender SponsorGender { get; set; } 
    public string? SponsorPhoneNumber { get; set; }
    public string? SponsorNotes { get; set; }
    public string SponsorEmail { get; set; } = string.Empty;
    public List<string>? SponsorSendAlsoTo { get; set; }
    public string SponsorBranchOffice { get; set; } = string.Empty;
}
