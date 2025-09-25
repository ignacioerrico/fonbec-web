using Fonbec.Web.DataAccess.DataModels.Students.Input;
using Mapster;

namespace Fonbec.Web.Logic.Models.Students.Input;

public record UpdateStudentInputModel(
    int StudentId,
    string StudentFirstName,
    string StudentLastName,
    string StudentNickName,
    string StudentEmail,
    string StudentPhone,
    string StudentNotes,
    DateTime? StudentSecondarySchoolStartYear,
    DateTime? StudentUniversityStartYear,
    int FacilitatorId
);

public class UpdateStudentInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UpdateStudentInputModel, UpdateStudentInputDataModel>()
            .Map(dest => dest.StudentId, src => src.StudentId)
            .Map(dest => dest.StudentFirstName, src => src.StudentFirstName)
            .Map(dest => dest.StudentLastName, src => src.StudentLastName)
            .Map(dest => dest.StudentNickName, src => src.StudentNickName,
                src => !string.IsNullOrWhiteSpace(src.StudentNickName))
            .Map(dest => dest.StudentEmail, src => src.StudentEmail,
                src => !string.IsNullOrWhiteSpace(src.StudentEmail))
            .Map(dest => dest.StudentPhoneNumber, src => src.StudentPhone,
                src => !string.IsNullOrWhiteSpace(src.StudentPhone))
            .Map(dest => dest.StudentNotes, src => src.StudentNotes,
                src => !string.IsNullOrWhiteSpace(src.StudentNotes))
            .Map(dest => dest.StudentSecondarySchoolStartYear, src => src.StudentSecondarySchoolStartYear)
            .Map(dest => dest.StudentUniversityStartYear, src => src.StudentUniversityStartYear)
            .Map(dest => dest.FacilitatorId, src => src.FacilitatorId);
    }
}