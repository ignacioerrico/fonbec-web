namespace Fonbec.Web.Logic.Models.Students;

public class StudentCreateViewModel
{
    public string StudentFirstName { get; set; } = null!;
    public string StudentLastName { get; set; } = null!;
    public string StudentNickName { get; set; } = null!;
    public string StudentEmail { get; set; } = null!;
    public string StudentPhoneNumber { get; set; } = null!;
    public string StudentNotes { get; set; } = null!;
    public DateTime? StudentSecondarySchoolSinceUtc { get; set; }
    public DateTime? StudentUniversitySinceUtc { get; set; }
    public int FacilitatorId { get; set; }
    public int CreatedById { get; set; }
}