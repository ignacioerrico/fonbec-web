using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
public class CreateSponsorInputDataModel
{
    public int ChapterId { get; set; } 
    
    public string SponsorFirstName { get; set; } = null!;
    
    public string SponsorLastName { get; set; } = null!;
    
    public string? SponsorNickName { get; set; }
    
    public Gender SponsorGender { get; set; } 
    
    public string? SponsorPhoneNumber { get; set; }
    
    public string? SponsorNotes { get; set; }
    
    public string SponsorEmail { get; set; } = string.Empty;
    
    public int CreatedById { get; set; }
}
