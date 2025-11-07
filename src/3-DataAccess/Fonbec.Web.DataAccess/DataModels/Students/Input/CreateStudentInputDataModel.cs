using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Students.Input;

public class CreateStudentInputDataModel
{
    public int StudentId { get; set; }

    public string StudentFirstName { get; set; } = null!;
    
    public string StudentLastName { get; set; } = null!;

    public string? StudentNickName { get; set; }

    public Gender StudentGender { get; set; }

    public string? StudentEmail { get; set; }

    public string? StudentPhoneNumber { get; set; }

    public string? StudentNotes { get; set; }

    public DateTime? StudentSecondarySchoolStartYear { get; set; }

    public DateTime? StudentUniversityStartYear { get; set; }

    public int FacilitatorId { get; set; }

    public int CreatedById { get; set; }
}