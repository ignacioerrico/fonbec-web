using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Ui.Models.Student;

public class StudentCreateBindModel
{
    public int ChapterId { get; set; }

    public string StudentFirstName { get; set; } = null!;
    
    public string StudentLastName { get; set; } = null!;
    
    public string StudentNickName { get; set; } = null!;
    
    public Gender StudentGender { get; set; }

    public string StudentEmail { get; set; } = null!;
    
    public string StudentPhoneNumber { get; set; } = null!;
    
    public string StudentNotes { get; set; } = null!;
    
    public DateTime? StudentSecondarySchoolStartYear { get; set; }
    
    public DateTime? StudentUniversityStartYear { get; set; }
    
    public int FacilitatorId { get; set; }
}