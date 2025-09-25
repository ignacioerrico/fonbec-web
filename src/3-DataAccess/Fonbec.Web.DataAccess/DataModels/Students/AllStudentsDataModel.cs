using Fonbec.Web.DataAccess.Entities.Abstract;
using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Students;

public class AllStudentsDataModel(Auditable auditable) : AuditableDataModel(auditable)
{
    public int StudentId { get; set; }

    public string StudentFirstName { get; set; } = null!;
    
    public string StudentLastName { get; set; } = null!;

    public string? StundentNickName { get; set; }

    public Gender StudentGender { get; set; }

    public bool IsStudentActive { get; set; }

    public int FacilitatorId { get; set; }

    public string FacilitatorFirstName { get; set; } = null!;

    public string FacilitatorLastName { get; set; } = null!;

    public string? FacilitatorEmail { get; set; } // This should never be null, but since it comes from AspNetUsers table, it can be null

    public string? StudentEmail { get; set; }

    public string? StudentNotes { get; set; }

    public EducationLevel StudentCurrentEducationLevel { get; set; }

    public DateTime? StudentSecondarySchoolStartYear { get; set; }

    public DateTime? StudentUniversityStartYear { get; set; }

    public string? StudentPhoneNumber { get; set; }
}