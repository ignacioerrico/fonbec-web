using Fonbec.Web.DataAccess.Entities.Abstract;
using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.Entities;

public class Student : UserWithoutAccount
{
    public string? Email { get; set; }

    // | SecondarySchoolStartYear | UniversityStartYear |
    // +--------------------------+---------------------|
    // | NULL                     | NULL                | Student is in primary school
    // | NOT NULL                 | NULL                | Student is in secondary school if SecondarySchoolSinceUtc < DateTime.UtcNow, else student is in primary school
    // | NULL                     | NOT NULL            | Student is in university if UniversitySinceUtc < DateTime.UtcNow, else student is in secondary school
    // | NOT NULL                 | NOT NULL            | Student may be in primary school, secondary school or university depending on the values of SecondarySchoolSinceUtc and UniversitySinceUtc

    public DateTime? SecondarySchoolStartYear { get; set; }

    public DateTime? UniversityStartYear { get; set; }

    /// <summary>
    /// The proxy is the user with role of 'Uploader' who is responsible for the student
    /// and uploads the documents on behalf of the student.
    /// </summary>
    public int FacilitatorId { get; set; }
    public FonbecWebUser Facilitator { get; set; } = null!;

    public EducationLevel CurrentEducationLevel
    {
        get
        {
            var now = DateTime.UtcNow;
            if (UniversityStartYear <= now)
            {
                return EducationLevel.University;
            }
            
            return SecondarySchoolStartYear <= now
                ? EducationLevel.SecondarySchool
                : EducationLevel.PrimarySchool;
        }
    }
}