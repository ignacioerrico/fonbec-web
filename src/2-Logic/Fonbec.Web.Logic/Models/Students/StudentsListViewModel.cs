using Fonbec.Web.DataAccess.DataModels.Students;
using Fonbec.Web.DataAccess.Entities.Enums;
using Mapster;

namespace Fonbec.Web.Logic.Models.Students;

public class StudentsListViewModel : AuditableViewModel
{
    public int StudentId { get; set; }
    public string StudentFirstName { get; set; } = string.Empty;
    public string StudentLastName { get; set; } = string.Empty;
    public string StundentNickName { get; set; } = string.Empty;
    public Gender StudentGender { get; set; }
    public bool IsStudentActive { get; set; }
    public int FacilitatorId { get; set; }
    public string FacilitatorFullName { get; set; } = string.Empty;
    public string FacilitatorEmail { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string StudentNotes { get; set; } = string.Empty;
    public string StudentCurrentEducationLevel { get; set; } = string.Empty;
    public DateTime? StudentSecondarySchoolStartYear { get; set; }
    public DateTime? StudentUniversityStartYear { get; set; }
    public string StudentPhoneNumber { get; set; } = string.Empty;
}

public class StudentsListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllStudentsDataModel, StudentsListViewModel>()
            .Map(dest => dest.StudentId, src => src.StudentId)
            .Map(dest => dest.StudentFirstName, src => src.StudentFirstName)
            .Map(dest => dest.StudentLastName, src => src.StudentLastName)
            .Map(dest => dest.StundentNickName, src => src.StundentNickName ?? string.Empty)
            .Map(dest => dest.StudentGender, src => src.StudentGender)
            .Map(dest => dest.IsStudentActive, src => src.IsStudentActive)
            .Map(dest => dest.FacilitatorId, src => src.FacilitatorId)
            .Map(dest => dest.FacilitatorFullName, src => $"{src.FacilitatorFirstName} {src.FacilitatorLastName}")
            .Map(dest => dest.FacilitatorEmail, src => src.FacilitatorEmail ?? string.Empty)
            .Map(dest => dest.StudentEmail, src => src.StudentEmail ?? string.Empty)
            .Map(dest => dest.StudentNotes, src => src.StudentNotes ?? string.Empty)
            .Map(dest => dest.StudentCurrentEducationLevel, src => src.StudentCurrentEducationLevel.EnumToString())
            .Map(dest => dest.StudentSecondarySchoolStartYear, src => src.StudentSecondarySchoolStartYear)
            .Map(dest => dest.StudentUniversityStartYear, src => src.StudentUniversityStartYear)
            .Map(dest => dest.StudentPhoneNumber, src => src.StudentPhoneNumber ?? string.Empty);
    }
}