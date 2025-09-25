namespace Fonbec.Web.DataAccess.DataModels.Students.Input;

public class UpdateStudentInputDataModel
{
    public int StudentId { get; set; }
    public string StudentFirstName { get; set; } = string.Empty;
    public string StudentLastName { get; set; } = string.Empty;
    public string? StudentNickName { get; set; }
    public string? StudentEmail { get; set; }
    public string? StudentPhoneNumber { get; set; }
    public string? StudentCurrentEducationLevel { get; set; }
    public string? StudentNotes { get; set; }
    public DateTime? StudentSecondarySchoolStartYear { get; set; }
    public DateTime? StudentUniversityStartYear { get; set; }
    public int FacilitatorId { get; set; }
}